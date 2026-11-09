using System;
using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Core.System.GameFlow;
using _PawSlidePopGame._Scripts.Feature.Match3.Boosters;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;
using _PawSlidePopGame._Scripts.Feature.Match3.Flow;
using _PawSlidePopGame._Scripts.Gameplay.Meta.Inventory;
using _PawSlidePopGame._Scripts.Gameplay.Meta.EconomyManager;
using _PawSlidePopGame._Scripts.UI.Base;
using _PawSlidePopGame._Scripts.UI.Components.HUD;
using _PawSlidePopGame._Scripts.UI.Manager;
using _PawSlidePopGame._Scripts.Data.Events;
using _PawSlidePopGame._Scripts.Data.Events.Payloads;
using _PawSlidePopGame._Scripts.Services.Ads;
using _PawSlidePopGame._Scripts.Services.Analytics;
using _PawSlidePopGame.Scripts.DesignPattern.ObserverPattern;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.UI.Popups
{
    public sealed class LevelIntroductionPopup : BasePopup
    {
        [Serializable]
        public class TargetItemViewReference
        {
            public GameObject root;
            public Image iconImage;
            public TMP_Text countText;
        }

        [Header("--- Level Intro Elements ---")]
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private Button btnClose;
        [SerializeField] private Button btnPlay;
        [SerializeField] private Button btnBackground;

        [Header("--- Target Elements ---")]
        [SerializeField] private List<TargetItemViewReference> targetItemViews = new List<TargetItemViewReference>();

        [Header("--- Booster Elements ---")]
        [SerializeField] private BoosterWidget boosterWidget;

        [Header("--- Animation Settings ---")]
        [SerializeField] private RectTransform animatedContent;
        [SerializeField] private RectTransform headlineRoot;
        [SerializeField] private float moveOffsetY = 200f;
        [SerializeField] private float moveDuration = 0.4f;
        [SerializeField] private Ease showEase = Ease.OutBack;
        [SerializeField] private Ease hideEase = Ease.InBack;

        [Header("--- Casual Idle Settings ---")]
        [SerializeField] private float pulseScale = 1.06f;
        [SerializeField] private float pulseDuration = 0.8f;
        [SerializeField] private float bobbingAmount = 8f;
        [SerializeField] private float bobbingDuration = 1.5f;

        private Vector2 _contentOriginalAnchoredPosition;
        private Vector2 _headlineOriginalAnchoredPosition;
        private string _levelId;
        private Match3LevelData _levelData;
        private Action<IReadOnlyList<BoosterDefinitionSO>> _onPlayCallback;

        private readonly HashSet<string> _selectedBoosterIds = new HashSet<string>();
        private BoosterButtonView[] _boosterViews;
        private BoosterDefinitionSO _pendingAdBooster;
        private bool _isAdShowing;

        // Tweens cache for casual animations
        private Tween _playBtnPulseTween;
        private Tween _headlineBobbingTween;
        private readonly List<Tween> _staggeredTweens = new List<Tween>();
        private readonly List<int> _targetFinalCounts = new List<int>();
        private readonly List<Tween> _targetBobbingTweens = new List<Tween>();
        private readonly List<Tween> _boosterBobbingTweens = new List<Tween>();

        protected override void Awake()
        {
            base.Awake();

            if (animatedContent != null)
            {
                _contentOriginalAnchoredPosition = animatedContent.anchoredPosition;
            }

            if (headlineRoot != null)
            {
                _headlineOriginalAnchoredPosition = headlineRoot.anchoredPosition;
            }
        }

        public void Setup(string levelId, Match3LevelData levelData, Action<IReadOnlyList<BoosterDefinitionSO>> onPlayCallback)
        {
            _levelId = levelId;
            _levelData = levelData;
            _onPlayCallback = onPlayCallback;
            _selectedBoosterIds.Clear();

            // Setup level title text
            if (levelText != null)
            {
                levelText.text = $"Level {levelData.DisplayLevelNumber}";
            }

            // Fill target items
            PopulateTargets();
        }

        private void OnEnable()
        {
            EventManager<AdsGameEvent>.AddListener<RewardedAdCompletedPayload>(
                AdsGameEvent.RewardedAdCompleted,
                HandleRewardedAdCompleted);
            EventManager<AdsGameEvent>.AddListener<RewardedAdFailedPayload>(
                AdsGameEvent.RewardedAdFailed,
                HandleRewardedAdFailed);
        }

        private void OnDisable()
        {
            EventManager<AdsGameEvent>.RemoveListener<RewardedAdCompletedPayload>(
                AdsGameEvent.RewardedAdCompleted,
                HandleRewardedAdCompleted);
            EventManager<AdsGameEvent>.RemoveListener<RewardedAdFailedPayload>(
                AdsGameEvent.RewardedAdFailed,
                HandleRewardedAdFailed);

            _pendingAdBooster = null;
            _isAdShowing = false;
        }

        protected override void OnBeforeShow()
        {
            base.OnBeforeShow();

            KillActiveTweens();
            ResetLayoutState();
            PrepareElementsForShow();

            BindButtons();

            if (boosterWidget != null)
            {
                boosterWidget.SetInteractionMode(BoosterWidget.BoosterWidgetInteractionMode.SelectionOnly);
                boosterWidget.OnBoosterClicked -= HandleBoosterClicked;
                boosterWidget.OnBoosterClicked += HandleBoosterClicked;
            }

            // Refresh pre-level booster checkboxes
            _boosterViews = null;
            RefreshBoosterCheckboxes();
        }

        protected override void PlayShowAnimation()
        {
            if (animatedContent != null)
            {
                // Main popup body scales and pops in
                animatedContent.localScale = Vector3.zero;
                animatedContent.DOScale(Vector3.one, moveDuration)
                    .SetEase(showEase)
                    .SetUpdate(true)
                    .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                    .OnComplete(() =>
                    {
                        SetupButtonsInteractable(true);
                        StartIdleAnimations();
                    });

                // Also fade in background / content position slightly
                animatedContent.anchoredPosition = _contentOriginalAnchoredPosition - new Vector2(0f, 50f);
                animatedContent.DOAnchorPos(_contentOriginalAnchoredPosition, moveDuration)
                    .SetEase(Ease.OutCubic)
                    .SetUpdate(true)
                    .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            }
            else
            {
                SetupButtonsInteractable(true);
                StartIdleAnimations();
            }

            // Trigger Staggered entry animation for child elements (Casual game style)
            PlayStaggeredEntryAnimations();
        }

        protected override void PlayHideAnimation(Action onComplete)
        {
            KillActiveTweens();

            if (animatedContent != null)
            {
                // Scale down and fade out popup body
                animatedContent.DOScale(Vector3.zero, moveDuration * 0.75f)
                    .SetEase(hideEase)
                    .SetUpdate(true)
                    .SetLink(gameObject, LinkBehaviour.KillOnDisable);

                animatedContent.DOAnchorPos(_contentOriginalAnchoredPosition - new Vector2(0f, moveOffsetY), moveDuration * 0.75f)
                    .SetEase(Ease.InCubic)
                    .SetUpdate(true)
                    .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                    .OnComplete(() => onComplete?.Invoke());
            }
            else
            {
                onComplete?.Invoke();
            }
        }

        private void PopulateTargets()
        {
            if (_levelData == null) return;

            _targetFinalCounts.Clear();

            var validTargets = _levelData.GetValidTargets();
            var levelManager = Match3LevelManager.Instance;
            var tileDb = levelManager != null ? levelManager.TileDatabase : null;

            for (int i = 0; i < targetItemViews.Count; i++)
            {
                var view = targetItemViews[i];
                if (view == null || view.root == null) continue;

                if (i < validTargets.Count && tileDb != null)
                {
                    var targetData = validTargets[i];
                    var contentDef = tileDb.GetContentDefinition(targetData.tileId);

                    if (contentDef != null)
                    {
                        view.root.transform.parent.gameObject.SetActive(true);
                        view.root.SetActive(true);
                        if (view.iconImage != null)
                        {
                            view.iconImage.sprite = contentDef.Icon;
                            view.iconImage.enabled = contentDef.Icon != null;
                        }
                        if (view.countText != null)
                        {
                            view.countText.text = $"x{targetData.requiredCount}";
                            view.countText.ForceMeshUpdate();
                        }

                        _targetFinalCounts.Add(targetData.requiredCount);
                        AdjustTargetItemLayout(view);

                        // Add interactive click feedback
                        var btn = view.root.transform.parent.gameObject.GetComponent<Button>();
                        if (btn == null)
                        {
                            btn = view.root.transform.parent.gameObject.AddComponent<Button>();
                            btn.transition = Selectable.Transition.None;
                        }
                        btn.onClick.RemoveAllListeners();
                        Transform targetTransform = view.root.transform.parent;
                        btn.onClick.AddListener(() => PlayTargetClickAnimation(targetTransform));
                    }
                    else
                    {
                        view.root.transform.parent.gameObject.SetActive(false);
                    }
                }
                else
                {
                    view.root.transform.parent.gameObject.SetActive(false);
                }
            }

            // Force parent Targets layout to rebuild
            if (targetItemViews.Count > 0 && targetItemViews[0]?.root != null)
            {
                var parentTargets = targetItemViews[0].root.transform.parent.parent as RectTransform;
                if (parentTargets != null)
                {
                    var parentLayoutGroup = parentTargets.GetComponent<HorizontalOrVerticalLayoutGroup>();
                    if (parentLayoutGroup != null)
                    {
                        parentLayoutGroup.spacing = 30f;
                        parentLayoutGroup.childScaleWidth = false;
                        parentLayoutGroup.childScaleHeight = false;
                    }
                    LayoutRebuilder.ForceRebuildLayoutImmediate(parentTargets);
                }
            }
        }

        private void AdjustTargetItemLayout(TargetItemViewReference view)
        {
            if (view == null || view.root == null) return;

            // view.root is actually the Icon GameObject. The parent is the TargetItem.
            RectTransform targetItemRt = view.root.transform.parent as RectTransform;
            if (targetItemRt == null) return;

            RectTransform iconRt = view.iconImage != null ? view.iconImage.rectTransform : null;
            RectTransform textRt = view.countText != null ? view.countText.rectTransform : null;
            RectTransform shadowRt = targetItemRt.Find("Shadow") as RectTransform;

            float iconWidth = iconRt != null ? iconRt.sizeDelta.x : 100f;
            float gap = 12f;
            float textWidth = textRt != null ? view.countText.preferredWidth : 0f;

            float totalWidth = iconWidth + (textWidth > 0f ? (gap + textWidth) : 0f);
            targetItemRt.sizeDelta = new Vector2(totalWidth, targetItemRt.sizeDelta.y);

            // Left-align icon and shadow inside TargetItem
            if (iconRt != null)
            {
                iconRt.anchorMin = new Vector2(0f, 0.5f);
                iconRt.anchorMax = new Vector2(0f, 0.5f);
                iconRt.pivot = new Vector2(0f, 0.5f);
                iconRt.anchoredPosition = new Vector2(0f, 0f);
                iconRt.sizeDelta = new Vector2(iconWidth, 100f);
            }
            if (shadowRt != null)
            {
                shadowRt.anchorMin = new Vector2(0f, 0.5f);
                shadowRt.anchorMax = new Vector2(0f, 0.5f);
                shadowRt.pivot = new Vector2(0f, 0.5f);
                shadowRt.anchoredPosition = new Vector2(0f, 0f);
                shadowRt.sizeDelta = new Vector2(iconWidth, 100f);
            }

            // Place text next to icon inside TargetItem
            if (textRt != null)
            {
                textRt.anchorMin = new Vector2(0f, 0.5f);
                textRt.anchorMax = new Vector2(0f, 0.5f);
                textRt.pivot = new Vector2(0f, 0.5f);
                textRt.anchoredPosition = new Vector2(iconWidth + gap, 0f);
                textRt.sizeDelta = new Vector2(textWidth + 10f, textRt.sizeDelta.y);
            }
        }

        private void PlayStaggeredEntryAnimations()
        {
            float delay = 0.15f;
            int activeTargetIndex = 0;

            // 1. Staggered scale pop for valid target items
            for (int i = 0; i < targetItemViews.Count; i++)
            {
                var view = targetItemViews[i];
                if (view != null && view.root != null && view.root.activeSelf)
                {
                    Transform targetItemTransform = view.root.transform.parent;
                    targetItemTransform.localScale = Vector3.zero;
                    Tween t = targetItemTransform.DOScale(Vector3.one, 0.35f)
                        .SetEase(Ease.OutBack)
                        .SetDelay(delay)
                        .SetUpdate(true)
                        .SetLink(targetItemTransform.gameObject, LinkBehaviour.KillOnDisable);

                    _staggeredTweens.Add(t);

                    // Counting animation starting with same delay
                    if (view.countText != null && activeTargetIndex < _targetFinalCounts.Count)
                    {
                        int finalCount = _targetFinalCounts[activeTargetIndex];
                        int startCount = 0;
                        view.countText.text = "x0";

                        float animDelay = delay;
                        var txt = view.countText;

                        Tween countTween = DOTween.To(() => startCount, val =>
                        {
                            startCount = val;
                            txt.text = $"x{val}";
                        }, finalCount, 0.5f)
                        .SetDelay(animDelay)
                        .SetEase(Ease.OutQuad)
                        .SetUpdate(true)
                        .SetLink(targetItemTransform.gameObject, LinkBehaviour.KillOnDisable);

                        _staggeredTweens.Add(countTween);
                    }

                    activeTargetIndex++;
                    delay += 0.1f;
                }
            }

            // 2. Scale pop for boosters list (staggered for each button)
            if (boosterWidget != null)
            {
                var boosterLayout = boosterWidget.GetComponentInChildren<HorizontalOrVerticalLayoutGroup>(true);
                if (boosterLayout != null)
                {
                    boosterLayout.childScaleWidth = false;
                    boosterLayout.childScaleHeight = false;
                }

                if (_boosterViews == null || _boosterViews.Length == 0)
                {
                    _boosterViews = boosterWidget.GetComponentsInChildren<BoosterButtonView>(true);
                }

                if (_boosterViews != null && _boosterViews.Length > 0)
                {
                    boosterWidget.transform.localScale = Vector3.one;

                    for (int i = 0; i < _boosterViews.Length; i++)
                    {
                        var view = _boosterViews[i];
                        if (view != null && view.gameObject.activeSelf)
                        {
                            view.transform.localScale = Vector3.zero;
                            Tween t = view.transform.DOScale(Vector3.one, 0.35f)
                                .SetEase(Ease.OutBack)
                                .SetDelay(delay)
                                .SetUpdate(true)
                                .SetLink(view.gameObject, LinkBehaviour.KillOnDisable);

                            _staggeredTweens.Add(t);
                            delay += 0.08f;
                        }
                    }
                }
                else
                {
                    boosterWidget.transform.localScale = Vector3.zero;
                    Tween t = boosterWidget.transform.DOScale(Vector3.one, 0.4f)
                        .SetEase(Ease.OutBack)
                        .SetDelay(delay)
                        .SetUpdate(true)
                        .SetLink(boosterWidget.gameObject, LinkBehaviour.KillOnDisable);

                    _staggeredTweens.Add(t);
                }
            }
        }

        private void StartIdleAnimations()
        {
            // Disable Targets layout group to allow bobbing/floating positions without conflict
            if (targetItemViews.Count > 0 && targetItemViews[0]?.root != null)
            {
                var parentTargets = targetItemViews[0].root.transform.parent.parent as RectTransform;
                if (parentTargets != null)
                {
                    var parentLayoutGroup = parentTargets.GetComponent<HorizontalOrVerticalLayoutGroup>();
                    if (parentLayoutGroup != null)
                    {
                        parentLayoutGroup.enabled = false;
                    }
                }
            }

            // Disable Booster layout group to allow bobbing/floating positions without conflict
            if (boosterWidget != null)
            {
                var boosterLayout = boosterWidget.GetComponentInChildren<HorizontalOrVerticalLayoutGroup>(true);
                if (boosterLayout != null)
                {
                    boosterLayout.enabled = false;
                }
            }

            // Play Button pulsing (phồng xẹp casual)
            if (btnPlay != null)
            {
                btnPlay.transform.localScale = Vector3.one;
                _playBtnPulseTween = btnPlay.transform.DOScale(Vector3.one * pulseScale, pulseDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true)
                    .SetLink(btnPlay.gameObject, LinkBehaviour.KillOnDisable);
            }

            // Headline / Bow / Bunny floating (bồng bềnh nhẹ)
            if (headlineRoot != null)
            {
                _headlineBobbingTween = headlineRoot.DOAnchorPos(_headlineOriginalAnchoredPosition + new Vector2(0f, bobbingAmount), bobbingDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true)
                    .SetLink(headlineRoot.gameObject, LinkBehaviour.KillOnDisable);
            }

            // Target items floating (trôi nổi nhẹ)
            for (int i = 0; i < targetItemViews.Count; i++)
            {
                var view = targetItemViews[i];
                if (view != null && view.root != null && view.root.activeSelf)
                {
                    RectTransform rt = view.root.transform.parent as RectTransform;
                    if (rt != null)
                    {
                        Vector2 targetPos = rt.anchoredPosition + new Vector2(0f, 6f);
                        float randomOffset = i * 0.25f;

                        Tween t = rt.DOAnchorPos(targetPos, 1.2f)
                            .SetEase(Ease.InOutSine)
                            .SetLoops(-1, LoopType.Yoyo)
                            .SetUpdate(true)
                            .SetDelay(randomOffset)
                            .SetLink(rt.gameObject, LinkBehaviour.KillOnDisable);

                        _targetBobbingTweens.Add(t);
                    }
                }
            }

            // Booster items floating (trôi nổi nhẹ)
            if (_boosterViews != null)
            {
                int activeBoosterCount = 0;
                for (int i = 0; i < _boosterViews.Length; i++)
                {
                    var view = _boosterViews[i];
                    if (view != null && view.gameObject.activeSelf)
                    {
                        RectTransform rt = view.GetComponent<RectTransform>();
                        if (rt != null)
                        {
                            Vector2 targetPos = rt.anchoredPosition + new Vector2(0f, 6f);
                            float randomOffset = activeBoosterCount * 0.2f + 0.12f;

                            Tween t = rt.DOAnchorPos(targetPos, 1.3f)
                                .SetEase(Ease.InOutSine)
                                .SetLoops(-1, LoopType.Yoyo)
                                .SetUpdate(true)
                                .SetDelay(randomOffset)
                                .SetLink(rt.gameObject, LinkBehaviour.KillOnDisable);

                            _boosterBobbingTweens.Add(t);
                            activeBoosterCount++;
                        }
                    }
                }
            }
        }

        private void BindButtons()
        {
            SetupButtonsInteractable(false);
            BindButton(btnPlay, HandlePlayPressed);
            BindButton(btnClose, HandleClosePressed);
            BindButton(btnBackground, HandleClosePressed);
        }

        private void HandlePlayPressed()
        {
            List<BoosterDefinitionSO> selectedBoosters = GetSelectedPreLevelBoosters();

            if (!CanConsumeSelectedBoosters(selectedBoosters))
            {
                return;
            }

            if (!HeartManager.Instance.CanSpendHeart())
            {
                UIManager.Instance?.ShowToast("Not enough hearts!");
                UIManager.Instance?.ShowPopup<RefillHeartPopup>();
                return;
            }

            for (int i = 0; i < selectedBoosters.Count; i++)
            {
                BoosterDefinitionSO booster = selectedBoosters[i];
                BoosterInventory.Instance.TryConsumeBooster(booster, "pre_level_booster_selected");

                if (booster != null)
                {
                    FirebaseService.LogBoosterUsed(booster.BoosterId, "pre_level", _levelId);
                }
            }

            Hide();
            _onPlayCallback?.Invoke(selectedBoosters);
        }

        private void HandleClosePressed()
        {
            Hide();
        }

        private void HandleBoosterClicked(BoosterDefinitionSO definition)
        {
            if (definition == null) return;

            if (definition.UsagePhase != BoosterUsagePhase.PreLevel)
            {
                UIManager.Instance?.ShowToast("This booster can only be used in gameplay.");
                return;
            }

            int currentLevelNumber = _levelData != null ? _levelData.DisplayLevelNumber : 1;
            if (!definition.IsUnlockedAtLevel(currentLevelNumber))
            {
                UIManager.Instance?.ShowToast("Booster is locked!");
                return;
            }

            int count = BoosterInventory.Instance != null ? BoosterInventory.Instance.GetCount(definition) : 0;
            bool hasBooster = count > 0 || definition.IsUnlimitedForDev;

            if (!hasBooster)
            {
                if (_isAdShowing) return;

                _pendingAdBooster = definition;
                _isAdShowing = true;

                EventManager<AdsGameEvent>.Post(
                    AdsGameEvent.RewardedAdRequested,
                    new RewardedAdRequestPayload(RewardedAdPlacement.FreeBooster, definition.BoosterId));
                return;
            }

            if (_selectedBoosterIds.Contains(definition.BoosterId))
            {
                _selectedBoosterIds.Remove(definition.BoosterId);
            }
            else
            {
                _selectedBoosterIds.Add(definition.BoosterId);
            }

            RefreshBoosterCheckboxes();
        }

        private void RefreshBoosterCheckboxes()
        {
            if (boosterWidget == null) return;

            if (_boosterViews == null || _boosterViews.Length == 0)
            {
                _boosterViews = boosterWidget.GetComponentsInChildren<BoosterButtonView>(true);
            }

            int activeCount = 0;
            foreach (var view in _boosterViews)
            {
                if (view != null && view.Definition != null)
                {
                    int currentLevelNumber = _levelData != null ? _levelData.DisplayLevelNumber : 1;
                    bool isUnlocked = view.Definition.IsUnlockedAtLevel(currentLevelNumber) && view.Definition.UsagePhase == BoosterUsagePhase.PreLevel;
                    int count = BoosterInventory.Instance != null ? BoosterInventory.Instance.GetCount(view.Definition) : 0;
                    bool showCheckbox = isUnlocked && (count > 0 || view.Definition.IsUnlimitedForDev);
                    bool isSelected = _selectedBoosterIds.Contains(view.Definition.BoosterId);
                    view.SetCheckboxState(showCheckbox, isSelected, false);

                    if (activeCount < 4)
                    {
                        view.gameObject.SetActive(true);
                        activeCount++;
                    }
                    else
                    {
                        view.gameObject.SetActive(false);
                    }
                }
            }
        }

        private List<BoosterDefinitionSO> GetSelectedPreLevelBoosters()
        {
            List<BoosterDefinitionSO> selectedBoosters = new List<BoosterDefinitionSO>();
            if (boosterWidget == null || _selectedBoosterIds.Count == 0)
            {
                return selectedBoosters;
            }

            if (_boosterViews == null || _boosterViews.Length == 0)
            {
                _boosterViews = boosterWidget.GetComponentsInChildren<BoosterButtonView>(true);
            }

            for (int i = 0; i < _boosterViews.Length; i++)
            {
                BoosterButtonView view = _boosterViews[i];
                BoosterDefinitionSO definition = view != null ? view.Definition : null;

                if (definition == null ||
                    definition.UsagePhase != BoosterUsagePhase.PreLevel ||
                    !_selectedBoosterIds.Contains(definition.BoosterId))
                {
                    continue;
                }

                selectedBoosters.Add(definition);
            }

            return selectedBoosters;
        }

        private bool CanConsumeSelectedBoosters(IReadOnlyList<BoosterDefinitionSO> selectedBoosters)
        {
            if (selectedBoosters == null || selectedBoosters.Count == 0)
            {
                return true;
            }

            BoosterInventory inventory = BoosterInventory.Instance;
            for (int i = 0; i < selectedBoosters.Count; i++)
            {
                BoosterDefinitionSO definition = selectedBoosters[i];
                if (definition == null)
                {
                    continue;
                }

                int currentLevelNumber = _levelData != null ? _levelData.DisplayLevelNumber : 1;
                if (!definition.IsUnlockedAtLevel(currentLevelNumber))
                {
                    UIManager.Instance?.ShowToast("Booster is locked!");
                    return false;
                }

                if (!inventory.HasBooster(definition))
                {
                    UIManager.Instance?.ShowToast($"Not enough {definition.DisplayName}!");
                    return false;
                }
            }

            return true;
        }

        private void SetupButtonsInteractable(bool enable)
        {
            if (btnPlay != null) btnPlay.interactable = enable;
            if (btnClose != null) btnClose.interactable = enable;
            if (btnBackground != null) btnBackground.interactable = enable;
        }

        private void PrepareElementsForShow()
        {
            if (animatedContent != null)
            {
                animatedContent.localScale = Vector3.zero;
            }
        }

        private void ResetLayoutState()
        {
            if (animatedContent != null)
            {
                animatedContent.anchoredPosition = _contentOriginalAnchoredPosition;
                animatedContent.localScale = Vector3.one;
                animatedContent.localRotation = Quaternion.identity;
            }

            if (headlineRoot != null)
            {
                headlineRoot.anchoredPosition = _headlineOriginalAnchoredPosition;
            }

            if (btnPlay != null)
            {
                btnPlay.transform.localScale = Vector3.one;
            }

            if (targetItemViews != null)
            {
                foreach (var view in targetItemViews)
                {
                    if (view != null && view.root != null)
                    {
                        view.root.transform.parent.localScale = Vector3.one;
                    }
                }
            }

            if (_boosterViews != null)
            {
                foreach (var view in _boosterViews)
                {
                    if (view != null)
                    {
                        view.transform.localScale = Vector3.one;
                    }
                }
            }

            // Re-enable layout group so next setup calculation is correct
            if (targetItemViews.Count > 0 && targetItemViews[0]?.root != null)
            {
                var parentTargets = targetItemViews[0].root.transform.parent.parent as RectTransform;
                if (parentTargets != null)
                {
                    var parentLayoutGroup = parentTargets.GetComponent<HorizontalOrVerticalLayoutGroup>();
                    if (parentLayoutGroup != null)
                    {
                        parentLayoutGroup.enabled = true;
                    }
                }
            }

            // Re-enable booster layout group
            if (boosterWidget != null)
            {
                var boosterLayout = boosterWidget.GetComponentInChildren<HorizontalOrVerticalLayoutGroup>(true);
                if (boosterLayout != null)
                {
                    boosterLayout.enabled = true;
                }
            }
        }

        private void KillActiveTweens()
        {
            _playBtnPulseTween?.Kill();
            _playBtnPulseTween = null;

            _headlineBobbingTween?.Kill();
            _headlineBobbingTween = null;

            for (int i = 0; i < _staggeredTweens.Count; i++)
            {
                _staggeredTweens[i]?.Kill();
            }
            _staggeredTweens.Clear();

            for (int i = 0; i < _targetBobbingTweens.Count; i++)
            {
                _targetBobbingTweens[i]?.Kill();
            }
            _targetBobbingTweens.Clear();

            for (int i = 0; i < _boosterBobbingTweens.Count; i++)
            {
                _boosterBobbingTweens[i]?.Kill();
            }
            _boosterBobbingTweens.Clear();

            animatedContent?.DOKill();
            transform.DOKill();
        }

        private void PlayTargetClickAnimation(Transform targetTransform)
        {
            if (targetTransform == null) return;

            targetTransform.DOKill(true);

            targetTransform.DOScale(new Vector3(1.15f, 0.85f, 1f), 0.12f)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    targetTransform.DOScale(Vector3.one, 0.25f)
                        .SetEase(Ease.OutElastic)
                        .SetUpdate(true);
                });
        }

        private void HandleRewardedAdCompleted(RewardedAdCompletedPayload payload)
        {
            if (payload.placement != RewardedAdPlacement.FreeBooster) return;

            if (_pendingAdBooster == null || payload.context != _pendingAdBooster.BoosterId) return;

            if (BoosterInventory.Instance != null)
            {
                BoosterInventory.Instance.AddBooster(_pendingAdBooster, 1, "rewarded_ad_pre_level");
            }

            _selectedBoosterIds.Add(_pendingAdBooster.BoosterId);
            UIManager.Instance?.ShowToast($"Earned 1 {_pendingAdBooster.DisplayName}!");

            _isAdShowing = false;
            _pendingAdBooster = null;

            RefreshBoosterCheckboxes();
            if (boosterWidget != null)
            {
                boosterWidget.RefreshAll();
            }
        }

        private void HandleRewardedAdFailed(RewardedAdFailedPayload payload)
        {
            if (payload.placement != RewardedAdPlacement.FreeBooster) return;

            if (_pendingAdBooster == null || payload.context != _pendingAdBooster.BoosterId) return;

            UIManager.Instance?.ShowToast("Failed to watch ad. Please try again.");

            _isAdShowing = false;
            _pendingAdBooster = null;
        }

        private void OnDestroy()
        {
            KillActiveTweens();

            if (boosterWidget != null)
            {
                boosterWidget.OnBoosterClicked -= HandleBoosterClicked;
            }
        }
    }
}
