using System;
using DG.Tweening;
using TMPro;
using _PawSlidePopGame._Scripts.Gameplay.Meta.EconomyManager;
using _PawSlidePopGame._Scripts.UI.Manager;
using _PawSlidePopGame._Scripts.UI.Popups;
using UnityEngine;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.UI.Components.HUD
{
    [DisallowMultipleComponent]
    public sealed class HeartWidget : MonoBehaviour
    {
        private struct IconState
        {
            public Vector3 Scale;
            public Vector2 Position;
            public Quaternion Rotation;

            public void Cache(Image icon)
            {
                if (icon != null)
                {
                    Scale = icon.transform.localScale;
                    Position = icon.rectTransform.anchoredPosition;
                    Rotation = icon.transform.localRotation;
                }
            }

            public void Restore(Image icon)
            {
                if (icon != null)
                {
                    icon.transform.localScale = Scale;
                    icon.rectTransform.anchoredPosition = Position;
                    icon.transform.localRotation = Rotation;
                }
            }
        }

        [Header("Refs")]
        [SerializeField] private TMP_Text amountText;
        [SerializeField] private RectTransform amountTextRoot;
        [SerializeField] private Image heartIcon;
        [SerializeField] private Image infiniteHeartIcon;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button addButton;

        [Header("Format")]
        [SerializeField] private string amountFormat = "{0}";
        [SerializeField] private string fullText = "Full";

        [Header("Obsolete Count Anim Refs (Kept for Serialization)")]
        [SerializeField] private bool subscribeHeartManager = true;
        [SerializeField] private bool pollHeartManagerValue = true;
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

        // Private State
        private int _displayedAmount;
        private int _targetAmount;
        private bool _hasCachedBaseState;
        private bool _initialized;
        private bool _isAnimating;
        private bool? _lastInfiniteState;
        private bool? _lastButtonInteractableState;

        private Vector2 _amountBaseAnchoredPosition;
        private Vector3 _amountBaseScale;
        private IconState _normalIconState;
        private IconState _infiniteIconState;

        // Tweens & Sequences
        private Sequence _animationSequence;
        private Sequence _shakeSequence;
        private Sequence _transitionSequence;
        private Tween _idleTween;
        private string _lastStatusText;

        private Image ActiveHeartIcon => 
            (GetHeartManager() != null && GetHeartManager().IsInfiniteHeartsActive && infiniteHeartIcon != null)
                ? infiniteHeartIcon
                : heartIcon;

        private RectTransform AmountRoot => 
            amountTextRoot != null ? amountTextRoot : (amountText != null ? amountText.rectTransform : null);

        #region Unity Lifecycle

        private void Awake()
        {
            CacheBaseState(force: true);
            SetupIconInteraction();
        }

        private void OnEnable()
        {
            CacheBaseState(force: false);
            RestoreBaseState();
            _lastButtonInteractableState = null;

            HeartManager manager = GetHeartManager();
            if (subscribeHeartManager && manager != null)
            {
                manager.OnHeartsChanged -= HandleHeartsChanged;
                manager.OnHeartsChanged += HandleHeartsChanged;
                SetAmount(manager.Hearts, animate: false);
            }

            if (addButton != null)
            {
                addButton.onClick.RemoveListener(HandleAddHeartsClicked);
                addButton.onClick.AddListener(HandleAddHeartsClicked);
            }

            StartIdleAnimation();
            UpdateButtonState(manager);
        }

        private void OnDisable()
        {
            HeartManager manager = GetHeartManager();
            if (manager != null)
            {
                manager.OnHeartsChanged -= HandleHeartsChanged;
            }

            if (addButton != null)
            {
                addButton.onClick.RemoveListener(HandleAddHeartsClicked);
            }

            _lastButtonInteractableState = null;
            _lastStatusText = null;
            KillAllTweens();
            RestoreBaseState();
            _isAnimating = false;
        }

        private void Start()
        {
            RefreshFromManager(animate: false);
        }

        private void Update()
        {
            HeartManager manager = GetHeartManager();
            if (manager == null) return;

            bool isInfinite = manager.IsInfiniteHeartsActive;
            UpdateIconVisibility(isInfinite);

            if (pollHeartManagerValue && !_isAnimating)
            {
                int currentHearts = manager.Hearts;
                if (_targetAmount != currentHearts)
                {
                    SetAmount(currentHearts, animate: _initialized);
                }
            }

            UpdateStatusText(manager, isInfinite);
            UpdateButtonState(manager);
        }

        #endregion

        #region Public API

        public void RefreshFromManager(bool animate)
        {
            HeartManager manager = GetHeartManager();
            if (manager != null)
            {
                SetAmount(manager.Hearts, animate);
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
                UpdateButtonState(GetHeartManager());
                return;
            }

            PlayAmountChanged(_displayedAmount, amount);
            UpdateButtonState(GetHeartManager());
        }

        public void SetInputEnabled(bool enabled)
        {
            if (addButton != null)
            {
                addButton.interactable = enabled;
            }
        }

        #endregion

        #region Private Handlers & Helpers

        private void HandleHeartsChanged(int previousAmount, int currentAmount)
        {
            if (!_initialized)
            {
                SetAmount(currentAmount, animate: false);
                return;
            }

            PlayAmountChanged(previousAmount, currentAmount);
            UpdateButtonState(GetHeartManager());
        }

        private HeartManager GetHeartManager()
        {
            return HeartManager.Instance;
        }

        private IconState GetIconBaseState(Image icon)
        {
            return icon == infiniteHeartIcon ? _infiniteIconState : _normalIconState;
        }

        private void RefreshText()
        {
            if (amountText != null)
            {
                amountText.SetText(amountFormat, _displayedAmount);
            }
        }

        private void UpdateIconVisibility(bool isInfinite)
        {
            if (_lastInfiniteState == isInfinite) return;

            bool isFirstTime = _lastInfiniteState == null;
            _lastInfiniteState = isInfinite;

            if (isFirstTime)
            {
                if (heartIcon != null) heartIcon.gameObject.SetActive(!isInfinite);
                if (infiniteHeartIcon != null) infiniteHeartIcon.gameObject.SetActive(isInfinite);
                RestoreBaseState();
                StartIdleAnimation();
                return;
            }

            PlayTransitionAnimation(isInfinite);
        }

        private void UpdateStatusText(HeartManager manager, bool isInfinite)
        {
            if (statusText == null) return;

            string targetText = string.Empty;
            if (isInfinite)
            {
                double infiniteSeconds = manager.RemainingInfiniteHeartsSeconds;
                int hours = (int)(infiniteSeconds / 3600);
                int minutes = (int)((infiniteSeconds % 3600) / 60);
                int seconds = (int)(infiniteSeconds % 60);
                targetText = hours > 0 ? $"{hours}:{minutes:00}:{seconds:00}" : $"{minutes:00}:{seconds:00}";
            }
            else if (manager.Hearts >= HeartManager.MaxHearts)
            {
                targetText = fullText;
            }
            else
            {
                double secondsRemaining = manager.SecondsUntilNextHeart;
                int minutes = (int)(secondsRemaining / 60);
                int seconds = (int)(secondsRemaining % 60);
                targetText = $"{minutes:00}:{seconds:00}";
            }

            if (_lastStatusText != targetText)
            {
                _lastStatusText = targetText;
                statusText.text = targetText;
            }
        }

        private void UpdateButtonState(HeartManager manager)
        {
            if (addButton == null || manager == null) return;

            bool canAdd = !manager.IsInfiniteHeartsActive && manager.Hearts < 5;
            if (_lastButtonInteractableState == null || _lastButtonInteractableState.Value != canAdd)
            {
                _lastButtonInteractableState = canAdd;
                addButton.interactable = canAdd;
            }
        }

        #endregion

        #region Animation Control

        private void PlayAmountChanged(int previousAmount, int currentAmount)
        {
            currentAmount = Mathf.Max(0, currentAmount);
            if (previousAmount == currentAmount)
            {
                SetAmount(currentAmount, animate: false);
                return;
            }

            KillAllTweens();
            CacheBaseState(force: false);
            RestoreBaseState();

            _displayedAmount = Mathf.Max(0, previousAmount);
            _targetAmount = currentAmount;
            RefreshText();
            _isAnimating = true;

            int direction = currentAmount > previousAmount ? 1 : -1;
            RectTransform root = AmountRoot;

            _animationSequence = DOTween.Sequence()
                .SetUpdate(useUnscaledTime)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);

            if (root != null)
            {
                Vector2 outPosition = _amountBaseAnchoredPosition + new Vector2(0f, slideOffsetY * direction);
                Vector2 inStartPosition = _amountBaseAnchoredPosition - new Vector2(0f, slideOffsetY * direction);
                Vector3 feedbackScale = _amountBaseScale * (direction > 0 ? increaseScale : decreaseScale);

                _animationSequence.Append(root.DOAnchorPos(outPosition, slideOutDuration).SetEase(slideOutEase));
                _animationSequence.Join(root.DOScale(feedbackScale, slideOutDuration).SetEase(slideOutEase));
                
                _animationSequence.AppendCallback(() =>
                {
                    root.anchoredPosition = inStartPosition;
                    _displayedAmount = _targetAmount;
                    RefreshText();
                });
                
                _animationSequence.Append(root.DOAnchorPos(_amountBaseAnchoredPosition, slideInDuration).SetEase(slideInEase));
                _animationSequence.Join(root.DOScale(_amountBaseScale, slideInDuration).SetEase(slideInEase));
            }
            else
            {
                _animationSequence.AppendInterval(slideOutDuration);
                _animationSequence.AppendCallback(() =>
                {
                    _displayedAmount = _targetAmount;
                    RefreshText();
                });
                _animationSequence.AppendInterval(slideInDuration);
            }

            Image activeIcon = ActiveHeartIcon;
            if (activeIcon != null)
            {
                IconState baseState = GetIconBaseState(activeIcon);
                activeIcon.transform.localScale = baseState.Scale;
                _animationSequence.Join(activeIcon.transform
                    .DOPunchScale(baseState.Scale * iconPunchScale, iconPunchDuration, 8, 0.75f)
                    .SetUpdate(useUnscaledTime));
            }

            _animationSequence.OnComplete(() =>
            {
                _displayedAmount = _targetAmount;
                RefreshText();
                RestoreBaseState();
                _isAnimating = false;
                StartIdleAnimation();
            });
        }

        private void PlayTransitionAnimation(bool isInfinite)
        {
            KillAllTweens();

            Image oldIcon = isInfinite ? heartIcon : infiniteHeartIcon;
            Image newIcon = isInfinite ? infiniteHeartIcon : heartIcon;
            IconState newBaseState = GetIconBaseState(newIcon);

            _transitionSequence = DOTween.Sequence()
                .SetUpdate(useUnscaledTime)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);

            if (newIcon != null)
            {
                newIcon.gameObject.SetActive(true);
                newIcon.transform.localScale = Vector3.zero;
                newIcon.transform.localRotation = newBaseState.Rotation;
            }

            float duration = 0.25f;

            if (oldIcon != null)
            {
                _transitionSequence.Join(oldIcon.transform.DOScale(Vector3.zero, duration).SetEase(Ease.InQuad));
            }
            if (newIcon != null)
            {
                _transitionSequence.Join(newIcon.transform.DOScale(newBaseState.Scale, duration).SetEase(Ease.OutBack));
            }

            RectTransform textRoot = AmountRoot;
            if (textRoot != null)
            {
                _transitionSequence.Join(textRoot.DOPunchScale(textRoot.localScale * 0.12f, duration, 5, 0.5f));
            }
            if (statusText != null)
            {
                _transitionSequence.Join(statusText.transform.DOPunchScale(statusText.transform.localScale * 0.12f, duration, 5, 0.5f));
            }

            _transitionSequence.OnComplete(() =>
            {
                if (oldIcon != null) oldIcon.gameObject.SetActive(false);
                RestoreBaseState();
                StartIdleAnimation();
            });
        }

        private void StartIdleAnimation()
        {
            Image activeIcon = ActiveHeartIcon;
            if (activeIcon == null || iconIdleAnimType == IconIdleAnimType.None) return;

            _idleTween?.Kill();
            IconState baseState = GetIconBaseState(activeIcon);

            if (iconIdleAnimType == IconIdleAnimType.Floating)
            {
                float startY = baseState.Position.y;
                float targetY = startY + idleFloatAmount;
                activeIcon.rectTransform.anchoredPosition = new Vector2(baseState.Position.x, startY);

                _idleTween = activeIcon.rectTransform.DOAnchorPosY(targetY, idleDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(useUnscaledTime)
                    .SetLink(activeIcon.gameObject);
            }
            else if (iconIdleAnimType == IconIdleAnimType.Pulsing)
            {
                activeIcon.transform.localScale = baseState.Scale;
                Vector3 targetScale = baseState.Scale * idlePulseScale;

                _idleTween = activeIcon.transform.DOScale(targetScale, idleDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(useUnscaledTime)
                    .SetLink(activeIcon.gameObject);
            }
        }

        private void PlayIconShake()
        {
            Image activeIcon = ActiveHeartIcon;
            if (activeIcon == null) return;

            KillAllTweens();

            IconState baseState = GetIconBaseState(activeIcon);
            baseState.Restore(activeIcon);

            _shakeSequence = DOTween.Sequence()
                .SetUpdate(useUnscaledTime)
                .SetLink(activeIcon.gameObject);

            _shakeSequence.Append(activeIcon.transform.DOPunchRotation(new Vector3(0f, 0f, shakeRotationStrength), shakeDuration, 10, 1f));
            _shakeSequence.Join(activeIcon.transform.DOPunchScale(baseState.Scale * shakeScaleStrength, shakeDuration, 10, 1f));

            _shakeSequence.OnComplete(() =>
            {
                _shakeSequence = null;
                StartIdleAnimation();
            });
        }

        #endregion

        #region Base State Cache/Restore

        private void CacheBaseState(bool force)
        {
            if (_hasCachedBaseState && !force) return;

            RectTransform root = AmountRoot;
            if (root != null)
            {
                _amountBaseAnchoredPosition = root.anchoredPosition;
                _amountBaseScale = root.localScale;
            }

            _normalIconState.Cache(heartIcon);
            _infiniteIconState.Cache(infiniteHeartIcon);
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

            _normalIconState.Restore(heartIcon);
            _infiniteIconState.Restore(infiniteHeartIcon);
        }

        private void KillAllTweens()
        {
            _animationSequence?.Kill();
            _animationSequence = null;
            _shakeSequence?.Kill();
            _shakeSequence = null;
            _transitionSequence?.Kill();
            _transitionSequence = null;
            _idleTween?.Kill();
            _idleTween = null;
        }

        private void SetupIconInteraction()
        {
            RegisterShakeHandler(heartIcon);
            RegisterShakeHandler(infiniteHeartIcon);

            if (addButton != null)
            {
                addButton.onClick.RemoveListener(HandleAddHeartsClicked);
                addButton.onClick.AddListener(HandleAddHeartsClicked);
            }
        }

        private void HandleAddHeartsClicked()
        {
            HeartManager manager = GetHeartManager();
            if (manager == null) return;

            if (!manager.IsInfiniteHeartsActive && manager.Hearts < 5)
            {
                UIManager.Instance?.ShowPopup<RefillHeartPopup>();
            }
        }

        private void RegisterShakeHandler(Image icon)
        {
            if (icon == null) return;
            Button button = icon.GetComponent<Button>();
            if (button == null)
            {
                button = icon.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
            }
            button.onClick.RemoveListener(PlayIconShake);
            button.onClick.AddListener(PlayIconShake);
        }

        #endregion
    }
}
