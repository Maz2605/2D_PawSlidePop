using System;
using _PawSlidePopGame._Scripts.UI.Base;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.UI.Popups
{
    public sealed class LevelLockedPopup : BasePopup
    {
        [Header("--- UI Elements ---")]
        [SerializeField] private RectTransform animatedContent;
        [SerializeField] private RectTransform lockTransform;
        [SerializeField] private RectTransform okButtonTransform;
        [SerializeField] private TMP_Text levelText;

        [Header("--- Buttons ---")]
        [SerializeField] private Button okButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button backgroundButton;

        [Header("--- Animation Settings ---")]
        [SerializeField] private float showScaleDuration = 0.25f;
        [SerializeField] private float hideScaleDuration = 0.2f;
        [SerializeField] private float startScale = 0.8f;
        [SerializeField] private Ease showEase = Ease.OutBack;
        [SerializeField] private Ease hideEase = Ease.InQuad;

        [Header("--- Idle Lock Rotation Settings ---")]
        [SerializeField] private float lockRotationAngle = 35f;
        [SerializeField] private float lockRotationDuration = 1.2f;

        [Header("--- Idle Button Float Settings ---")]
        [SerializeField] private float buttonFloatDistance = 15f;
        [SerializeField] private float buttonFloatDuration = 1.5f;

        private Sequence _showSequence;
        private Tween _lockTween;
        private Tween _buttonFloatTween;

        private Vector3 _lockBaseEulerAngles;
        private Vector2 _buttonBaseAnchoredPosition;

        private string _levelId;
        private Match3LevelData _levelData;

        public string LevelId => _levelId;
        public Match3LevelData LevelData => _levelData;

        protected override void Awake()
        {
            base.Awake();

            if (lockTransform != null)
            {
                _lockBaseEulerAngles = lockTransform.localEulerAngles;
            }

            if (okButtonTransform != null)
            {
                _buttonBaseAnchoredPosition = okButtonTransform.anchoredPosition;
            }
        }

        public void Setup(string levelId, Match3LevelData levelData = null)
        {
            _levelId = levelId;
            _levelData = levelData;

            if (levelText != null)
            {
                if (levelData != null)
                {
                    levelText.text = $"Level {levelData.DisplayLevelNumber}";
                }
                else
                {
                    levelText.text = $"Level {ExtractLevelNumber(levelId)}";
                }
            }
        }

        private int ExtractLevelNumber(string levelId)
        {
            if (string.IsNullOrEmpty(levelId)) return 1;

            int idx = levelId.LastIndexOf('_');
            if (idx >= 0 && idx < levelId.Length - 1)
            {
                string numStr = levelId.Substring(idx + 1);
                if (int.TryParse(numStr, out int num))
                {
                    return num;
                }
            }
            return 1;
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
            // Lock harmonic swing (rotate left and right)
            if (lockTransform != null)
            {
                Vector3 targetEulerAngles = _lockBaseEulerAngles + new Vector3(0f, 0f, lockRotationAngle);
                lockTransform.localEulerAngles = _lockBaseEulerAngles + new Vector3(0f, 0f, -lockRotationAngle);

                _lockTween = lockTransform.DOLocalRotate(targetEulerAngles, lockRotationDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true)
                    .SetLink(lockTransform.gameObject, LinkBehaviour.KillOnDisable);
            }

            // Button float/drift up and down
            if (okButtonTransform != null)
            {
                okButtonTransform.anchoredPosition = _buttonBaseAnchoredPosition;

                _buttonFloatTween = okButtonTransform.DOAnchorPosY(_buttonBaseAnchoredPosition.y + buttonFloatDistance, buttonFloatDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true)
                    .SetLink(okButtonTransform.gameObject, LinkBehaviour.KillOnDisable);
            }
        }

        private void KillActiveTweens()
        {
            _showSequence?.Kill();
            _showSequence = null;

            _lockTween?.Kill();
            _lockTween = null;

            _buttonFloatTween?.Kill();
            _buttonFloatTween = null;

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

            if (lockTransform != null)
            {
                lockTransform.localEulerAngles = _lockBaseEulerAngles;
            }

            if (okButtonTransform != null)
            {
                okButtonTransform.anchoredPosition = _buttonBaseAnchoredPosition;
            }
        }

        private void BindButtons()
        {
            SetButtonsInteractable(false);

            BindButton(okButton, HandleClosePressed);
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

        private void SetButtonsInteractable(bool interactable)
        {
            if (okButton != null) okButton.interactable = interactable;
            if (closeButton != null) closeButton.interactable = interactable;
            if (backgroundButton != null) backgroundButton.interactable = interactable;
        }

        private void OnDestroy()
        {
            KillActiveTweens();
        }
    }
}
