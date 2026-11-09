using System;
using _PawSlidePopGame._Scripts.Core.System.GameFlow;
using MazFramework.UI;
using _PawSlidePopGame._Scripts.UI.Popups;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.UI.Screens
{
    public class StartScreen : BaseScreen
    {
        [Header("--- Start Screen Buttons ---")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;

        [Header("--- Animatable Elements ---")]
        [SerializeField] private RectTransform headlineRect;
        [SerializeField] private RectTransform playButtonRect;
        [SerializeField] private RectTransform catRect;
        [SerializeField] private RectTransform pandaRect;
        [SerializeField] private RectTransform birdRect;
        [SerializeField] private CanvasGroup topLeftButtonsCG;
        [SerializeField] private CanvasGroup topRightButtonsCG;

        private Tween _pulseTween;
        private Sequence _entranceSequence;

        protected override void OnBeforeShow()
        {
            // Reset states before animation
            if (headlineRect != null) headlineRect.localScale = Vector3.zero;
            if (playButtonRect != null) playButtonRect.localScale = Vector3.zero;
            if (catRect != null) catRect.localScale = Vector3.zero;
            if (pandaRect != null) pandaRect.localScale = Vector3.zero;
            if (birdRect != null) birdRect.localScale = Vector3.zero;
            
            if (topLeftButtonsCG != null) topLeftButtonsCG.alpha = 0f;
            if (topRightButtonsCG != null) topRightButtonsCG.alpha = 0f;

            _entranceSequence?.Kill();
            _pulseTween?.Kill();

            // Bind buttons
            if (playButton != null)
            {
                if (playButton != null) { playButton.onClick.RemoveListener(OnPlayButtonClicked); playButton.onClick.AddListener(OnPlayButtonClicked); }
            }

            if (settingsButton != null)
            {
                if (settingsButton != null) { settingsButton.onClick.RemoveListener(OnSettingsButtonClicked); settingsButton.onClick.AddListener(OnSettingsButtonClicked); }
            }
        }

        protected override void OnShown()
        {
            // Sequence of animations
            _entranceSequence?.Kill();
            Sequence seq = _entranceSequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDisable);

            if (headlineRect != null)
            {
                seq.Append(headlineRect.DOScale(1.1591f, 0.5f).SetEase(Ease.OutBack)); // Default scale is 1.1591 in scene
            }

            if (playButtonRect != null)
            {
                // Play button scale default in scene is 0.8
                seq.Join(playButtonRect.DOScale(0.8f, 0.6f).SetEase(Ease.OutBack).OnComplete(StartPlayButtonPulse));
            }

            if (catRect != null)
            {
                seq.Join(catRect.DOScale(1f, 0.5f).SetEase(Ease.OutBack).SetDelay(0.1f));
            }

            if (pandaRect != null)
            {
                seq.Join(pandaRect.DOScale(1f, 0.5f).SetEase(Ease.OutBack).SetDelay(0.15f));
            }

            if (birdRect != null)
            {
                seq.Join(birdRect.DOScale(1f, 0.5f).SetEase(Ease.OutBack).SetDelay(0.2f));
            }

            if (topLeftButtonsCG != null)
            {
                seq.Join(topLeftButtonsCG.DOFade(1f, 0.4f).SetDelay(0.25f));
            }

            if (topRightButtonsCG != null)
            {
                seq.Join(topRightButtonsCG.DOFade(1f, 0.4f).SetDelay(0.25f));
            }
        }

        private void StartPlayButtonPulse()
        {
            if (playButtonRect == null) return;
            
            _pulseTween = playButtonRect.DOScale(0.85f, 0.6f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
        }

        private void OnPlayButtonClicked()
        {
            _entranceSequence?.Kill();
            _pulseTween?.Kill();
            GameAppFlowManager.Instance.EnterMainMenu();
        }

        private void OnSettingsButtonClicked()
        {
            UIManager.Instance.ShowPopup<SettingsPopup>();
        }

        protected override void OnBeforeHide()
        {
            _entranceSequence?.Kill();
            _pulseTween?.Kill();
        }
        protected override void OnDisable()
        {
            if (DOTween.instance != null) { _entranceSequence?.Kill(); _pulseTween?.Kill(); }
            if (playButton != null) playButton.onClick.RemoveListener(OnPlayButtonClicked);
            if (settingsButton != null) settingsButton.onClick.RemoveListener(OnSettingsButtonClicked);
            base.OnDisable();
        }
    }
}
