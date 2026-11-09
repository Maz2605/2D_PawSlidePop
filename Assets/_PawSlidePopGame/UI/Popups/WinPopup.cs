using _PawSlidePopGame._Scripts.Core.Audio;
using _PawSlidePopGame._Scripts.Core.Vibration;
using _PawSlidePopGame._Scripts.Core.System.GameFlow;
using _PawSlidePopGame._Scripts.Data.Events.Payloads;
using _PawSlidePopGame._Scripts.UI.Base;
using _PawSlidePopGame._Scripts.UI.Components.Popup;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.UI.Popups
{
    public sealed class WinPopup : BasePopup
    {
        [Header("--- Section Managers ---")]
        [SerializeField] private WinPopupStarSection starSection;
        [SerializeField] private WinPopupCompleteSection completeSection;
        [SerializeField] private WinPopupDataSection dataSection;
        [SerializeField] private PopupElement buttonsElement;
        [SerializeField] private RectTransform animatedContent;

        [Header("--- Buttons ---")]
        [SerializeField] private Button levelsButton;
        [SerializeField] private Button repeatButton;
        [SerializeField] private Button nextLevelButton;

        [Header("--- Sequence Settings ---")]
        [SerializeField] private float phaseOverlap = -0.1f; // Độ gối đầu giữa các phase để tạo cảm giác mượt
        [SerializeField] private float starSectionOverlapStart = 0.2f; // Bắt đầu các section còn lại trong khi stars vẫn animate
        [SerializeField] private float contentFrameShowDuration = 0.18f;
        [SerializeField] private float contentFrameStartScale = 0.94f;

        private GameplayHudSnapshot _snapshot;
        private Sequence _showSequence;

        public void SetSnapshot(GameplayHudSnapshot snapshot)
        {
            _snapshot = snapshot?.Clone();
        }

        protected override UISoundType OpenSound => UISoundType.None;
        protected override UISoundType CloseSound => UISoundType.None;

        protected override void OnBeforeShow()
        {
            if (AudioController.Instance != null)
            {
                AudioController.Instance.PlayWinMusic();
                AudioController.Instance.PlayLevelWin();
            }
            if (VibrationManager.Instance != null)
            {
                VibrationManager.Instance.PlayWin();
            }

            KillActiveTweens();
            ResetLayoutState();

            starSection.Prepare();
            completeSection.Prepare();
            dataSection.Prepare();
            buttonsElement?.PrepareForShow();

            starSection.Bind(_snapshot != null ? _snapshot.reachedStars : 0);
            BindButtons();
        }

        protected override void PlayShowAnimation()
        {
            // Tạo Sequence tổng, gán SetLink để tự động Kill khi Popup bị đóng/hủy
            _showSequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);

            if (animatedContent != null)
            {
                animatedContent.localScale = Vector3.one * contentFrameStartScale;
                _showSequence.Append(animatedContent.DOScale(Vector3.one, contentFrameShowDuration).SetEase(Ease.OutBack));
            }

            // Bước 1: Chạy cụm Stars
            _showSequence.Append(starSection.GetShowSequence(_snapshot?.reachedStars ?? 0));

            // Bước 2: Chạy các section khác trong khi star animation vẫn đang chạy
            float parallelStartTime = Mathf.Max(0f, starSectionOverlapStart);
            _showSequence.Insert(parallelStartTime, completeSection.GetShowSequence());
            _showSequence.Insert(parallelStartTime, dataSection.GetShowSequence(
                _snapshot?.levelNumber ?? 1, 
                _snapshot?.currentScore ?? 0));

            // Bước 3: Hiện các nút điều hướng
            if (buttonsElement != null)
            {
                _showSequence.AppendInterval(phaseOverlap);
                _showSequence.Append(buttonsElement.DoPopUp());
            }

            // Kết thúc: Cho phép tương tác nút
            _showSequence.OnComplete(() => SetButtonsInteractable(true));
        }

        protected override void PlayHideAnimation(System.Action onComplete)
        {
            KillActiveTweens();

            Transform animationTarget = animatedContent != null ? animatedContent : transform;
            
            Sequence hideSequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);

            hideSequence.Join(canvasGroup.DOFade(0f, 0.2f));
            hideSequence.Join(animationTarget.DOScale(0.95f, 0.2f).SetEase(Ease.InQuad));
            hideSequence.OnComplete(() => onComplete?.Invoke());
        }

        #region --- Logic Hỗ Trợ ---

        private void BindButtons()
        {
            SetButtonsInteractable(false);
            BindButton(levelsButton, HandleLevelsPressed);
            BindButton(repeatButton, HandleRepeatPressed);
            BindButton(nextLevelButton, HandleNextLevelPressed);
        }

        private void SetButtonsInteractable(bool enabled)
        {
            if (levelsButton) levelsButton.interactable = enabled;
            if (repeatButton) repeatButton.interactable = enabled;
            if (nextLevelButton) nextLevelButton.interactable = enabled;
        }

        private void HandleLevelsPressed() => GameAppFlowManager.Instance?.EnterMainMenu();
        private void HandleRepeatPressed() => GameAppFlowManager.Instance?.RestartGameplay();
        private void HandleNextLevelPressed() => GameAppFlowManager.Instance?.StartNextLevel();

        private void ResetLayoutState()
        {
            ResetAnimatedContentRect();
        }

        private void ResetAnimatedContentRect()
        {
            if (animatedContent == null)
            {
                return;
            }

            animatedContent.anchoredPosition = Vector2.zero;
            animatedContent.localScale = Vector3.one;
            animatedContent.localRotation = Quaternion.identity;
        }

        private void KillActiveTweens()
        {
            _showSequence?.Kill();
            animatedContent?.DOKill();
            transform.DOKill();
        }

        #endregion
    }
}
