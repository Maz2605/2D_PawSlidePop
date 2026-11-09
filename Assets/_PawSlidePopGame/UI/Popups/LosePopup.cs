using MazFramework.Audio;
using MazFramework.Haptics;
using PawAudioConfig = _PawSlidePopGame._Scripts.Data.Config.PawSlidePopAudioConfig;
using System;
using _PawSlidePopGame._Scripts.Core.System.GameFlow;
using _PawSlidePopGame._Scripts.Data.Events.Payloads;
using MazFramework.UI;
using _PawSlidePopGame._Scripts.UI.Components.Popup;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.UI.Popups
{
    public sealed class LosePopup : BasePopup
    {
        [Header("--- Section Managers ---")]
        [SerializeField] private RectTransform animatedContent;
        [SerializeField] private Transform topRoot;
        [SerializeField] private Transform centerRoot;
        [SerializeField] private Transform bottomRoot;
        [SerializeField] private PopupElement topElement;
        [SerializeField] private PopupElement centerElement;
        [SerializeField] private LosePopupProgressSection progressSection;
        [SerializeField] private PopupElement buttonsElement;

        [Header("--- Buttons ---")]
        [SerializeField] private Button homeButton;
        [SerializeField] private Button repeatButton;

        [Header("--- Sequence Settings ---")]
        [SerializeField] private float sectionOverlap = 0.14f;
        [SerializeField] private float phaseOverlap = -0.08f;
        [SerializeField] private float contentFrameShowDuration = 0.18f;
        [SerializeField] private float contentFrameStartScale = 0.94f;

        private GameplayHudSnapshot _snapshot;
        private Sequence _showSequence;

        public void SetSnapshot(GameplayHudSnapshot snapshot)
        {
            _snapshot = snapshot?.Clone();
        }


        public override bool TryHandleBack()
        {
            return true;
        }

        protected override void OnBeforeShow()
        {
            base.OnBeforeShow();

            var audio = AudioManager.Instance;
            if (audio?.Config is PawAudioConfig config)
            {
                var resultMusic = config.Music.resultLoseBackground;
                if (resultMusic != null) audio.PlayMusic(resultMusic);
                else audio.StopMusic(.25f);
                audio.PlaySfx(config.Match3Sfx.levelLose);
            }
            if (MazFramework.Haptics.HapticManager.Instance != null)
            {
                MazFramework.Haptics.HapticManager.Instance.PlayFailure();
            }

            KillActiveTweens();
            ResetLayoutState();

            topElement?.SetIdleEnabled(false);
            centerElement?.SetIdleEnabled(false);
            topElement?.PrepareForShow();
            centerElement?.PrepareForShow();
            buttonsElement?.PrepareForShow();
            progressSection?.Prepare();

            BindButtons();
        }

        protected override Tween CreateShowTween(CanvasGroup group, float duration)
        {
            _showSequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);

            if (animatedContent != null)
            {
                animatedContent.localScale = Vector3.one * contentFrameStartScale;
                _showSequence.Append(animatedContent.DOScale(Vector3.one, contentFrameShowDuration).SetEase(Ease.OutBack));
            }

            if (topElement != null)
            {
                _showSequence.Append(topElement.DoPopUp());
            }

            Sequence centerSequence = BuildCenterSequence();
            if (centerSequence != null)
            {
                float insertTime = Mathf.Max(0f, _showSequence.Duration(false) - sectionOverlap);
                _showSequence.Insert(insertTime, centerSequence);
            }

            if (buttonsElement != null)
            {
                _showSequence.AppendInterval(phaseOverlap);
                _showSequence.Append(buttonsElement.DoPopUp());
            }

            _showSequence.AppendCallback(() => SetButtonsInteractable(true));
            _showSequence.Insert(0f, group.DOFade(1f, ShowDuration));
            return _showSequence;
        }

        protected override Tween CreateHideTween(CanvasGroup group, float duration)
        {
            KillActiveTweens();

            Transform animationTarget = animatedContent != null ? animatedContent : transform;

            Sequence hideSequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);

            hideSequence.Join(CanvasGroup.DOFade(0f, 0.2f));
            hideSequence.Join(animationTarget.DOScale(0.95f, 0.2f).SetEase(Ease.InQuad));
            
            return hideSequence;
        }

        #region Helpers

        private void BindButtons()
        {
            SetButtonsInteractable(false);
            if (homeButton != null) { homeButton.onClick.RemoveListener(HandleHomePressed); homeButton.onClick.AddListener(HandleHomePressed); }
            if (repeatButton != null) { repeatButton.onClick.RemoveListener(HandleRepeatPressed); repeatButton.onClick.AddListener(HandleRepeatPressed); }
        }

        private void SetButtonsInteractable(bool enabled)
        {
            if (homeButton) homeButton.interactable = enabled;
            if (repeatButton) repeatButton.interactable = enabled;
        }

        private void HandleRepeatPressed()
        {
            GameAppFlowManager.Instance?.RestartGameplay();
        }

        private void HandleHomePressed()
        {
            GameAppFlowManager.Instance?.EnterMainMenu();
        }

        private Sequence BuildCenterSequence()
        {
            if (centerElement == null && progressSection == null)
            {
                return null;
            }

            Sequence centerSequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);

            if (centerElement != null)
            {
                centerSequence.Join(centerElement.DoPopUp());
            }

            if (progressSection != null)
            {
                centerSequence.Join(progressSection.GetShowSequence(_snapshot));
            }

            return centerSequence;
        }

        private void ResetLayoutState()
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
            topRoot?.DOKill();
            centerRoot?.DOKill();
            bottomRoot?.DOKill();
            progressSection?.DOKill();
            transform.DOKill();
        }

        #endregion
        protected override void OnDisable()
        {
            if (homeButton != null) homeButton.onClick.RemoveListener(HandleHomePressed);
            if (repeatButton != null) repeatButton.onClick.RemoveListener(HandleRepeatPressed);
            base.OnDisable();
        }
    }
}
