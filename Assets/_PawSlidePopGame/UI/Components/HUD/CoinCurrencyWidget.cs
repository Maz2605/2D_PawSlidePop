using DG.Tweening;
using TMPro;
using _PawSlidePopGame._Scripts.Data.Events;
using _PawSlidePopGame._Scripts.Data.Events.Payloads;
using _PawSlidePopGame._Scripts.Gameplay.Meta.EconomyManager;
using _PawSlidePopGame._Scripts.Services.Ads;
using _PawSlidePopGame.Scripts.DesignPattern.ObserverPattern;
using UnityEngine;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.UI.Components.HUD
{
    [DisallowMultipleComponent]
    public sealed class CoinCurrencyWidget : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private TMP_Text amountText;
        [SerializeField] private RectTransform amountTextRoot;
        [SerializeField] private Image coinIcon;
        [SerializeField] private Button addButton;

        [Header("Format")]
        [SerializeField] private string amountFormat = "{0}";

        [Header("Count Animation")]
        [SerializeField] private bool subscribeEconomyManager = true;
        [SerializeField] private bool pollEconomyManagerValue = true;
        [SerializeField] private bool enableDevAddCoinHotkey = true;
        [SerializeField] private UnityEngine.InputSystem.Key devAddCoinKey = UnityEngine.InputSystem.Key.Digit0;
        [SerializeField] private int devAddCoinAmount = 1000;

        [Header("Rewarded Ad Test")]
        [SerializeField] private bool enableRewardedAdTestButton = true;
        [SerializeField] private RewardedAdPlacement rewardedAdPlacement = RewardedAdPlacement.CoinWidget;
        [SerializeField] private string rewardedAdContext = "coin_widget";

        [SerializeField] private bool useUnscaledTime = true;
        [SerializeField] private float countDuration = 0.35f;
        [SerializeField] private Ease countEase = Ease.OutCubic;

        [Header("Slide Animation")]
        [SerializeField] private float slideOffsetY = 18f;
        [SerializeField] private float slideOutDuration = 0.12f;
        [SerializeField] private float slideInDuration = 0.18f;
        [SerializeField] private Ease slideOutEase = Ease.InCubic;
        [SerializeField] private Ease slideInEase = Ease.OutBack;

        [Header("Feedback")]
        [SerializeField] private float iconPunchScale = 0.12f;
        [SerializeField] private float iconPunchDuration = 0.18f;
        [SerializeField] private float increaseScale = 1.08f;
        [SerializeField] private float decreaseScale = 0.94f;

        [Header("Icon Animation Settings")]
        [SerializeField] private IconIdleAnimType iconIdleAnimType = IconIdleAnimType.Floating;
        [SerializeField] private float idleDuration = 2.0f;
        [SerializeField] private float idleFloatAmount = 6f;
        [SerializeField] private float idlePulseScale = 1.06f;
        [SerializeField] private float shakeDuration = 0.5f;
        [SerializeField] private float shakeRotationStrength = 15f;
        [SerializeField] private float shakeScaleStrength = 0.15f;

        private int _displayedAmount;
        private int _targetAmount;
        private Sequence _updateSequence;
        private Tween _countTween;
        private Vector2 _amountBaseAnchoredPosition;
        private Vector3 _amountBaseScale;
        private Vector3 _iconBaseScale;
        private Vector2 _iconBaseAnchoredPosition;
        private Tween _idleTween;
        private Sequence _shakeTween;
        private bool _hasCachedBaseState;
        private bool _initialized;
        private bool _isRewardedAdShowing;

        private RectTransform AmountRoot
        {
            get
            {
                if (amountTextRoot != null)
                {
                    return amountTextRoot;
                }

                return amountText != null ? amountText.rectTransform : null;
            }
        }

        private void Awake()
        {
            CacheBaseState(force: true);
            SetupIconInteraction();
        }

        private void OnEnable()
        {
            CacheBaseState(force: false);
            RestoreBaseState();
            RegisterAddButton();
            SubscribeAdEvents();

            EconomyManager economyManager = GetEconomyManager();
            if (subscribeEconomyManager && economyManager != null)
            {
                economyManager.OnCoinsChanged -= HandleCoinsChanged;
                economyManager.OnCoinsChanged += HandleCoinsChanged;
                SetAmount(economyManager.Coins, animate: false);
            }

            StartIdleAnimation();
        }

        private void OnDisable()
        {
            EconomyManager economyManager = GetEconomyManager();
            if (economyManager != null)
            {
                economyManager.OnCoinsChanged -= HandleCoinsChanged;
            }

            UnregisterAddButton();
            UnsubscribeAdEvents();
            _isRewardedAdShowing = false;
            KillTweens();
            KillIdleAndShakeTweens();
            RestoreBaseState();
        }

        private void Start()
        {
            RefreshFromEconomy(animate: false);
        }

        private void Update()
        {
            EconomyManager economyManager = GetEconomyManager();
            if (pollEconomyManagerValue && economyManager != null && _targetAmount != economyManager.Coins)
            {
                SetAmount(economyManager.Coins, animate: _initialized);
            }

            if (!enableDevAddCoinHotkey || devAddCoinAmount <= 0 || economyManager == null)
            {
                return;
            }

            UnityEngine.InputSystem.Keyboard keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            UnityEngine.InputSystem.Controls.KeyControl keyControl = keyboard[devAddCoinKey];
            if (keyControl != null && keyControl.wasPressedThisFrame)
            {
                economyManager.AddCoins(devAddCoinAmount, "dev_hotkey");
            }
        }

        public void RefreshFromEconomy(bool animate)
        {
            EconomyManager economyManager = GetEconomyManager();
            if (economyManager != null)
            {
                SetAmount(economyManager.Coins, animate);
            }
        }

        public void SetAmount(int amount, bool animate)
        {
            amount = Mathf.Max(0, amount);
            if (!_initialized || !animate)
            {
                _displayedAmount = amount;
                _targetAmount = amount;
                RefreshText();
                _initialized = true;
                return;
            }

            PlayAmountChanged(_displayedAmount, amount);
        }

        public void SetInputEnabled(bool enabled)
        {
            if (addButton != null)
            {
                addButton.interactable = enabled && !_isRewardedAdShowing;
            }
        }

        private void RegisterAddButton()
        {
            if (addButton == null)
            {
                return;
            }

            addButton.onClick.RemoveListener(HandleAddButtonClicked);
            addButton.onClick.AddListener(HandleAddButtonClicked);
        }

        private void UnregisterAddButton()
        {
            if (addButton != null)
            {
                addButton.onClick.RemoveListener(HandleAddButtonClicked);
            }
        }

        private void HandleAddButtonClicked()
        {
            if (!enableRewardedAdTestButton || _isRewardedAdShowing)
            {
                return;
            }

            EventManager<AdsGameEvent>.Post(
                AdsGameEvent.RewardedAdRequested,
                new RewardedAdRequestPayload(rewardedAdPlacement, rewardedAdContext));
        }

        private void SubscribeAdEvents()
        {
            EventManager<AdsGameEvent>.AddListener<RewardedAdRequestPayload>(
                AdsGameEvent.RewardedAdStarted,
                HandleRewardedAdStarted);
            EventManager<AdsGameEvent>.AddListener<RewardedAdCompletedPayload>(
                AdsGameEvent.RewardedAdCompleted,
                HandleRewardedAdCompleted);
            EventManager<AdsGameEvent>.AddListener<RewardedAdFailedPayload>(
                AdsGameEvent.RewardedAdFailed,
                HandleRewardedAdFailed);
        }

        private void UnsubscribeAdEvents()
        {
            EventManager<AdsGameEvent>.RemoveListener<RewardedAdRequestPayload>(
                AdsGameEvent.RewardedAdStarted,
                HandleRewardedAdStarted);
            EventManager<AdsGameEvent>.RemoveListener<RewardedAdCompletedPayload>(
                AdsGameEvent.RewardedAdCompleted,
                HandleRewardedAdCompleted);
            EventManager<AdsGameEvent>.RemoveListener<RewardedAdFailedPayload>(
                AdsGameEvent.RewardedAdFailed,
                HandleRewardedAdFailed);
        }

        private void HandleRewardedAdStarted(RewardedAdRequestPayload payload)
        {
            if (payload.placement != rewardedAdPlacement)
            {
                return;
            }

            _isRewardedAdShowing = true;
            SetInputEnabled(false);
        }

        private void HandleRewardedAdCompleted(RewardedAdCompletedPayload payload)
        {
            if (payload.placement != rewardedAdPlacement)
            {
                return;
            }

            _isRewardedAdShowing = false;
            SetInputEnabled(true);
        }

        private void HandleRewardedAdFailed(RewardedAdFailedPayload payload)
        {
            if (payload.placement != rewardedAdPlacement)
            {
                return;
            }

            _isRewardedAdShowing = false;
            SetInputEnabled(true);
        }

        private void HandleCoinsChanged(int previousAmount, int currentAmount, string reason)
        {
            if (!_initialized)
            {
                SetAmount(currentAmount, animate: false);
                return;
            }

            PlayAmountChanged(previousAmount, currentAmount);
        }

        private EconomyManager GetEconomyManager()
        {
            return EconomyManager.Instance;
        }

        private void PlayAmountChanged(int previousAmount, int currentAmount)
        {
            currentAmount = Mathf.Max(0, currentAmount);
            if (previousAmount == currentAmount)
            {
                SetAmount(currentAmount, animate: false);
                return;
            }

            KillTweens();
            KillIdleAndShakeTweens();
            CacheBaseState(force: false);
            RestoreBaseState();

            _displayedAmount = Mathf.Max(0, previousAmount);
            _targetAmount = currentAmount;
            RefreshText();

            int direction = currentAmount > previousAmount ? 1 : -1;
            RectTransform root = AmountRoot;
            Vector2 outPosition = _amountBaseAnchoredPosition + new Vector2(0f, slideOffsetY * direction);
            Vector2 inStartPosition = _amountBaseAnchoredPosition - new Vector2(0f, slideOffsetY * direction);
            Vector3 feedbackScale = _amountBaseScale * (direction > 0 ? increaseScale : decreaseScale);

            _updateSequence = DOTween.Sequence()
                .SetUpdate(useUnscaledTime)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);

            if (root != null)
            {
                _updateSequence.Append(root.DOAnchorPos(outPosition, slideOutDuration).SetEase(slideOutEase));
                _updateSequence.Join(root.DOScale(feedbackScale, slideOutDuration).SetEase(slideOutEase));
                _updateSequence.AppendCallback(() =>
                {
                    root.anchoredPosition = inStartPosition;
                    _displayedAmount = _targetAmount;
                    RefreshText();
                });
                _updateSequence.Append(root.DOAnchorPos(_amountBaseAnchoredPosition, slideInDuration).SetEase(slideInEase));
                _updateSequence.Join(root.DOScale(_amountBaseScale, slideInDuration).SetEase(slideInEase));
            }
            else
            {
                _updateSequence.AppendInterval(slideOutDuration);
                _updateSequence.AppendCallback(() =>
                {
                    _displayedAmount = _targetAmount;
                    RefreshText();
                });
                _updateSequence.AppendInterval(slideInDuration);
            }

            _countTween = DOTween.To(() => previousAmount, value =>
                {
                    _displayedAmount = Mathf.Max(0, value);
                    RefreshText();
                }, currentAmount, countDuration)
                .SetEase(countEase)
                .SetUpdate(useUnscaledTime)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);

            if (coinIcon != null)
            {
                coinIcon.transform.localScale = _iconBaseScale;
                _updateSequence.Join(coinIcon.transform.DOPunchScale(_iconBaseScale * iconPunchScale, iconPunchDuration, 8, 0.75f)
                    .SetUpdate(useUnscaledTime));
            }

            _updateSequence.OnComplete(() =>
            {
                _displayedAmount = _targetAmount;
                RefreshText();
                RestoreBaseState();
                StartIdleAnimation();
            });
        }

        private void RefreshText()
        {
            if (amountText != null)
            {
                amountText.SetText(amountFormat, _displayedAmount);
            }
        }

        private void CacheBaseState(bool force)
        {
            if (_hasCachedBaseState && !force)
            {
                return;
            }

            RectTransform root = AmountRoot;
            if (root != null)
            {
                _amountBaseAnchoredPosition = root.anchoredPosition;
                _amountBaseScale = root.localScale;
            }

            if (coinIcon != null)
            {
                _iconBaseScale = coinIcon.transform.localScale;
                _iconBaseAnchoredPosition = coinIcon.rectTransform.anchoredPosition;
            }

            _hasCachedBaseState = true;
        }

        private void RestoreBaseState()
        {
            RectTransform root = AmountRoot;
            if (root != null)
            {
                root.anchoredPosition = _amountBaseAnchoredPosition;
                root.localScale = _amountBaseScale;
            }

            if (coinIcon != null)
            {
                coinIcon.transform.localScale = _iconBaseScale;
                coinIcon.rectTransform.anchoredPosition = _iconBaseAnchoredPosition;
                coinIcon.transform.localRotation = Quaternion.identity;
            }
        }

        private void KillTweens()
        {
            _updateSequence?.Kill();
            _updateSequence = null;
            _countTween?.Kill();
            _countTween = null;
        }

        private void KillIdleAndShakeTweens()
        {
            _idleTween?.Kill();
            _idleTween = null;
            _shakeTween?.Kill();
            _shakeTween = null;
        }

        private void SetupIconInteraction()
        {
            if (coinIcon != null)
            {
                Button button = coinIcon.GetComponent<Button>();
                if (button == null)
                {
                    button = coinIcon.gameObject.AddComponent<Button>();
                    button.transition = Selectable.Transition.None;
                }
                button.onClick.RemoveListener(PlayIconShake);
                button.onClick.AddListener(PlayIconShake);
            }
        }

        private void StartIdleAnimation()
        {
            if (coinIcon == null || iconIdleAnimType == IconIdleAnimType.None)
            {
                return;
            }

            _idleTween?.Kill();

            if (iconIdleAnimType == IconIdleAnimType.Floating)
            {
                float startY = _iconBaseAnchoredPosition.y;
                float targetY = startY + idleFloatAmount;
                coinIcon.rectTransform.anchoredPosition = new Vector2(_iconBaseAnchoredPosition.x, startY);

                _idleTween = coinIcon.rectTransform.DOAnchorPosY(targetY, idleDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(useUnscaledTime)
                    .SetLink(coinIcon.gameObject);
            }
            else if (iconIdleAnimType == IconIdleAnimType.Pulsing)
            {
                coinIcon.transform.localScale = _iconBaseScale;
                Vector3 targetScale = _iconBaseScale * idlePulseScale;

                _idleTween = coinIcon.transform.DOScale(targetScale, idleDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(useUnscaledTime)
                    .SetLink(coinIcon.gameObject);
            }
        }

        private void PlayIconShake()
        {
            if (coinIcon == null)
            {
                return;
            }

            _idleTween?.Kill();
            _idleTween = null;
            _shakeTween?.Kill();
            _shakeTween = null;

            coinIcon.transform.localScale = _iconBaseScale;
            coinIcon.rectTransform.anchoredPosition = _iconBaseAnchoredPosition;
            coinIcon.transform.localRotation = Quaternion.identity;

            _shakeTween = DOTween.Sequence()
                .SetUpdate(useUnscaledTime)
                .SetLink(coinIcon.gameObject);

            _shakeTween.Append(coinIcon.transform.DOPunchRotation(new Vector3(0f, 0f, shakeRotationStrength), shakeDuration, 10, 1f));
            _shakeTween.Join(coinIcon.transform.DOPunchScale(_iconBaseScale * shakeScaleStrength, shakeDuration, 10, 1f));

            _shakeTween.OnComplete(() =>
            {
                _shakeTween = null;
                StartIdleAnimation();
            });
        }
    }
}
