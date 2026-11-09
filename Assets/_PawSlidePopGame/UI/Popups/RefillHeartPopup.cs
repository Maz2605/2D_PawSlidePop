using System;
using _PawSlidePopGame._Scripts.UI.Base;
using _PawSlidePopGame._Scripts.Gameplay.Meta.EconomyManager;
using _PawSlidePopGame._Scripts.UI.Manager;
using _PawSlidePopGame._Scripts.Data.Events;
using _PawSlidePopGame._Scripts.Data.Events.Payloads;
using _PawSlidePopGame._Scripts.Services.Ads;
using _PawSlidePopGame.Scripts.DesignPattern.ObserverPattern;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.UI.Popups
{
    public sealed class RefillHeartPopup : BasePopup
    {
        [Header("--- UI Elements ---")]
        [SerializeField] private RectTransform animatedContent;
        [SerializeField] private RectTransform heartTransform;
        [SerializeField] private TMP_Text heartCountText;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text refillPriceText;

        [Header("--- Buttons ---")]
        [SerializeField] private Button refillButton;
        [SerializeField] private Button freeAdButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button backgroundButton;

        [Header("--- Refill Settings ---")]
        [SerializeField] private RefillHeartConfigSO refillConfig;
        [SerializeField] private int fallbackRefillPriceCoins = 150;

        private int RefillPriceCoins => refillConfig != null ? refillConfig.RefillPriceCoins : fallbackRefillPriceCoins;

        [Header("--- Animation Settings ---")]
        [SerializeField] private float showScaleDuration = 0.25f;
        [SerializeField] private float hideScaleDuration = 0.2f;
        [SerializeField] private float startScale = 0.8f;
        [SerializeField] private Ease showEase = Ease.OutBack;
        [SerializeField] private Ease hideEase = Ease.InQuad;

        [Header("--- Idle Heart Pulse Settings ---")]
        [SerializeField] private float heartPulseScale = 1.1f;
        [SerializeField] private float heartPulseDuration = 0.8f;

        [Header("--- Idle Button Float Settings ---")]
        [SerializeField] private RectTransform refillButtonTransform;
        [SerializeField] private float buttonFloatDistance = 12f;
        [SerializeField] private float buttonFloatDuration = 1.6f;

        private Sequence _showSequence;
        private Tween _heartPulseTween;
        private Tween _refillButtonFloatTween;

        private Vector2 _refillButtonBasePosition;

        private Action _onRefillClick;

        // Cache state to avoid string allocation GC in Update()
        private int _cachedHearts = -1;
        private int _cachedSecondsRemaining = -1;
        private bool _cachedInfiniteState;
        private bool _isAdShowing;

        protected override void Awake()
        {
            base.Awake();

            if (refillButtonTransform != null)
            {
                _refillButtonBasePosition = refillButtonTransform.anchoredPosition;
            }
        }

        private void Update()
        {
            if (gameObject.activeInHierarchy)
            {
                UpdateTimerDisplay();
            }
        }

        public void Setup(Action onRefillClick = null)
        {
            _onRefillClick = onRefillClick;

            // Reset cache to force refresh on Setup
            _cachedHearts = -1;
            _cachedSecondsRemaining = -1;

            if (refillPriceText != null)
            {
                refillPriceText.text = RefillPriceCoins.ToString();
            }

            UpdateTimerDisplay();
        }

        private void UpdateTimerDisplay()
        {
            HeartManager manager = HeartManager.Instance;
            if (manager == null) return;

            bool isInfinite = manager.IsInfiniteHeartsActive;
            int currentHearts = manager.Hearts;

            bool stateChanged = isInfinite != _cachedInfiniteState || currentHearts != _cachedHearts;

            if (stateChanged)
            {
                _cachedInfiniteState = isInfinite;
                _cachedHearts = currentHearts;

                // Update hearts count
                if (heartCountText != null)
                {
                    heartCountText.text = isInfinite ? "∞" : currentHearts.ToString();
                }
            }

            // Update recovery countdown
            if (timerText != null)
            {
                if (isInfinite)
                {
                    double infiniteSeconds = manager.RemainingInfiniteHeartsSeconds;
                    int intSeconds = (int)infiniteSeconds;
                    if (stateChanged || intSeconds != _cachedSecondsRemaining)
                    {
                        _cachedSecondsRemaining = intSeconds;
                        timerText.text = FormatTime(infiniteSeconds);
                    }
                }
                else if (currentHearts >= HeartManager.MaxHearts)
                {
                    if (stateChanged || _cachedSecondsRemaining != 0)
                    {
                        _cachedSecondsRemaining = 0;
                        timerText.text = "00:00:00";
                    }
                }
                else
                {
                    double secondsRemaining = manager.SecondsUntilNextHeart;
                    int intSeconds = (int)secondsRemaining;
                    if (stateChanged || intSeconds != _cachedSecondsRemaining)
                    {
                        _cachedSecondsRemaining = intSeconds;
                        timerText.text = FormatTime(secondsRemaining);
                    }
                }
            }
        }

        private string FormatTime(double totalSeconds)
        {
            int hours = (int)(totalSeconds / 3600);
            int minutes = (int)((totalSeconds % 3600) / 60);
            int seconds = (int)(totalSeconds % 60);
            return $"{hours:00}:{minutes:00}:{seconds:00}";
        }

        protected override void OnBeforeShow()
        {
            base.OnBeforeShow();

            KillActiveTweens();
            ResetLayoutState();
            BindButtons();
        }

        protected override void PlayShowAnimation()
        {
            _showSequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);

            if (animatedContent != null)
            {
                animatedContent.localScale = Vector3.one * startScale;
                _showSequence.Append(
                    animatedContent.DOScale(Vector3.one, showScaleDuration)
                        .SetEase(showEase)
                );
            }

            _showSequence.OnComplete(() =>
            {
                SetButtonsInteractable(true);
                StartIdleAnimations();
            });
        }

        protected override void PlayHideAnimation(Action onComplete)
        {
            KillActiveTweens();

            Sequence hideSequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);

            if (animatedContent != null)
            {
                hideSequence.Join(
                    animatedContent.DOScale(startScale, hideScaleDuration)
                        .SetEase(hideEase)
                );
            }

            hideSequence.Join(canvasGroup.DOFade(0f, hideScaleDuration).SetEase(hideEase));
            hideSequence.OnComplete(() => onComplete?.Invoke());
        }

        private void StartIdleAnimations()
        {
            // Heart scale pulsing
            if (heartTransform != null)
            {
                heartTransform.localScale = Vector3.one;
                _heartPulseTween = heartTransform.DOScale(Vector3.one * heartPulseScale, heartPulseDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true)
                    .SetLink(heartTransform.gameObject, LinkBehaviour.KillOnDisable);
            }

            // Refill button drift
            if (refillButtonTransform != null)
            {
                refillButtonTransform.anchoredPosition = _refillButtonBasePosition;
                _refillButtonFloatTween = refillButtonTransform.DOAnchorPosY(_refillButtonBasePosition.y + buttonFloatDistance, buttonFloatDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true)
                    .SetLink(refillButtonTransform.gameObject, LinkBehaviour.KillOnDisable);
            }
        }

        private void KillActiveTweens()
        {
            _showSequence?.Kill();
            _showSequence = null;

            _heartPulseTween?.Kill();
            _heartPulseTween = null;

            _refillButtonFloatTween?.Kill();
            _refillButtonFloatTween = null;

            if (animatedContent != null)
            {
                animatedContent.DOKill();
            }

            transform.DOKill();
        }

        private void ResetLayoutState()
        {
            if (animatedContent != null)
            {
                animatedContent.localScale = Vector3.one;
                animatedContent.localRotation = Quaternion.identity;
                animatedContent.anchoredPosition = Vector2.zero;
            }

            if (heartTransform != null)
            {
                heartTransform.localScale = Vector3.one;
            }

            if (refillButtonTransform != null)
            {
                refillButtonTransform.anchoredPosition = _refillButtonBasePosition;
            }
        }

        private void BindButtons()
        {
            SetButtonsInteractable(false);

            BindButton(refillButton, HandleRefillPressed);
            BindButton(freeAdButton, HandleFreeAdPressed);
            BindButton(closeButton, HandleClosePressed);
            BindButtonWithoutPressFx(backgroundButton, HandleClosePressed);
        }

        private void BindButtonWithoutPressFx(Button btn, Action onClickAction)
        {
            if (btn == null)
            {
                return;
            }

            btn.onClick?.RemoveAllListeners();
            btn.onClick?.AddListener(() => onClickAction?.Invoke());
        }

        private void HandleClosePressed()
        {
            Hide();
        }

        private void HandleRefillPressed()
        {
            EconomyManager economy = EconomyManager.Instance;
            HeartManager hearts = HeartManager.Instance;

            if (economy == null || hearts == null)
            {
                Hide();
                return;
            }

            // Check if hearts are already full
            if (hearts.Hearts >= HeartManager.MaxHearts)
            {
                UIManager.Instance?.ShowToast("Hearts are already full!");
                Hide();
                return;
            }

            // Check if player has enough coins
            if (economy.CanSpendCoins(RefillPriceCoins))
            {
                // Spend coins and refill hearts
                if (economy.TrySpendCoins(RefillPriceCoins, "refill_hearts"))
                {
                    int heartsToAdd = HeartManager.MaxHearts - hearts.Hearts;
                    hearts.AddHearts(heartsToAdd, allowOverfill: false);
                    UIManager.Instance?.ShowToast("Hearts refilled!");

                    _onRefillClick?.Invoke();
                    Hide();
                }
            }
            else
            {
                UIManager.Instance?.ShowToast("Not enough coins!");
            }
        }

        private void HandleFreeAdPressed()
        {
            HeartManager hearts = HeartManager.Instance;
            if (hearts == null) return;

            if (hearts.Hearts >= HeartManager.MaxHearts)
            {
                UIManager.Instance?.ShowToast("Hearts are already full!");
                return;
            }

            if (_isAdShowing) return;
            _isAdShowing = true;

            EventManager<AdsGameEvent>.Post(
                AdsGameEvent.RewardedAdRequested,
                new RewardedAdRequestPayload(RewardedAdPlacement.FreeHeart));
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

            _isAdShowing = false;
        }

        private void HandleRewardedAdCompleted(RewardedAdCompletedPayload payload)
        {
            if (payload.placement != RewardedAdPlacement.FreeHeart) return;

            _isAdShowing = false;

            HeartManager hearts = HeartManager.Instance;
            if (hearts != null)
            {
                hearts.AddHearts(1, allowOverfill: false);
                UIManager.Instance?.ShowToast("Earned 1 Heart!");
            }
        }

        private void HandleRewardedAdFailed(RewardedAdFailedPayload payload)
        {
            if (payload.placement != RewardedAdPlacement.FreeHeart) return;

            _isAdShowing = false;
            UIManager.Instance?.ShowToast("Failed to watch ad. Please try again.");
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (refillButton != null) refillButton.interactable = interactable;
            if (freeAdButton != null) freeAdButton.interactable = interactable;
            if (closeButton != null) closeButton.interactable = interactable;
            if (backgroundButton != null) backgroundButton.interactable = interactable;
        }

        private void OnDestroy()
        {
            KillActiveTweens();
        }
    }
}
