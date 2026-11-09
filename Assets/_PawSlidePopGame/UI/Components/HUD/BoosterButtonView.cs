using System;
using _PawSlidePopGame._Scripts.Feature.Match3.Boosters;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace _PawSlidePopGame._Scripts.UI.Components.HUD
{
    [DisallowMultipleComponent]
    public sealed class BoosterButtonView : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private Button button;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private GameObject lockRoot;
        [SerializeField] private GameObject lockBubbleRoot;
        [SerializeField] private GameObject countNotifyRoot;
        [SerializeField] private GameObject priceRoot;
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private GameObject adRoot;
        [SerializeField] private GameObject checkboxRoot;
        [SerializeField] private Toggle checkboxToggle;

        [Header("Visuals")]
        [SerializeField] private Sprite unlockedBackgroundSprite;
        [SerializeField] private Sprite lockedBackgroundSprite;
        [SerializeField] private string countFormat = "x{0}";
        [SerializeField] private string unlimitedCountText = "∞";
        [SerializeField] private float selectedIconScale = 1.12f;
        [SerializeField] private float selectedIconOffsetY = 10f;

        private BoosterDefinitionSO _definition;
        private Action<BoosterDefinitionSO> _onClicked;
        private RectTransform _iconRectTransform;
        private Vector2 _iconBaseAnchoredPosition;
        private Vector3 _iconBaseScale = Vector3.one;
        private bool _hasIconBaseState;

        private Vector3 _checkboxBaseScale = Vector3.one;
        private bool _hasCheckboxBaseState;
        private Tween _checkboxTween;
        private Tween _lockTween;

        public BoosterDefinitionSO Definition => _definition;

        private void Awake()
        {
            CacheIconBaseState();
            CacheCheckboxBaseState();
        }

        private void OnEnable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClicked);
                button.onClick.AddListener(HandleClicked);
            }
        }

        private void OnDisable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClicked);
            }

            if (_checkboxTween != null)
            {
                _checkboxTween.Kill();
                _checkboxTween = null;
            }

            if (_lockTween != null)
            {
                _lockTween.Kill();
                _lockTween = null;
            }
        }

        public void Bind(BoosterDefinitionSO definition, int count, bool isSelected, Action<BoosterDefinitionSO> onClicked)
        {
            _definition = definition;
            _onClicked = onClicked;
            CacheIconBaseState();
            CacheCheckboxBaseState();

            if (button != null)
            {
                button.interactable = definition != null;
            }

            if (iconImage != null)
            {
                iconImage.sprite = definition != null ? definition.Icon : null;
                iconImage.enabled = definition != null && definition.Icon != null;
            }

            SetCheckboxVisible(false, animate: false);

            SetState(count, isSelected);
        }

        public void SetState(int count, bool isSelected)
        {
            int currentLevelNumber = 1;
            if (_PawSlidePopGame._Scripts.Feature.Match3.Flow.Match3LevelManager.Instance != null && 
                _PawSlidePopGame._Scripts.Feature.Match3.Flow.Match3LevelManager.Instance.CurrentLevelData != null)
            {
                currentLevelNumber = _PawSlidePopGame._Scripts.Feature.Match3.Flow.Match3LevelManager.Instance.CurrentLevelData.DisplayLevelNumber;
            }
            else if (_PawSlidePopGame._Scripts.Gameplay.Meta.MapManager.LevelProgressRepository.Instance != null)
            {
                currentLevelNumber = _PawSlidePopGame._Scripts.Gameplay.Meta.MapManager.LevelProgressRepository.Instance.GetHighestUnlockedLevelNumber();
            }

            bool isUnlocked = _definition != null && _definition.IsUnlockedAtLevel(currentLevelNumber);

            if (backgroundImage != null)
            {
                Sprite targetSprite = isUnlocked ? unlockedBackgroundSprite : lockedBackgroundSprite;
                if (targetSprite != null)
                {
                    backgroundImage.sprite = targetSprite;
                }

                backgroundImage.color = Color.white;
            }

            if (lockRoot != null)
            {
                lockRoot.SetActive(!isUnlocked);
                if (!isUnlocked)
                {
                    if (_lockTween == null && Application.isPlaying)
                    {
                        lockRoot.transform.localRotation = Quaternion.Euler(0f, 0f, -8f);
                        _lockTween = lockRoot.transform.DOLocalRotate(new Vector3(0f, 0f, 8f), 0.6f)
                            .SetEase(Ease.InOutSine)
                            .SetLoops(-1, LoopType.Yoyo)
                            .SetUpdate(true);
                    }
                }
                else
                {
                    if (_lockTween != null)
                    {
                        _lockTween.Kill();
                        _lockTween = null;
                    }
                    lockRoot.transform.localRotation = Quaternion.identity;
                }
            }

            if (lockBubbleRoot != null)
            {
                lockBubbleRoot.SetActive(!isUnlocked);
            }

            ApplySelectedIconState(isSelected);

            bool showCount = isUnlocked && (count > 0 || (_definition != null && _definition.IsUnlimitedForDev));
            bool showPrice = isUnlocked && !showCount;
            bool showAd = false;

            bool isPreLevel = _definition != null && _definition.UsagePhase == BoosterUsagePhase.PreLevel;
            if (adRoot != null && showPrice && isPreLevel)
            {
                showPrice = false;
                showAd = true;
            }

            if (countNotifyRoot != null)
            {
                countNotifyRoot.SetActive(showCount);
            }

            if (countText != null)
            {
                countText.SetText(ResolveCountText(count));
            }

            if (priceRoot != null)
            {
                priceRoot.SetActive(showPrice);
            }

            if (priceText != null && _definition != null)
            {
                priceText.SetText(_definition.CoinPrice.ToString());
            }

            if (adRoot != null)
            {
                adRoot.SetActive(showAd);
            }
        }

        public void SetCheckboxVisible(bool visible, bool animate = true)
        {
            CacheCheckboxBaseState();

            if (checkboxRoot == null)
            {
                return;
            }

            if (_checkboxTween != null)
            {
                _checkboxTween.Kill();
                _checkboxTween = null;
            }

            if (visible)
            {
                checkboxRoot.SetActive(true);
                if (animate && Application.isPlaying)
                {
                    checkboxRoot.transform.localScale = Vector3.zero;
                    _checkboxTween = checkboxRoot.transform
                        .DOScale(_checkboxBaseScale, 0.3f)
                        .SetEase(Ease.OutBack)
                        .SetUpdate(true);
                }
                else
                {
                    checkboxRoot.transform.localScale = _checkboxBaseScale;
                }
            }
            else
            {
                if (animate && Application.isPlaying && checkboxRoot.activeSelf)
                {
                    _checkboxTween = checkboxRoot.transform
                        .DOScale(Vector3.zero, 0.2f)
                        .SetEase(Ease.InQuad)
                        .SetUpdate(true)
                        .OnComplete(() => checkboxRoot.SetActive(false));
                }
                else
                {
                    checkboxRoot.transform.localScale = Vector3.zero;
                    checkboxRoot.SetActive(false);
                }
            }
        }

        public void SetCheckboxState(bool visible, bool isChecked = false, bool animate = true)
        {
            SetCheckboxVisible(visible, animate);
            if (checkboxToggle != null)
            {
                checkboxToggle.isOn = isChecked;
            }
        }

        private void CacheIconBaseState()
        {
            if (_hasIconBaseState || iconImage == null)
            {
                return;
            }

            _iconRectTransform = iconImage.rectTransform;
            _iconBaseAnchoredPosition = _iconRectTransform.anchoredPosition;
            _iconBaseScale = _iconRectTransform.localScale;
            _hasIconBaseState = true;
        }

        private void CacheCheckboxBaseState()
        {
            if (_hasCheckboxBaseState || checkboxRoot == null)
            {
                return;
            }

            _checkboxBaseScale = checkboxRoot.transform.localScale;
            _hasCheckboxBaseState = true;
        }

        private void ApplySelectedIconState(bool isSelected)
        {
            if (!_hasIconBaseState || _iconRectTransform == null)
            {
                return;
            }

            _iconRectTransform.anchoredPosition = isSelected
                ? _iconBaseAnchoredPosition + new Vector2(0f, selectedIconOffsetY)
                : _iconBaseAnchoredPosition;

            _iconRectTransform.localScale = isSelected
                ? _iconBaseScale * selectedIconScale
                : _iconBaseScale;
        }

        private string ResolveCountText(int count)
        {
            if (_definition != null && _definition.IsUnlimitedForDev)
            {
                return unlimitedCountText;
            }

            return string.Format(countFormat, Mathf.Max(0, count));
        }

        private void HandleClicked()
        {
            if (_definition != null)
            {
                _onClicked?.Invoke(_definition);
            }
        }
    }
}
