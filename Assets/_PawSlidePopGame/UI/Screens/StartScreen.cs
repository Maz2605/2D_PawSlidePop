using System;
using System.Reflection;
using _PawSlidePopGame._Scripts.Core.System.GameFlow;
using _PawSlidePopGame._Scripts.UI.Base;
using _PawSlidePopGame._Scripts.UI.Manager;
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

        protected override void Awake()
        {
            base.Awake();
            RegisterSelfToUIManager();
        }

        private void RegisterSelfToUIManager()
        {
            try
            {
                var uiManager = UIManager.Instance;
                if (uiManager != null)
                {
                    // Register ourselves in _screenCache using Reflection
                    var cacheField = typeof(UIManager).GetField("_screenCache", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (cacheField != null)
                    {
                        var cache = (System.Collections.Generic.Dictionary<Type, BaseScreen>)cacheField.GetValue(uiManager);
                        if (cache != null)
                        {
                            cache[typeof(StartScreen)] = this;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StartScreen] Failed to dynamically register with UIManager: {ex.Message}");
            }
        }

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

            _pulseTween?.Kill();

            // Bind buttons
            if (playButton != null)
            {
                BindButton(playButton, OnPlayButtonClicked);
            }

            if (settingsButton != null)
            {
                BindButton(settingsButton, OnSettingsButtonClicked);
            }
        }

        protected override void OnAfterShow()
        {
            // Sequence of animations
            Sequence seq = DOTween.Sequence().SetUpdate(true);

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
            _pulseTween?.Kill();
            GameAppFlowManager.Instance.EnterMainMenu();
        }

        private void OnSettingsButtonClicked()
        {
            UIManager.Instance.ShowPopup<SettingsPopup>();
        }

        protected override void OnBeforeHide()
        {
            _pulseTween?.Kill();
        }
    }
}
