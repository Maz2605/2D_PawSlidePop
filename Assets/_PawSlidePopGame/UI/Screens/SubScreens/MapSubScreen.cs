using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Core.System.GameFlow;
using _PawSlidePopGame._Scripts.Data.LevelProvider;
using _PawSlidePopGame._Scripts.Gameplay.Meta.MapManager;
using _PawSlidePopGame._Scripts.UI.Base;
using UnityEngine;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.UI.Screens.SubScreens
{
    public class MapSubScreen : BaseSubScreen
    {
        [Header("Fallback / Editor Binding")]
        [SerializeField] private Button btnLevel;
        [SerializeField] private string tempLevelId = "Level_001";

        [Header("Dynamic Binding Settings")]
        [SerializeField] private string levelIdPrefix = "Level_";

        [Header("Procedural Map")]
        [SerializeField] private MapWorldConfigSO mapConfig;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private RectTransform sectionRoot;
        [SerializeField] private MapLevelNodeView levelNodePrefab;
        [SerializeField, Min(1)] private int currentLevelNumber = 1;
        [SerializeField, Min(0f)] private float elasticScrollStrength = 0.12f;
        [SerializeField, Range(0.1f, 1.2f)] private float lockedSectionRevealRatio = 1f;
        [SerializeField, Min(1f)] private float progressBounceBackSpeed = 14f;
        [SerializeField, Range(0f, 1f)] private float targetViewportRatio = 0.4f;

        private readonly List<(Button button, string levelId)> _levelButtons = new List<(Button, string)>();
        private readonly ResourcesLevelCatalogProvider _catalogProvider = new ResourcesLevelCatalogProvider();
        private readonly MapManager _mapManager = new MapManager();
        private readonly List<MapSectionView> _sectionInstances = new List<MapSectionView>();

        private MapManager.MapLayoutData _layout;
        private bool _isScrollSubscribed;
        private bool _pendingSnapToCurrentLevel;
        private int _renderRetryCount;
        private float _scale = 1f;

        public override void Init()
        {
            if (isInitialized)
            {
                return;
            }

            base.Init();
            if (!TryRenderScenicMap())
            {
                DiscoverAndBindLevelButtons();
            }
        }

        public override void Show()
        {
            base.Show();

            if (isInitialized && HasScenicMapBinding())
            {
                TryRenderScenicMap();
            }
        }

        private void LateUpdate()
        {
            if (!isInitialized || contentRoot == null || scrollRect == null)
            {
                return;
            }

            if (_layout == null && _renderRetryCount < 5)
            {
                _renderRetryCount++;
                if (TryRenderScenicMap())
                {
                    _renderRetryCount = 0;
                }
                else
                {
                    return;
                }
            }

            if (_layout == null)
            {
                return;
            }

            UpdateMapScale();

            if (_pendingSnapToCurrentLevel)
            {
                _pendingSnapToCurrentLevel = false;
                ScrollToCurrentLevel(_layout);
            }

            ApplyProgressScrollLimit(immediate: false);
        }

        private void DiscoverAndBindLevelButtons()
        {
            _levelButtons.Clear();

            // Find all buttons in children
            Button[] buttons = GetComponentsInChildren<Button>(true);
            foreach (Button btn in buttons)
            {
                if (btn == null) continue;

                // Check if button name starts with "BtnLevel"
                if (btn.name.StartsWith("BtnLevel"))
                {
                    string levelId = DetermineLevelId(btn);
                    if (!string.IsNullOrEmpty(levelId))
                    {
                        _levelButtons.Add((btn, levelId));
                        BindButton(btn, () => HandleLevelPressed(levelId));
                        Debug.Log($"[MapSubScreen] Successfully bound level button '{btn.name}' to level '{levelId}'", btn);
                    }
                }
            }

            // Fallback to original single button if no dynamic buttons were bound
            if (_levelButtons.Count == 0 && btnLevel != null)
            {
                string fallbackLevelId = string.IsNullOrWhiteSpace(tempLevelId) ? "Level_001" : tempLevelId;
                BindButton(btnLevel, () => HandleLevelPressed(fallbackLevelId));
                Debug.Log($"[MapSubScreen] Fallback bound single button '{btnLevel.name}' to level '{fallbackLevelId}'", btnLevel);
            }
        }

        private bool TryRenderScenicMap()
        {
            if (!HasScenicMapBinding())
            {
                return false;
            }

            string fallbackCurrentLevelId = $"{levelIdPrefix}{1:D3}";
            LevelProgressRepository.Instance.EnsureInitializedProgress(fallbackCurrentLevelId);

            IReadOnlyList<string> levelIds = _catalogProvider.GetLevelIds();
            if (levelIds == null || levelIds.Count == 0)
            {
                Debug.LogWarning("[MapSubScreen] Cannot render procedural map because no level JSON files were found in Resources/Levels.", this);
                return false;
            }

            string currentLevelId = LevelProgressRepository.Instance.GetCurrentLevelId(fallbackCurrentLevelId);
            int resolvedCurrentLevelNumber = MapManager.TryParseLevelNumber(currentLevelId, out int parsedCurrentLevelNumber)
                ? parsedCurrentLevelNumber
                : Mathf.Max(1, currentLevelNumber);
            int resolvedHighestUnlockedLevelNumber = Mathf.Max(
                resolvedCurrentLevelNumber,
                LevelProgressRepository.Instance.GetHighestUnlockedLevelNumber());

            _layout = _mapManager.BuildLayout(
                levelIds,
                mapConfig,
                resolvedHighestUnlockedLevelNumber,
                resolvedCurrentLevelNumber,
                LevelProgressRepository.Instance,
                skipSectionZero: true);
            if (_layout == null || _layout.Entries.Count == 0 || _layout.Sections.Count == 0)
            {
                return false;
            }

            RectTransform viewport = scrollRect.viewport != null ? scrollRect.viewport : transform as RectTransform;
            if (viewport != null)
            {
                float viewportWidth = viewport.rect.width;
                _scale = viewportWidth > 0.01f ? (viewportWidth / 1080f) : 1f;
            }

            PrepareContent(_layout);
            RenderSections(_layout);
            Canvas.ForceUpdateCanvases();
            UpdateSectionCulling();
            ScrollToCurrentLevel(_layout);
            _pendingSnapToCurrentLevel = true;
            ApplyProgressScrollLimit(immediate: true);
            if (btnLevel != null)
            {
                btnLevel.gameObject.SetActive(false);
            }
            return true;
        }

        private bool HasScenicMapBinding()
        {
            if (!HasRequiredScenicBindings())
            {
                return false;
            }

            EnsureScrollSubscription();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.elasticity = Mathf.Max(0f, elasticScrollStrength);
            scrollRect.inertia = true;
            scrollRect.content = contentRoot;
            return true;
        }

        private void PrepareContent(MapManager.MapLayoutData layout)
        {
            ClearChildren(sectionRoot);
            _sectionInstances.Clear();

            float contentHeight = _mapManager.CalculateContentHeight(layout, mapConfig);
            contentRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight * _scale);
            sectionRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
            sectionRoot.localScale = new Vector3(_scale, _scale, 1f);
        }

        private void RenderSections(MapManager.MapLayoutData layout)
        {
            for (int i = 0; i < layout.Sections.Count; i++)
            {
                MapManager.MapSectionLayout sectionLayout = layout.Sections[i];
                if (sectionLayout.Prefab == null)
                {
                    continue;
                }


                MapSectionView sectionInstance = Instantiate(sectionLayout.Prefab, sectionRoot);
                sectionInstance.name = $"{sectionLayout.Prefab.name}_{sectionLayout.SectionIndex:000}";

                RectTransform rect = sectionInstance.transform as RectTransform;
                if (rect != null)
                {
                    rect.anchorMin = new Vector2(0.5f, 0f);
                    rect.anchorMax = new Vector2(0.5f, 0f);
                    rect.pivot = new Vector2(0.5f, 0f);
                    rect.anchoredPosition = new Vector2(0f, sectionLayout.AnchoredY);
                    rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, sectionLayout.SectionHeight);
                }

                sectionInstance.BindSection(
                    SliceEntries(layout.Entries, sectionLayout.StartEntryIndex, sectionLayout.EntryCount),
                    levelNodePrefab,
                    HandleLevelPressed);
                _sectionInstances.Add(sectionInstance);
            }
        }

        private void ScrollToCurrentLevel(MapManager.MapLayoutData layout)
        {
            if (scrollRect == null || contentRoot == null || layout == null || layout.Entries.Count == 0)
            {
                return;
            }

            int targetIndex = 0;
            for (int i = 0; i < layout.Entries.Count; i++)
            {
                if (layout.Entries[i].State == MapLevelState.Current)
                {
                    targetIndex = i;
                    break;
                }
            }

            RectTransform viewport = scrollRect.viewport != null ? scrollRect.viewport : transform as RectTransform;
            if (viewport == null)
            {
                return;
            }

            float targetPosY = layout.Entries[targetIndex].AnchoredPosition.y * _scale;
            float viewportHeight = viewport.rect.height;
            float contentHeight = Mathf.Max(1f, contentRoot.rect.height);
            float maxScrollY = Mathf.Max(0f, contentHeight - viewportHeight);
            float ratio = targetViewportRatio > 0.001f ? targetViewportRatio : 0.4f;
            float endY = Mathf.Clamp(maxScrollY - targetPosY + (viewportHeight * ratio), 0f, maxScrollY);
            contentRoot.anchoredPosition = new Vector2(contentRoot.anchoredPosition.x, endY);
            ApplyProgressScrollLimit(immediate: true);
            UpdateSectionCulling();
        }

        private void UpdateSectionCulling()
        {
            if (_layout == null || _layout.Sections.Count == 0 || _sectionInstances.Count == 0 || contentRoot == null)
            {
                return;
            }

            RectTransform viewport = scrollRect != null && scrollRect.viewport != null
                ? scrollRect.viewport
                : transform as RectTransform;
            if (viewport == null)
            {
                return;
            }

            float viewportHeight = viewport.rect.height;
            float contentHeight = Mathf.Max(1f, contentRoot.rect.height);

            // Correct culling viewport range for Top-Pivot content in content space:
            float currentViewportMinY = contentHeight - viewportHeight - contentRoot.anchoredPosition.y;

            // Add 1 screen height of padding above and below (3 screens total range)
            float viewportMinY = currentViewportMinY - viewportHeight;
            float viewportMaxY = currentViewportMinY + 2f * viewportHeight;
            float viewportCenterY = (viewportMinY + viewportMaxY) * 0.5f;
            HashSet<int> activeSectionIndexes = ResolveActiveSectionIndexes(viewportMinY, viewportMaxY, viewportCenterY);

            for (int i = 0; i < _sectionInstances.Count; i++)
            {
                if (_sectionInstances[i] == null)
                {
                    continue;
                }

                bool shouldBeActive = activeSectionIndexes.Contains(i);
                if (_sectionInstances[i].gameObject.activeSelf != shouldBeActive)
                {
                    _sectionInstances[i].gameObject.SetActive(shouldBeActive);
                }
            }
        }

        private HashSet<int> ResolveActiveSectionIndexes(float viewportMinY, float viewportMaxY, float viewportCenterY)
        {
            HashSet<int> activeIndexes = new HashSet<int>();
            List<int> nearestIndexes = new List<int>(_layout.Sections.Count);

            for (int i = 0; i < _layout.Sections.Count; i++)
            {
                MapManager.MapSectionLayout section = _layout.Sections[i];
                float sectionMinY = section.AnchoredY * _scale;
                float sectionMaxY = (section.AnchoredY + section.SectionHeight) * _scale;

                if (RangesOverlap(viewportMinY, viewportMaxY, sectionMinY, sectionMaxY))
                {
                    activeIndexes.Add(i);
                }

                nearestIndexes.Add(i);
            }

            nearestIndexes.Sort((a, b) =>
            {
                float distanceA = Mathf.Abs((_layout.Sections[a].CenterY * _scale) - viewportCenterY);
                float distanceB = Mathf.Abs((_layout.Sections[b].CenterY * _scale) - viewportCenterY);
                return distanceA.CompareTo(distanceB);
            });

            int minActiveCount = Mathf.Min(3, _layout.Sections.Count);
            for (int i = 0; i < nearestIndexes.Count && activeIndexes.Count < minActiveCount; i++)
            {
                activeIndexes.Add(nearestIndexes[i]);
            }

            return activeIndexes;
        }

        private static bool RangesOverlap(float minA, float maxA, float minB, float maxB)
        {
            return maxA >= minB && maxB >= minA;
        }

        private void EnsureScrollSubscription()
        {
            if (_isScrollSubscribed || scrollRect == null)
            {
                return;
            }

            scrollRect.onValueChanged.AddListener(HandleScrollValueChanged);
            _isScrollSubscribed = true;
        }

        private void HandleScrollValueChanged(Vector2 _)
        {
            ApplyProgressScrollLimit(immediate: false);
            UpdateSectionCulling();
        }

        private void ApplyProgressScrollLimit(bool immediate)
        {
            if (!TryGetMinimumAllowedAnchoredY(out float minimumAllowedAnchoredY) ||
                !TryGetBottomLimitParameters(out float bottomRestingScrollY, out float bottomOvershootLimit))
            {
                return;
            }

            Vector2 anchoredPosition = contentRoot.anchoredPosition;
            RectTransform viewport = scrollRect.viewport != null ? scrollRect.viewport : transform as RectTransform;
            if (viewport == null)
            {
                return;
            }

            float viewportHeight = viewport.rect.height;
            float maxOvershoot = viewportHeight * 0.5f;

            bool isUserInteracting = UnityEngine.InputSystem.Pointer.current != null && UnityEngine.InputSystem.Pointer.current.press.isPressed;

            if (isUserInteracting)
            {
                // We only apply custom elastic dampening when we are below our custom progress lock (minimumAllowedAnchoredY)
                // and minimumAllowedAnchoredY is greater than 0 (i.e. progress lock is active).
                if (minimumAllowedAnchoredY > 0.01f && anchoredPosition.y < minimumAllowedAnchoredY)
                {
                    float overshoot = minimumAllowedAnchoredY - anchoredPosition.y;
                    float softOvershoot = maxOvershoot * (1f - Mathf.Exp(-0.5f * overshoot / maxOvershoot));
                    contentRoot.anchoredPosition = new Vector2(anchoredPosition.x, minimumAllowedAnchoredY - softOvershoot);
                }
                // Custom elastic dampening when pulling past Section 1 bottom to reveal Section 0
                else if (anchoredPosition.y > bottomRestingScrollY)
                {
                    float overshoot = anchoredPosition.y - bottomRestingScrollY;
                    float softOvershoot = bottomOvershootLimit * (1f - Mathf.Exp(-0.5f * overshoot / bottomOvershootLimit));
                    contentRoot.anchoredPosition = new Vector2(anchoredPosition.x, bottomRestingScrollY + softOvershoot);
                }
                return;
            }

            // If not interacting and we are below minimumAllowedAnchoredY, we custom-lerp bounce back to minimumAllowedAnchoredY
            if (anchoredPosition.y < minimumAllowedAnchoredY)
            {
                // Safety clamp to prevent extreme overshoot
                float absoluteMinY = minimumAllowedAnchoredY - maxOvershoot;
                if (anchoredPosition.y < absoluteMinY)
                {
                    contentRoot.anchoredPosition = new Vector2(anchoredPosition.x, absoluteMinY);
                    scrollRect.velocity = new Vector2(scrollRect.velocity.x, 0f);
                    anchoredPosition.y = absoluteMinY;
                }

                float nextY = immediate || !Application.isPlaying
                    ? minimumAllowedAnchoredY
                    : Mathf.Lerp(
                        anchoredPosition.y,
                        minimumAllowedAnchoredY,
                        1f - Mathf.Exp(-progressBounceBackSpeed * Time.unscaledDeltaTime));

                if (Mathf.Abs(nextY - minimumAllowedAnchoredY) <= 0.01f)
                {
                    nextY = minimumAllowedAnchoredY;
                }

                contentRoot.anchoredPosition = new Vector2(anchoredPosition.x, nextY);
            }
            // If not interacting and we are past bottomRestingScrollY, we custom-lerp bounce back to bottomRestingScrollY (Section 1)
            else if (anchoredPosition.y > bottomRestingScrollY)
            {
                // Safety clamp to prevent extreme bottom overshoot
                float absoluteMaxY = bottomRestingScrollY + bottomOvershootLimit;
                if (anchoredPosition.y > absoluteMaxY)
                {
                    contentRoot.anchoredPosition = new Vector2(anchoredPosition.x, absoluteMaxY);
                    scrollRect.velocity = new Vector2(scrollRect.velocity.x, 0f);
                    anchoredPosition.y = absoluteMaxY;
                }

                float nextY = immediate || !Application.isPlaying
                    ? bottomRestingScrollY
                    : Mathf.Lerp(
                        anchoredPosition.y,
                        bottomRestingScrollY,
                        1f - Mathf.Exp(-progressBounceBackSpeed * Time.unscaledDeltaTime));

                if (Mathf.Abs(nextY - bottomRestingScrollY) <= 0.01f)
                {
                    nextY = bottomRestingScrollY;
                }

                contentRoot.anchoredPosition = new Vector2(anchoredPosition.x, nextY);
            }
        }

        private bool TryGetBottomLimitParameters(out float bottomRestingScrollY, out float bottomOvershootLimit)
        {
            bottomRestingScrollY = 0f;
            bottomOvershootLimit = 0f;
            if (_layout == null || _layout.Sections.Count == 0 || scrollRect == null)
            {
                return false;
            }

            RectTransform viewport = scrollRect.viewport != null ? scrollRect.viewport : transform as RectTransform;
            if (viewport == null)
            {
                return false;
            }

            float viewportHeight = viewport.rect.height;
            float contentHeight = Mathf.Max(1f, contentRoot.rect.height);
            float maxScrollY = Mathf.Max(0f, contentHeight - viewportHeight);

            bottomOvershootLimit = viewportHeight * 0.5f;
            bottomRestingScrollY = maxScrollY;

            // Find Section 1 to calculate resting position (hide Section 0 at rest)
            MapManager.MapSectionLayout section1 = null;
            for (int i = 0; i < _layout.Sections.Count; i++)
            {
                if (_layout.Sections[i].SectionIndex == 1)
                {
                    section1 = _layout.Sections[i];
                    break;
                }
            }

            if (section1 != null)
            {
                float calculatedBottomResting = contentHeight - viewportHeight - (section1.AnchoredY * _scale);
                bottomRestingScrollY = Mathf.Clamp(calculatedBottomResting, 0f, maxScrollY);
            }

            // Find Section 0 to calculate the overshoot limit (max 40% of Section 0 height)
            MapManager.MapSectionLayout section0 = null;
            for (int i = 0; i < _layout.Sections.Count; i++)
            {
                if (_layout.Sections[i].SectionIndex == 0)
                {
                    section0 = _layout.Sections[i];
                    break;
                }
            }

            if (section0 != null)
            {
                bottomOvershootLimit = (section0.SectionHeight * _scale) * 0.4f;
            }

            return true;
        }

        private bool TryGetMinimumAllowedAnchoredY(out float minimumAllowedAnchoredY)
        {
            minimumAllowedAnchoredY = 0f;
            if (_layout == null || _layout.Entries.Count == 0 || _layout.Sections.Count == 0 || scrollRect == null)
            {
                return false;
            }

            RectTransform viewport = scrollRect.viewport != null ? scrollRect.viewport : transform as RectTransform;
            if (viewport == null)
            {
                return false;
            }

            MapLevelEntry? firstLockedEntry = null;
            for (int i = 0; i < _layout.Entries.Count; i++)
            {
                if (_layout.Entries[i].State == MapLevelState.Locked)
                {
                    firstLockedEntry = _layout.Entries[i];
                    break;
                }
            }

            if (!firstLockedEntry.HasValue)
            {
                minimumAllowedAnchoredY = 0f;
                return true;
            }

            // The section containing the first locked entry is the active section the player is currently playing in.
            // The actual locked section boundary we want to restrict is the section *after* the active section.
            int activeSectionIndex = firstLockedEntry.Value.SectionIndex;
            int targetLockedSectionIndex = activeSectionIndex + 1;

            MapManager.MapSectionLayout lockedSection = null;
            for (int i = 0; i < _layout.Sections.Count; i++)
            {
                if (_layout.Sections[i].SectionIndex == targetLockedSectionIndex)
                {
                    lockedSection = _layout.Sections[i];
                    break;
                }
            }

            // If there is no next locked section (meaning the player is in the last section), they can scroll the entire map.
            if (lockedSection == null)
            {
                minimumAllowedAnchoredY = 0f;
                return true;
            }

            float viewportHeight = viewport.rect.height;
            float allowedVisibleTopY = (lockedSection.AnchoredY + (lockedSection.SectionHeight * lockedSectionRevealRatio)) * _scale;
            float contentHeight = Mathf.Max(1f, contentRoot.rect.height);
            float maxScrollY = Mathf.Max(0f, contentHeight - viewportHeight);

            minimumAllowedAnchoredY = Mathf.Clamp(contentHeight - allowedVisibleTopY, 0f, maxScrollY);
            return true;
        }

        private static List<MapLevelEntry> SliceEntries(IReadOnlyList<MapLevelEntry> entries, int startIndex, int count)
        {
            List<MapLevelEntry> result = new List<MapLevelEntry>();
            if (entries == null || count <= 0)
            {
                return result;
            }

            int safeStart = Mathf.Clamp(startIndex, 0, entries.Count);
            int safeEnd = Mathf.Clamp(startIndex + count, safeStart, entries.Count);
            for (int i = safeStart; i < safeEnd; i++)
            {
                result.Add(entries[i]);
            }

            return result;
        }

        private static void ClearChildren(RectTransform root)
        {
            if (root == null)
            {
                return;
            }

            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        private string DetermineLevelId(Button btn)
        {
            if (btn.TryGetComponent(out MapLevelNodeView nodeView) &&
                nodeView.Button != null &&
                nodeView.Button == btn)
            {
                return null;
            }

            string name = btn.name;
            if (name == "BtnLevel")
            {
                return $"{levelIdPrefix}001";
            }

            int openParen = name.IndexOf('(');
            int closeParen = name.IndexOf(')');
            if (openParen >= 0 && closeParen > openParen)
            {
                string numStr = name.Substring(openParen + 1, closeParen - openParen - 1);
                if (int.TryParse(numStr, out int indexInParen))
                {
                    int levelNumFromName = indexInParen + 1;
                    return $"{levelIdPrefix}{levelNumFromName:D3}";
                }
            }

            return null;
        }

        private void HandleLevelPressed(string levelId)
        {
            if (!GameAppFlowManager.Instance.RequestStartLevel(levelId))
            {
                Debug.LogWarning($"[MapSubScreen] Failed to request gameplay start for level '{levelId}'.", this);
            }
        }

        private bool HasRequiredScenicBindings()
        {
            if (mapConfig == null || scrollRect == null || contentRoot == null || sectionRoot == null || levelNodePrefab == null)
            {
                Debug.LogWarning(
                    $"[MapSubScreen] Scenic map binding incomplete on '{name}'. " +
                    $"mapConfig={(mapConfig != null)}, scrollRect={(scrollRect != null)}, contentRoot={(contentRoot != null)}, " +
                    $"sectionRoot={(sectionRoot != null)}, levelNodePrefab={(levelNodePrefab != null)}.",
                    this);
                return false;
            }

            if (scrollRect.viewport == null)
            {
                Debug.LogWarning($"[MapSubScreen] Scenic map requires ScrollRect.viewport to be assigned on '{name}'.", this);
                return false;
            }

            if (sectionRoot.parent != contentRoot)
            {
                Debug.LogWarning($"[MapSubScreen] Scenic map requires sectionRoot to be a child of contentRoot on '{name}'.", this);
                return false;
            }

            return true;
        }

        private void UpdateMapScale()
        {
            if (scrollRect == null || contentRoot == null || sectionRoot == null || _layout == null)
            {
                return;
            }

            RectTransform viewport = scrollRect.viewport != null ? scrollRect.viewport : transform as RectTransform;
            if (viewport == null)
            {
                return;
            }

            float viewportWidth = viewport.rect.width;
            float newScale = viewportWidth > 0.01f ? (viewportWidth / 1080f) : 1f;

            if (Mathf.Abs(_scale - newScale) > 0.001f)
            {
                _scale = newScale;
                ReapplyMapScale();
            }
        }

        private void ReapplyMapScale()
        {
            if (contentRoot == null || sectionRoot == null || _layout == null)
            {
                return;
            }

            sectionRoot.localScale = new Vector3(_scale, _scale, 1f);

            float contentHeight = _mapManager.CalculateContentHeight(_layout, mapConfig);
            contentRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight * _scale);
            sectionRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);

            ApplyProgressScrollLimit(immediate: true);
            UpdateSectionCulling();
        }

        private void OnValidate()
        {
            if (targetViewportRatio <= 0.001f)
            {
                targetViewportRatio = 0.4f;
            }
        }

        private void OnEnable()
        {
            LevelProgressRepository.OnDataChanged += HandleLevelProgressChanged;
        }

        private void OnDisable()
        {
            LevelProgressRepository.OnDataChanged -= HandleLevelProgressChanged;
        }

        private void HandleLevelProgressChanged()
        {
            if (isInitialized && HasScenicMapBinding())
            {
                TryRenderScenicMap();
            }
        }

        private void OnDestroy()
        {
            if (_isScrollSubscribed && scrollRect != null)
            {
                scrollRect.onValueChanged.RemoveListener(HandleScrollValueChanged);
            }
        }
    }
}
