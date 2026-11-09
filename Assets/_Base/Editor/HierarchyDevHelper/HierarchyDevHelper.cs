using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MazFramework.Editor
{
    /// <summary>
    /// Adds lightweight development controls to every visible GameObject row in the Hierarchy.
    /// This script lives in an Editor folder, so it is never included in a player build.
    /// </summary>
    [InitializeOnLoad]
    internal static class HierarchyDevHelper
    {
        private const string MenuRoot = "Tools/Dev Helper/Hierarchy/";
        private const string EnabledMenu = MenuRoot + "Enabled";
        private const string ComponentIconsMenu = MenuRoot + "Show Component Icons";
        private const string SceneVisibilityMenu = MenuRoot + "Show Editor Visibility";
        private const string EnabledPreference = "MazFramework.HierarchyDevHelper.Enabled";
        private const string ComponentIconsPreference = "MazFramework.HierarchyDevHelper.ComponentIcons";
        private const string SceneVisibilityPreference = "MazFramework.HierarchyDevHelper.SceneVisibility";

        private const float ControlSize = 16f;
        private const float ControlGap = 2f;
        private const float HierarchyIndentWidth = 14f;
        private const float ActiveToggleRightOffset = 4f;
        private const float MinimumHierarchyWidth = 125f;
        private const float FullIconDisplayWidth = 260f;
        private const float TwoIconDisplayWidth = 190f;
        private const float MinimumChildBadgeWidth = 18f;
        private const float ChildBadgeHorizontalPadding = 6f;
        private const float ComponentIconOverlap = 1f;
        private const int MaxVisibleComponentIcons = 3;
        private const int MaxCachedItems = 4096;

        private static readonly Dictionary<int, HierarchyItemInfo> ItemCache =
            new Dictionary<int, HierarchyItemInfo>(256);
        private static readonly Dictionary<Type, Texture> ComponentIconCache =
            new Dictionary<Type, Texture>(64);

        private static readonly List<Component> ComponentBuffer = new List<Component>(16);
        private static readonly List<GameObject> GameObjectBuffer = new List<GameObject>(64);
        private static readonly HashSet<int> GameObjectIdBuffer = new HashSet<int>();
        private static readonly StringBuilder TooltipBuilder = new StringBuilder(192);

        private static GUIStyle _badgeStyle;
        private static GUIStyle _infoButtonStyle;
        private static GUIStyle _iconButtonStyle;
        private static GUIContent _visibleContent;
        private static GUIContent _hiddenContent;
        private static GUIContent _warningContent;
        private static GUIContent _activeContent;
        private static GUIContent _inheritedInactiveContent;
        private static bool _repaintScheduled;

        private static bool IsEnabled
        {
            get => EditorPrefs.GetBool(EnabledPreference, true);
            set => EditorPrefs.SetBool(EnabledPreference, value);
        }

        private static bool ShowComponentIcons
        {
            get => EditorPrefs.GetBool(ComponentIconsPreference, true);
            set => EditorPrefs.SetBool(ComponentIconsPreference, value);
        }

        private static bool ShowSceneVisibility
        {
            get => EditorPrefs.GetBool(SceneVisibilityPreference, true);
            set => EditorPrefs.SetBool(SceneVisibilityPreference, value);
        }

        static HierarchyDevHelper()
        {
            MigratePreference(EnabledPreference, "3DMatchFactory.HierarchyDevHelper.Enabled");
            MigratePreference(ComponentIconsPreference, "3DMatchFactory.HierarchyDevHelper.ComponentIcons");
            MigratePreference(SceneVisibilityPreference, "3DMatchFactory.HierarchyDevHelper.SceneVisibility");

            EditorApplication.hierarchyWindowItemOnGUI += DrawHierarchyItem;
            EditorApplication.hierarchyChanged += InvalidateCache;
            EditorApplication.projectChanged += InvalidateCache;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            Undo.undoRedoPerformed += InvalidateCache;
            ObjectChangeEvents.changesPublished += OnObjectChangesPublished;
            SceneVisibilityManager.visibilityChanged += ScheduleHierarchyRepaint;
        }

        private static void MigratePreference(string preference, string legacyPreference)
        {
            if (!EditorPrefs.HasKey(preference) && EditorPrefs.HasKey(legacyPreference))
            {
                EditorPrefs.SetBool(preference, EditorPrefs.GetBool(legacyPreference, true));
            }
        }

        [MenuItem(EnabledMenu, priority = 100)]
        private static void ToggleEnabled()
        {
            IsEnabled = !IsEnabled;
            InvalidateCache();
        }

        [MenuItem(EnabledMenu, true)]
        private static bool ValidateEnabled()
        {
            Menu.SetChecked(EnabledMenu, IsEnabled);
            return true;
        }

        [MenuItem(ComponentIconsMenu, priority = 101)]
        private static void ToggleComponentIcons()
        {
            ShowComponentIcons = !ShowComponentIcons;
            InvalidateCache();
        }

        [MenuItem(ComponentIconsMenu, true)]
        private static bool ValidateComponentIcons()
        {
            Menu.SetChecked(ComponentIconsMenu, ShowComponentIcons);
            return true;
        }

        [MenuItem(SceneVisibilityMenu, priority = 102)]
        private static void ToggleSceneVisibility()
        {
            ShowSceneVisibility = !ShowSceneVisibility;
            ScheduleHierarchyRepaint();
        }

        [MenuItem(SceneVisibilityMenu, true)]
        private static bool ValidateSceneVisibility()
        {
            Menu.SetChecked(SceneVisibilityMenu, ShowSceneVisibility);
            return true;
        }

        private static void DrawHierarchyItem(int instanceId, Rect rowRect)
        {
            if (!IsEnabled || rowRect.width < MinimumHierarchyWidth)
            {
                return;
            }

            GameObject gameObject = EditorUtility.EntityIdToObject(instanceId) as GameObject;
            if (gameObject == null || EditorUtility.IsPersistent(gameObject))
            {
                return;
            }

            EnsureStyles();

            if (!ItemCache.TryGetValue(instanceId, out HierarchyItemInfo itemInfo))
            {
                if (ItemCache.Count >= MaxCachedItems)
                {
                    ItemCache.Clear();
                }

                itemInfo = BuildItemInfo(gameObject);
                ItemCache.Add(instanceId, itemInfo);
            }

            float controlY = rowRect.y + Mathf.Floor((rowRect.height - ControlSize) * 0.5f);
            float right = rowRect.xMax - ControlGap;

            // Follow the item's indentation so the toggles also communicate hierarchy
            // depth. Reserve Unity's foldout slot and a small gap before the icon.
            float activeToggleX = Mathf.Max(
                0f,
                rowRect.x
                - HierarchyIndentWidth
                - ControlSize
                - ControlGap
                + ActiveToggleRightOffset);
            Rect activeRect = new Rect(
                activeToggleX,
                controlY,
                ControlSize,
                ControlSize);

            bool inheritedInactive = gameObject.activeSelf && !gameObject.activeInHierarchy;
            Color previousColor = GUI.color;
            if (inheritedInactive)
            {
                GUI.color = new Color(1f, 0.72f, 0.25f, 1f);
            }

            EditorGUI.BeginChangeCheck();
            bool isActive = EditorGUI.Toggle(
                activeRect,
                inheritedInactive ? _inheritedInactiveContent : _activeContent,
                gameObject.activeSelf);
            if (EditorGUI.EndChangeCheck())
            {
                ApplyActiveState(gameObject, isActive, Event.current.alt);
            }
            GUI.color = previousColor;

            Rect infoRect = new Rect(right - ControlSize, activeRect.y, ControlSize, ControlSize);
            right = infoRect.xMin - ControlGap;
            if (GUI.Button(infoRect, itemInfo.InfoContent, _infoButtonStyle))
            {
                PopupWindow.Show(infoRect, new HierarchyInfoPopup(gameObject));
            }

            if (ShowSceneVisibility)
            {
                bool isHidden = SceneVisibilityManager.instance.IsHidden(gameObject, false);
                Rect visibilityRect = new Rect(right - ControlSize, activeRect.y, ControlSize, ControlSize);
                right = visibilityRect.xMin - ControlGap;
                if (GUI.Button(visibilityRect, isHidden ? _hiddenContent : _visibleContent, _iconButtonStyle))
                {
                    ApplySceneVisibility(gameObject, !isHidden, Event.current.alt);
                }
            }

            if (itemInfo.HasMissingScript)
            {
                Rect warningRect = new Rect(right - ControlSize, activeRect.y, ControlSize, ControlSize);
                right = warningRect.xMin - ControlGap;
                GUI.Label(warningRect, _warningContent);
            }

            if (itemInfo.ChildCount > 0)
            {
                Rect childRect = new Rect(right - itemInfo.ChildBadgeWidth, activeRect.y, itemInfo.ChildBadgeWidth, ControlSize);
                right = childRect.xMin - ControlGap;
                GUI.Label(childRect, itemInfo.ChildContent, _badgeStyle);
            }

            if (!ShowComponentIcons)
            {
                return;
            }

            int visibleIconCount = rowRect.width >= FullIconDisplayWidth
                ? itemInfo.ComponentContents.Length
                : Mathf.Min(
                    itemInfo.ComponentContents.Length,
                    rowRect.width >= TwoIconDisplayWidth ? 2 : 1);

            for (int index = visibleIconCount - 1; index >= 0; index--)
            {
                Rect iconRect = new Rect(right - ControlSize, activeRect.y, ControlSize, ControlSize);
                right = iconRect.xMin + ComponentIconOverlap;
                GUI.Label(iconRect, itemInfo.ComponentContents[index]);
            }
        }

        private static HierarchyItemInfo BuildItemInfo(GameObject gameObject)
        {
            ComponentBuffer.Clear();
            gameObject.GetComponents(ComponentBuffer);

            int validComponentCount = 0;
            bool hasMissingScript = false;
            for (int index = 1; index < ComponentBuffer.Count; index++)
            {
                if (ComponentBuffer[index] == null)
                {
                    hasMissingScript = true;
                }
                else
                {
                    validComponentCount++;
                }
            }

            int displayCount = Mathf.Min(validComponentCount, MaxVisibleComponentIcons);
            GUIContent[] componentContents = new GUIContent[displayCount];

            TooltipBuilder.Clear();
            TooltipBuilder.Append(gameObject.name);
            TooltipBuilder.Append("\n\nComponents:");

            int displayIndex = 0;
            for (int index = 1; index < ComponentBuffer.Count; index++)
            {
                Component component = ComponentBuffer[index];
                string componentName = component == null
                    ? "Missing Script"
                    : ObjectNames.NicifyVariableName(component.GetType().Name);

                TooltipBuilder.Append("\n• ");
                TooltipBuilder.Append(componentName);

                if (component == null || displayIndex >= displayCount)
                {
                    continue;
                }

                Texture icon = GetComponentIcon(component);
                componentContents[displayIndex] = new GUIContent(icon, componentName);
                displayIndex++;
            }

            if (ComponentBuffer.Count == 1)
            {
                TooltipBuilder.Append("\n• Transform only");
            }

            int childCount = gameObject.transform.childCount;
            TooltipBuilder.Append("\n\nDirect children: ");
            TooltipBuilder.Append(childCount);

            string childCountText = childCount.ToString();
            GUIContent childContent = new GUIContent(childCountText, $"{childCount} direct child GameObject(s)");
            float childBadgeWidth = Mathf.Max(
                MinimumChildBadgeWidth,
                _badgeStyle.CalcSize(childContent).x + ChildBadgeHorizontalPadding);
            GUIContent infoContent = new GUIContent("i", TooltipBuilder.ToString());

            ComponentBuffer.Clear();
            return new HierarchyItemInfo(
                componentContents,
                childContent,
                infoContent,
                childBadgeWidth,
                childCount,
                hasMissingScript);
        }

        private static Texture GetComponentIcon(Component component)
        {
            Type componentType = component.GetType();
            if (ComponentIconCache.TryGetValue(componentType, out Texture icon))
            {
                return icon;
            }

            icon = EditorGUIUtility.ObjectContent(null, componentType).image;
            if (icon == null)
            {
                icon = AssetPreview.GetMiniThumbnail(component);
            }

            ComponentIconCache[componentType] = icon;
            return icon;
        }

        private static void EnsureStyles()
        {
            if (_badgeStyle != null)
            {
                return;
            }

            _badgeStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 9,
                clipping = TextClipping.Clip,
                normal =
                {
                    textColor = EditorGUIUtility.isProSkin
                        ? new Color(0.72f, 0.72f, 0.72f)
                        : new Color(0.32f, 0.32f, 0.32f)
                }
            };

            _infoButtonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 9,
                padding = new RectOffset(0, 0, 0, 0)
            };

            _iconButtonStyle = new GUIStyle(GUIStyle.none)
            {
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(0, 0, 0, 0)
            };

            _visibleContent = CreateIconContent(
                "scenevis_visible",
                "Visible in Scene view. Click to hide; Alt-click includes descendants.",
                "V");
            _hiddenContent = CreateIconContent(
                "scenevis_hidden",
                "Hidden in Scene view only. Click to show; Alt-click includes descendants.",
                "H");
            _warningContent = new GUIContent(
                EditorGUIUtility.IconContent("console.warnicon.sml").image,
                "Missing Script detected");
            _activeContent = new GUIContent(
                string.Empty,
                "Toggle active state. Applies to selected GameObjects; Alt-click includes descendants.");
            _inheritedInactiveContent = new GUIContent(
                string.Empty,
                "Active Self is enabled, but a parent is inactive. Alt-click includes descendants.");
        }

        private static GUIContent CreateIconContent(string iconName, string tooltip, string fallbackText)
        {
            Texture image = EditorGUIUtility.IconContent(iconName).image;
            return image != null
                ? new GUIContent(image, tooltip)
                : new GUIContent(fallbackText, tooltip);
        }

        private static void ApplyActiveState(GameObject source, bool isActive, bool includeDescendants)
        {
            CollectInteractionTargets(source, includeDescendants);
            Undo.RecordObjects(
                GameObjectBuffer.ToArray(),
                isActive ? "Enable GameObjects" : "Disable GameObjects");
            for (int index = 0; index < GameObjectBuffer.Count; index++)
            {
                GameObject target = GameObjectBuffer[index];
                if (target.activeSelf == isActive)
                {
                    continue;
                }

                target.SetActive(isActive);
                EditorUtility.SetDirty(target);
            }

            ClearInteractionBuffers();
            InvalidateCache();
        }

        private static void ApplySceneVisibility(GameObject source, bool hide, bool includeDescendants)
        {
            CollectInteractionTargets(source, false);
            GameObject[] targets = GameObjectBuffer.ToArray();
            if (hide)
            {
                SceneVisibilityManager.instance.Hide(targets, includeDescendants);
            }
            else
            {
                SceneVisibilityManager.instance.Show(targets, includeDescendants);
            }

            ClearInteractionBuffers();
            ScheduleHierarchyRepaint();
        }

        private static void CollectInteractionTargets(GameObject source, bool includeDescendants)
        {
            ClearInteractionBuffers();

            GameObject[] selectedObjects = Selection.gameObjects;
            bool sourceIsSelected = Array.IndexOf(selectedObjects, source) >= 0;

            if (sourceIsSelected && selectedObjects.Length > 1)
            {
                foreach (GameObject selectedObject in selectedObjects)
                {
                    AddTarget(selectedObject, includeDescendants);
                }
            }
            else
            {
                AddTarget(source, includeDescendants);
            }
        }

        private static void ClearInteractionBuffers()
        {
            GameObjectBuffer.Clear();
            GameObjectIdBuffer.Clear();
        }

        private static void AddTarget(GameObject gameObject, bool includeDescendants)
        {
            if (gameObject == null || !GameObjectIdBuffer.Add(gameObject.GetInstanceID()))
            {
                return;
            }

            GameObjectBuffer.Add(gameObject);
            if (!includeDescendants)
            {
                return;
            }

            // Iterate instead of recursing so very deep hierarchies cannot overflow the stack.
            int traversalIndex = GameObjectBuffer.Count - 1;
            while (traversalIndex < GameObjectBuffer.Count)
            {
                Transform transform = GameObjectBuffer[traversalIndex].transform;
                for (int childIndex = 0; childIndex < transform.childCount; childIndex++)
                {
                    GameObject child = transform.GetChild(childIndex).gameObject;
                    if (GameObjectIdBuffer.Add(child.GetInstanceID()))
                    {
                        GameObjectBuffer.Add(child);
                    }
                }

                traversalIndex++;
            }
        }

        private static void OnObjectChangesPublished(ref ObjectChangeEventStream stream)
        {
            // Ignore frequent property-only changes (for example moving a Transform).
            // The cached data only depends on hierarchy/component structure.
            for (int index = 0; index < stream.length; index++)
            {
                switch (stream.GetEventType(index))
                {
                    case ObjectChangeKind.ChangeGameObjectStructure:
                        stream.GetChangeGameObjectStructureEvent(
                            index,
                            out ChangeGameObjectStructureEventArgs structureArgs);
                        ItemCache.Remove(structureArgs.instanceId);
                        ScheduleHierarchyRepaint();
                        break;

                    case ObjectChangeKind.ChangeGameObjectStructureHierarchy:
                        stream.GetChangeGameObjectStructureHierarchyEvent(
                            index,
                            out ChangeGameObjectStructureHierarchyEventArgs hierarchyArgs);
                        ItemCache.Remove(hierarchyArgs.instanceId);
                        ScheduleHierarchyRepaint();
                        break;

                    case ObjectChangeKind.UpdatePrefabInstances:
                        InvalidateCache();
                        return;
                }
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange _)
        {
            InvalidateCache();
        }

        [MenuItem(MenuRoot + "Refresh Cache", priority = 120)]
        private static void InvalidateCache()
        {
            ItemCache.Clear();
            ScheduleHierarchyRepaint();
        }

        private static void ScheduleHierarchyRepaint()
        {
            if (_repaintScheduled)
            {
                return;
            }

            _repaintScheduled = true;
            EditorApplication.delayCall += RepaintHierarchy;
        }

        private static void RepaintHierarchy()
        {
            _repaintScheduled = false;
            EditorApplication.RepaintHierarchyWindow();
        }

        private readonly struct HierarchyItemInfo
        {
            public readonly GUIContent[] ComponentContents;
            public readonly GUIContent ChildContent;
            public readonly GUIContent InfoContent;
            public readonly float ChildBadgeWidth;
            public readonly int ChildCount;
            public readonly bool HasMissingScript;

            public HierarchyItemInfo(
                GUIContent[] componentContents,
                GUIContent childContent,
                GUIContent infoContent,
                float childBadgeWidth,
                int childCount,
                bool hasMissingScript)
            {
                ComponentContents = componentContents;
                ChildContent = childContent;
                InfoContent = infoContent;
                ChildBadgeWidth = childBadgeWidth;
                ChildCount = childCount;
                HasMissingScript = hasMissingScript;
            }
        }

        private sealed class HierarchyInfoPopup : PopupWindowContent
        {
            private const float PopupWidth = 340f;
            private const float PopupMinimumHeight = 210f;
            private const float PopupMaximumHeight = 430f;
            private const float PopupBaseHeight = 125f;
            private const float StatusAreaHeight = 24f;
            private const float StatusBadgeHeight = 18f;
            private const float ItemHeight = 20f;
            private const float ItemHeightWithSpacing = 22f;
            private const float ActiveLabelWidth = 58f;
            private const float ComponentToggleWidth = 18f;
            private const float NonToggleIndent = 22f;
            private const float HeaderSpacing = 4f;
            private const float SectionSpacing = 8f;
            private const float DisabledComponentAlpha = 0.55f;
            private const int MaxRowsBeforeScrolling = 6;

            private readonly GameObject _gameObject;
            private readonly Component[] _components;
            private readonly GUIContent _titleContent;
            private readonly List<GUIContent> _statusBadges = new List<GUIContent>(6);
            private readonly Dictionary<int, int> _missingReferencesByComponent =
                new Dictionary<int, int>(8);
            private readonly int _componentCount;
            private readonly int _missingScriptCount;
            private readonly int _missingReferenceCount;
            private readonly int _directChildCount;
            private readonly int _activeDirectChildCount;
            private readonly int _totalDescendantCount;
            private Vector2 _scrollPosition;

            public HierarchyInfoPopup(GameObject gameObject)
            {
                _gameObject = gameObject;
                _titleContent = new GUIContent(
                    gameObject.name,
                    EditorGUIUtility.IconContent("GameObject Icon").image);
                _directChildCount = gameObject.transform.childCount;

                _components = gameObject.GetComponents<Component>();
                string context = DetectContext(gameObject, _components);
                bool isPooled = false;
                int missingReferenceCount = 0;
                for (int index = 1; index < _components.Length; index++)
                {
                    Component component = _components[index];
                    if (component == null)
                    {
                        _missingScriptCount++;
                    }
                    else
                    {
                        _componentCount++;
                        int componentMissingReferences = CountMissingReferences(component);
                        if (componentMissingReferences > 0)
                        {
                            _missingReferencesByComponent[component.GetInstanceID()] = componentMissingReferences;
                            missingReferenceCount += componentMissingReferences;
                        }

                        isPooled |= IsPoolingComponent(component);
                    }
                }

                _missingReferenceCount = missingReferenceCount;

                CountChildren(
                    gameObject.transform,
                    out int activeDirectChildCount,
                    out int totalDescendantCount);
                _activeDirectChildCount = activeDirectChildCount;
                _totalDescendantCount = totalDescendantCount;

                BuildStatusBadges(context, isPooled);
            }

            public override Vector2 GetWindowSize()
            {
                int visibleRows = Mathf.Min(
                        _componentCount + _missingScriptCount,
                        MaxRowsBeforeScrolling)
                    + Mathf.Min(_directChildCount, MaxRowsBeforeScrolling);
                float statusHeight = _statusBadges.Count > 0 ? StatusAreaHeight : 0f;
                float height = Mathf.Clamp(
                    PopupBaseHeight + statusHeight + visibleRows * ItemHeightWithSpacing,
                    PopupMinimumHeight,
                    PopupMaximumHeight);
                return new Vector2(PopupWidth, height);
            }

            public override void OnGUI(Rect rect)
            {
                if (_gameObject == null)
                {
                    EditorGUILayout.HelpBox("This GameObject no longer exists.", MessageType.Info);
                    return;
                }

                DrawHeader();
                DrawStatusBadges();
                EditorGUILayout.Space(HeaderSpacing);

                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
                DrawComponents();
                EditorGUILayout.Space(SectionSpacing);
                DrawChildren();
                EditorGUILayout.EndScrollView();
            }

            private void DrawHeader()
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField(_titleContent, EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();

                    EditorGUI.BeginChangeCheck();
                    bool active = EditorGUILayout.ToggleLeft(
                        "Active",
                        _gameObject.activeSelf,
                        GUILayout.Width(ActiveLabelWidth));
                    if (EditorGUI.EndChangeCheck())
                    {
                        ApplyActiveState(_gameObject, active, Event.current.alt);
                    }
                }
            }

            private void DrawStatusBadges()
            {
                if (_statusBadges.Count == 0)
                {
                    return;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int index = 0; index < _statusBadges.Count; index++)
                    {
                        GUILayout.Label(
                            _statusBadges[index],
                            EditorStyles.miniButton,
                            GUILayout.ExpandWidth(false),
                            GUILayout.Height(StatusBadgeHeight));
                    }

                    GUILayout.FlexibleSpace();
                }
            }

            private void DrawComponents()
            {
                string componentHeader = _missingReferenceCount > 0
                    ? $"Components ({_componentCount})  •  {_missingReferenceCount} missing reference(s)"
                    : $"Components ({_componentCount})";
                EditorGUILayout.LabelField(componentHeader, EditorStyles.boldLabel);

                if (_componentCount == 0 && _missingScriptCount == 0)
                {
                    EditorGUILayout.LabelField("Transform only", EditorStyles.miniLabel);
                }

                for (int index = 1; index < _components.Length; index++)
                {
                    Component component = _components[index];
                    if (component == null)
                    {
                        EditorGUILayout.HelpBox("Missing Script", MessageType.Warning);
                        continue;
                    }

                    string componentName = ObjectNames.NicifyVariableName(component.GetType().Name);
                    componentName += GetComponentSummary(component);
                    if (_missingReferencesByComponent.TryGetValue(component.GetInstanceID(), out int missingCount))
                    {
                        componentName += $"  •  Missing {missingCount}";
                    }

                    GUIContent content = new GUIContent(componentName, GetComponentIcon(component));
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (TryGetComponentEnabled(component, out bool componentEnabled))
                        {
                            EditorGUI.BeginChangeCheck();
                            bool newEnabled = GUILayout.Toggle(
                                componentEnabled,
                                GUIContent.none,
                                GUILayout.Width(ComponentToggleWidth),
                                GUILayout.Height(ItemHeight));
                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObject(component, newEnabled ? "Enable Component" : "Disable Component");
                                SetComponentEnabled(component, newEnabled);
                                EditorUtility.SetDirty(component);
                            }

                            Color previousColor = GUI.color;
                            if (!componentEnabled)
                            {
                                GUI.color = new Color(
                                    previousColor.r,
                                    previousColor.g,
                                    previousColor.b,
                                    DisabledComponentAlpha);
                            }

                            DrawComponentButton(component, content);
                            GUI.color = previousColor;
                        }
                        else
                        {
                            GUILayout.Space(NonToggleIndent);
                            DrawComponentButton(component, content);
                        }
                    }
                }
            }

            private void DrawChildren()
            {
                Transform transform = _gameObject.transform;
                string childHeader = $"Children ({_activeDirectChildCount}/{_directChildCount} active)";
                if (_totalDescendantCount > _directChildCount)
                {
                    childHeader += $"  •  {_totalDescendantCount} total";
                }

                EditorGUILayout.LabelField(childHeader, EditorStyles.boldLabel);

                if (transform.childCount == 0)
                {
                    EditorGUILayout.LabelField("None", EditorStyles.miniLabel);
                    return;
                }

                for (int index = 0; index < transform.childCount; index++)
                {
                    GameObject child = transform.GetChild(index).gameObject;
                    string childLabel = child.activeSelf ? child.name : $"{child.name}  (inactive)";
                    GUIContent content = new GUIContent(childLabel, EditorGUIUtility.IconContent("GameObject Icon").image);
                    if (GUILayout.Button(content, EditorStyles.objectField, GUILayout.Height(ItemHeight)))
                    {
                        Selection.activeGameObject = child;
                        EditorGUIUtility.PingObject(child);
                        editorWindow.Close();
                    }
                }
            }

            private void DrawComponentButton(Component component, GUIContent content)
            {
                if (!GUILayout.Button(content, EditorStyles.objectField, GUILayout.Height(ItemHeight)))
                {
                    return;
                }

                Selection.activeObject = component;
                EditorGUIUtility.PingObject(component);
                editorWindow.Close();
            }

            private void BuildStatusBadges(string context, bool isPooled)
            {
                _statusBadges.Add(new GUIContent(context));

                if (_gameObject.activeSelf && !_gameObject.activeInHierarchy)
                {
                    _statusBadges.Add(new GUIContent("Parent Off", "A parent GameObject is inactive."));
                }

                if (SceneVisibilityManager.instance.IsHidden(_gameObject, false))
                {
                    _statusBadges.Add(new GUIContent("Hidden", "Hidden in the Scene view only."));
                }

                PrefabInstanceStatus prefabStatus = PrefabUtility.GetPrefabInstanceStatus(_gameObject);
                if (prefabStatus != PrefabInstanceStatus.NotAPrefab)
                {
                    bool hasOverrides = prefabStatus == PrefabInstanceStatus.Connected
                        && PrefabUtility.HasPrefabInstanceAnyOverrides(_gameObject, false);
                    GameObject prefabSource = PrefabUtility.GetCorrespondingObjectFromSource(_gameObject);
                    string prefabLabel = hasOverrides ? "Prefab Override" : "Prefab";
                    string prefabTooltip = prefabSource != null
                        ? $"Source: {prefabSource.name}"
                        : $"Prefab status: {prefabStatus}";
                    _statusBadges.Add(new GUIContent(prefabLabel, prefabTooltip));
                }

                if (_gameObject.layer != 0)
                {
                    string layerName = LayerMask.LayerToName(_gameObject.layer);
                    _statusBadges.Add(new GUIContent(
                        string.IsNullOrEmpty(layerName) ? $"Layer {_gameObject.layer}" : layerName,
                        "GameObject layer"));
                }

                if (_gameObject.tag != "Untagged")
                {
                    _statusBadges.Add(new GUIContent(_gameObject.tag, "GameObject tag"));
                }

                if (isPooled)
                {
                    _statusBadges.Add(new GUIContent("Pooled"));
                }

                int problemCount = _missingScriptCount + _missingReferenceCount;
                if (problemCount > 0)
                {
                    _statusBadges.Add(new GUIContent(
                        $"Missing {problemCount}",
                        _warningContent.image,
                        $"{_missingScriptCount} missing script(s), {_missingReferenceCount} missing reference(s)"));
                }
            }

            private static string DetectContext(GameObject gameObject, IReadOnlyList<Component> components)
            {
                bool hasPuzzleMarker = false;
                bool hasPhysics = false;
                bool hasRenderer = false;
                bool hasEffect = false;
                bool hasUi = gameObject.transform is RectTransform;

                for (int index = 1; index < components.Count; index++)
                {
                    Component component = components[index];
                    if (component == null)
                    {
                        continue;
                    }

                    Type type = component.GetType();
                    string typeName = type.Name;
                    string typeNamespace = type.Namespace ?? string.Empty;
                    hasPhysics |= component is Collider || component is Collider2D
                        || component is Rigidbody || component is Rigidbody2D;
                    hasRenderer |= component is Renderer;
                    hasEffect |= component is ParticleSystem || component is TrailRenderer
                        || component is LineRenderer || component is AudioSource;
                    hasUi |= component is Canvas || component is CanvasGroup
                        || typeNamespace.StartsWith("UnityEngine.UI", StringComparison.Ordinal);
                    hasPuzzleMarker |= typeName != "PooledItem" && ContainsPuzzleKeyword(typeName);
                }

                if (hasUi)
                {
                    return "UI";
                }

                if (hasPuzzleMarker || (hasPhysics && hasRenderer))
                {
                    return "Puzzle";
                }

                if (hasEffect)
                {
                    return "Effect";
                }

                return gameObject.transform.childCount > 0 ? "Container" : "Object";
            }

            private static bool ContainsPuzzleKeyword(string typeName)
            {
                return typeName.IndexOf("Puzzle", StringComparison.OrdinalIgnoreCase) >= 0
                    || typeName.IndexOf("Match", StringComparison.OrdinalIgnoreCase) >= 0
                    || typeName.IndexOf("Piece", StringComparison.OrdinalIgnoreCase) >= 0
                    || typeName.IndexOf("Item", StringComparison.OrdinalIgnoreCase) >= 0
                    || typeName.IndexOf("Board", StringComparison.OrdinalIgnoreCase) >= 0;
            }

            private static bool IsPoolingComponent(Component component)
            {
                Type type = component.GetType();
                if (type.Name == "PooledItem")
                {
                    return true;
                }

                Type[] interfaces = type.GetInterfaces();
                for (int index = 0; index < interfaces.Length; index++)
                {
                    if (interfaces[index].Name == "IPoolable")
                    {
                        return true;
                    }
                }

                return false;
            }

            private static int CountMissingReferences(Component component)
            {
                int missingCount = 0;
                try
                {
                    using SerializedObject serializedObject = new SerializedObject(component);
                    SerializedProperty property = serializedObject.GetIterator();
                    while (property.NextVisible(true))
                    {
                        if (property.propertyPath == "m_Script"
                            || property.propertyType != SerializedPropertyType.ObjectReference)
                        {
                            continue;
                        }

                        if (property.objectReferenceValue == null
                            && property.objectReferenceInstanceIDValue != 0)
                        {
                            missingCount++;
                        }
                    }
                }
                catch (Exception)
                {
                    // Some native or third-party components cannot be safely serialized for inspection.
                }

                return missingCount;
            }

            private static void CountChildren(
                Transform root,
                out int activeDirectChildCount,
                out int totalDescendantCount)
            {
                activeDirectChildCount = 0;
                totalDescendantCount = 0;
                Stack<Transform> pending = new Stack<Transform>();

                for (int index = 0; index < root.childCount; index++)
                {
                    Transform child = root.GetChild(index);
                    if (child.gameObject.activeSelf)
                    {
                        activeDirectChildCount++;
                    }

                    pending.Push(child);
                }

                while (pending.Count > 0)
                {
                    Transform current = pending.Pop();
                    totalDescendantCount++;
                    for (int index = 0; index < current.childCount; index++)
                    {
                        pending.Push(current.GetChild(index));
                    }
                }
            }

            private static bool TryGetComponentEnabled(Component component, out bool isEnabled)
            {
                switch (component)
                {
                    case Collider2D collider2D:
                        isEnabled = collider2D.enabled;
                        return true;
                    case Behaviour behaviour:
                        isEnabled = behaviour.enabled;
                        return true;
                    case Renderer renderer:
                        isEnabled = renderer.enabled;
                        return true;
                    case Collider collider:
                        isEnabled = collider.enabled;
                        return true;
                    default:
                        isEnabled = true;
                        return false;
                }
            }

            private static void SetComponentEnabled(Component component, bool isEnabled)
            {
                switch (component)
                {
                    case Collider2D collider2D:
                        collider2D.enabled = isEnabled;
                        break;
                    case Behaviour behaviour:
                        behaviour.enabled = isEnabled;
                        break;
                    case Renderer renderer:
                        renderer.enabled = isEnabled;
                        break;
                    case Collider collider:
                        collider.enabled = isEnabled;
                        break;
                }
            }

            private static string GetComponentSummary(Component component)
            {
                switch (component)
                {
                    case Collider collider when collider.isTrigger:
                        return "  •  Trigger";
                    case Collider2D collider2D when collider2D.isTrigger:
                        return "  •  Trigger";
                    case Rigidbody rigidbody:
                        return rigidbody.isKinematic ? "  •  Kinematic" : "  •  Dynamic";
                    case Rigidbody2D rigidbody2D:
                        return $"  •  {rigidbody2D.bodyType}";
                    case CanvasGroup canvasGroup:
                        return $"  •  Alpha {canvasGroup.alpha:0.##}"
                            + (canvasGroup.blocksRaycasts ? "  •  Raycasts" : string.Empty);
                    case Canvas canvas:
                        return $"  •  Order {canvas.sortingOrder}";
                    case ParticleSystem particleSystem:
                        return particleSystem.main.loop ? "  •  Loop" : string.Empty;
                    case AudioSource audioSource when audioSource.loop:
                        return "  •  Loop";
                    default:
                        return string.Empty;
                }
            }
        }
    }
}
