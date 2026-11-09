using System;
using _PawSlidePopGame._Scripts.Core.System.GameFlow;
using _PawSlidePopGame._Scripts.Feature.Match3.Flow;
using MazFramework.UI;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.UI.Popups
{
    public sealed class PausePopup : BasePopup
    {
        [Header("--- Elements ---")]
        [SerializeField] private RectTransform animatedContent;
        [SerializeField] private RectTransform animalRoot;
        [SerializeField] private RectTransform buttonGroupRoot;

        [Header("--- Buttons ---")]
        [SerializeField] private Button btnRestart;
        [SerializeField] private Button btnResume;
        [SerializeField] private Button btnQuit;
        [SerializeField] private Button btnBackground;

        [Header("--- Sequence Settings ---")]
        [SerializeField] private float moveOffsetY = 220f;
        [SerializeField] private float moveDuration = 0.3f;
        [SerializeField] private Ease showEase = Ease.OutCubic;
        [SerializeField] private Ease hideEase = Ease.InCubic;

        private Sequence _showSequence;
        private Vector2 _animalOriginalAnchoredPosition;
        private Vector2 _buttonGroupOriginalAnchoredPosition;

        public override bool TryHandleBack()
        {
            GameFlowManager.Instance?.ResumeGameplay();
            return true;
        }

        protected override void Awake()
        {
            base.Awake();

            if (animalRoot != null)
            {
                _animalOriginalAnchoredPosition = animalRoot.anchoredPosition;
            }

            if (buttonGroupRoot != null)
            {
                _buttonGroupOriginalAnchoredPosition = buttonGroupRoot.anchoredPosition;
            }
        }

        protected override void OnBeforeShow()
        {
            base.OnBeforeShow();

            KillActiveTweens();
            ResetLayoutState();
            PrepareElementsForShow();

            BindButtons();
        }

        protected override Tween CreateShowTween(CanvasGroup group, float duration)
        {
            _showSequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);

            if (animalRoot != null)
            {
                _showSequence.Join(
                    animalRoot.DOAnchorPos(_animalOriginalAnchoredPosition, moveDuration)
                        .SetEase(showEase));
            }

            if (buttonGroupRoot != null)
            {
                _showSequence.Join(
                    buttonGroupRoot.DOAnchorPos(_buttonGroupOriginalAnchoredPosition, moveDuration)
                        .SetEase(showEase));
            }

            _showSequence.AppendCallback(() => SetupButtonsInteractable(true));
            _showSequence.Insert(0f, group.DOFade(1f, ShowDuration));
            return _showSequence;
        }

        protected override Tween CreateHideTween(CanvasGroup group, float duration)
        {
            KillActiveTweens();

            Sequence hideSequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);

            if (buttonGroupRoot != null)
            {
                hideSequence.Join(
                    buttonGroupRoot.DOAnchorPos(
                            _buttonGroupOriginalAnchoredPosition + new Vector2(0f, -moveOffsetY),
                            moveDuration)
                        .SetEase(hideEase));
            }

            if (animalRoot != null)
            {
                hideSequence.Join(
                    animalRoot.DOAnchorPos(
                            _animalOriginalAnchoredPosition + new Vector2(0f, moveOffsetY),
                            moveDuration)
                        .SetEase(hideEase));
            }

            
            return hideSequence;
        }

        private void BindButtons()
        {
            SetupButtonsInteractable(false);
            if (btnRestart != null) { btnRestart.onClick.RemoveListener(HandleRestartPressed); btnRestart.onClick.AddListener(HandleRestartPressed); }
            if (btnQuit != null) { btnQuit.onClick.RemoveListener(HandleQuitPressed); btnQuit.onClick.AddListener(HandleQuitPressed); }
            if (btnResume != null) { btnResume.onClick.RemoveListener(HandleResumePressed); btnResume.onClick.AddListener(HandleResumePressed); }
            if (btnBackground != null) { btnBackground.onClick.RemoveListener(HandleResumePressed); btnBackground.onClick.AddListener(HandleResumePressed); }
        }

        private void HandleResumePressed()
        {
            GameFlowManager.Instance?.ResumeGameplay();
        }

        private void HandleQuitPressed()
        {
            if (GameAppFlowManager.Instance != null)
            {
                GameAppFlowManager.Instance.EnterMainMenu();
                return;
            }

            Debug.LogWarning("[PausePopup] Missing GameAppFlowManager. Quit request ignored.", this);
        }

        private void HandleRestartPressed()
        {
            if (GameAppFlowManager.Instance != null)
            {
                GameAppFlowManager.Instance.RestartGameplay();
                return;
            }

            Debug.LogWarning("[PausePopup] Missing GameAppFlowManager. Restart request ignored.", this);
        }

        private void SetupButtonsInteractable(bool enable)
        {
            if (btnRestart) btnRestart.interactable = enable;
            if (btnResume) btnResume.interactable = enable;
            if (btnQuit) btnQuit.interactable = enable;
            if (btnBackground) btnBackground.interactable = enable;
        }

        private void PrepareElementsForShow()
        {
            if (animalRoot != null)
            {
                animalRoot.anchoredPosition = _animalOriginalAnchoredPosition + new Vector2(0f, moveOffsetY);
            }

            if (buttonGroupRoot != null)
            {
                buttonGroupRoot.anchoredPosition = _buttonGroupOriginalAnchoredPosition + new Vector2(0f, -moveOffsetY);
            }
        }

        private void ResetLayoutState()
        {
            if (animatedContent != null)
            {
                animatedContent.anchoredPosition = Vector2.zero;
                animatedContent.localScale = Vector3.one;
                animatedContent.localRotation = Quaternion.identity;
            }

            if (animalRoot != null)
            {
                animalRoot.anchoredPosition = _animalOriginalAnchoredPosition;
                animalRoot.localScale = Vector3.one;
                animalRoot.localRotation = Quaternion.identity;
            }

            if (buttonGroupRoot != null)
            {
                buttonGroupRoot.anchoredPosition = _buttonGroupOriginalAnchoredPosition;
                buttonGroupRoot.localScale = Vector3.one;
                buttonGroupRoot.localRotation = Quaternion.identity;
            }
        }

        private void KillActiveTweens()
        {
            _showSequence?.Kill();
            animatedContent?.DOKill();
            animalRoot?.DOKill();
            buttonGroupRoot?.DOKill();
            transform.DOKill();
        }


        protected override void OnDisable()
        {
            if (btnRestart != null) btnRestart.onClick.RemoveListener(HandleRestartPressed);
            if (btnQuit != null) btnQuit.onClick.RemoveListener(HandleQuitPressed);
            if (btnResume != null) btnResume.onClick.RemoveListener(HandleResumePressed);
            if (btnBackground != null) btnBackground.onClick.RemoveListener(HandleResumePressed);
            base.OnDisable();
        }
    }
}
