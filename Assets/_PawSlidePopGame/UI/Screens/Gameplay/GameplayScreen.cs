using _PawSlidePopGame._Scripts.Feature.Match3.Flow;
using _PawSlidePopGame._Scripts.Core.System.GameFlow;
using _PawSlidePopGame._Scripts.Data.Events;
using _PawSlidePopGame._Scripts.Data.Events.Payloads;
using _PawSlidePopGame._Scripts.Feature.Match3.Presenter;
using _PawSlidePopGame._Scripts.UI.Base;
using _PawSlidePopGame._Scripts.UI.Components.HUD;
using _PawSlidePopGame.Scripts.DesignPattern.ObserverPattern;
using _PawSlidePopGame._Scripts.Feature.Match3.Boosters;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.UI.Screens.Gameplay
{
    public class GameplayScreen : BaseScreen
    {
        [SerializeField] private TopHUDPresenter topHUDPresenter;
        [SerializeField] private BottomHUDPresenter bottomHUDPresenter;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button skipSugarCrushButton;

        [Header("FX Prefabs")]
        [SerializeField] private FloatingScoreText floatingScoreTextPrefab;
        public FloatingScoreText FloatingScoreTextPrefab => floatingScoreTextPrefab;

        private GameplayPausePopupPresenter _pausePopupPresenter;

        public static GameplayScreen Instance { get; private set; }

        private System.Collections.Generic.List<SpriteRenderer> _backgroundRenderers;

        protected override void Awake()
        {
            Instance = this;
            base.Awake();
            EnsurePausePopupPresenter();
        }

        protected virtual void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnEnable()
        {
            EventManager<LogicGameEvent>.AddListener<InGameSubStateChangedPayload>(
                LogicGameEvent.InGameSubStateChanged,
                HandleSubStateChanged);

            RefreshPauseButtonState(GameFlowManager.Instance != null
                ? GameFlowManager.Instance.CurrentInGameSubState
                : InGameSubState.None);

            var boosterController = BoosterController.Instance;
            if (boosterController != null)
            {
                boosterController.OnActiveBoosterChanged -= HandleActiveBoosterChanged;
                boosterController.OnActiveBoosterChanged += HandleActiveBoosterChanged;
                HandleActiveBoosterChanged(boosterController.ActiveBooster);
            }
        }

        private void OnDisable()
        {
            EventManager<LogicGameEvent>.RemoveListener<InGameSubStateChangedPayload>(
                LogicGameEvent.InGameSubStateChanged,
                HandleSubStateChanged);

            var boosterController = BoosterController.Instance;
            if (boosterController != null)
            {
                boosterController.OnActiveBoosterChanged -= HandleActiveBoosterChanged;
            }
        }

        protected override void OnBeforeShow()
        {
            topHUDPresenter?.ResetView();
            bottomHUDPresenter?.ResetView();
            SetBackgroundDimmed(false, 0f);
            SetButtonDimmed(pauseButton, false, 0f);
            SetButtonDimmed(skipSugarCrushButton, false, 0f);

            BindButton(pauseButton, HandlePausePressed);
            BindButton(skipSugarCrushButton, HandleSkipSugarCrushPressed);
            if (skipSugarCrushButton != null)
            {
                skipSugarCrushButton.gameObject.SetActive(false);
            }
            RefreshPauseButtonState(GameFlowManager.Instance != null
                ? GameFlowManager.Instance.CurrentInGameSubState
                : InGameSubState.None);
        }

        protected override void OnBeforeHide()
        {
            topHUDPresenter?.ResetView();
            bottomHUDPresenter?.ResetView();
            SetBackgroundDimmed(false, 0f);
            SetButtonDimmed(pauseButton, false, 0f);
            SetButtonDimmed(skipSugarCrushButton, false, 0f);

            if (skipSugarCrushButton != null)
            {
                skipSugarCrushButton.transform.DOKill();
                var cg = skipSugarCrushButton.GetComponent<CanvasGroup>();
                if (cg != null) cg.DOKill();
                skipSugarCrushButton.gameObject.SetActive(false);
            }
            RefreshPauseButtonState(InGameSubState.None);
        }

        private void HandlePausePressed()
        {
            GameFlowManager.Instance?.PauseGameplay();
        }

        private void HandleSkipSugarCrushPressed()
        {
            var boardPresenter = Match3BoardPresenter.Instance;
            if (boardPresenter != null)
            {
                boardPresenter.SkipSugarCrush();
            }
            if (skipSugarCrushButton != null)
            {
                skipSugarCrushButton.transform.DOKill();
                var cg = skipSugarCrushButton.GetComponent<CanvasGroup>();
                if (cg != null) cg.DOKill();
                skipSugarCrushButton.gameObject.SetActive(false);
            }
        }

        private void HandleSubStateChanged(InGameSubStateChangedPayload payload)
        {
            RefreshPauseButtonState(payload.Current);
            if (payload.Current == InGameSubState.Victory)
            {
                int moves = GameFlowManager.Instance != null ? GameFlowManager.Instance.LastSnapshot?.remainingMoves ?? 0 : 0;
                if (skipSugarCrushButton != null)
                {
                    bool show = moves > 0;
                    skipSugarCrushButton.gameObject.SetActive(show);
                    if (show)
                    {
                        skipSugarCrushButton.transform.DOKill();
                        skipSugarCrushButton.transform.localScale = Vector3.zero;

                        var cg = skipSugarCrushButton.GetComponent<CanvasGroup>();
                        if (cg == null)
                        {
                            cg = skipSugarCrushButton.gameObject.AddComponent<CanvasGroup>();
                        }
                        cg.DOKill();
                        cg.alpha = 1f;

                        // Entry animation (Scale from 0 to 1 with Back ease)
                        skipSugarCrushButton.transform.DOScale(Vector3.one, 0.35f)
                            .SetEase(Ease.OutBack)
                            .SetLink(skipSugarCrushButton.gameObject);

                        // Pulse (blinking) alpha effect
                        cg.DOFade(0.7f, 0.5f)
                          .SetEase(Ease.InOutSine)
                          .SetLoops(-1, LoopType.Yoyo)
                          .SetLink(skipSugarCrushButton.gameObject);
                    }
                    else
                    {
                        skipSugarCrushButton.transform.DOKill();
                        var cg = skipSugarCrushButton.GetComponent<CanvasGroup>();
                        if (cg != null) cg.DOKill();
                    }
                }
            }
            else
            {
                if (skipSugarCrushButton != null)
                {
                    skipSugarCrushButton.transform.DOKill();
                    var cg = skipSugarCrushButton.GetComponent<CanvasGroup>();
                    if (cg != null) cg.DOKill();
                    skipSugarCrushButton.gameObject.SetActive(false);
                }
            }
        }

        private void RefreshPauseButtonState(InGameSubState subState)
        {
            if (pauseButton == null)
            {
                return;
            }

            pauseButton.interactable = GameFlowManager.IsInteractiveGameplaySubState(subState);
        }

        private void EnsurePausePopupPresenter()
        {
            _pausePopupPresenter = GetComponent<GameplayPausePopupPresenter>();
            if (_pausePopupPresenter == null)
            {
                _pausePopupPresenter = gameObject.AddComponent<GameplayPausePopupPresenter>();
            }
        }

        private void OnValidate()
        {
            if (topHUDPresenter == null)
            {
                topHUDPresenter = GetComponentInChildren<TopHUDPresenter>(true);
            }

            if (bottomHUDPresenter == null)
            {
                bottomHUDPresenter = GetComponentInChildren<BottomHUDPresenter>(true);
            }
        }

        private void HandleActiveBoosterChanged(BoosterDefinitionSO activeBooster)
        {
            bool isBoosterActive = activeBooster != null;

            // 1. Darken the background
            SetBackgroundDimmed(isBoosterActive);

            // 2. Darken HUD presenters
            topHUDPresenter?.SetDimmed(isBoosterActive);
            bottomHUDPresenter?.SetDimmed(isBoosterActive, activeBooster);

            // 3. Darken other screen buttons
            SetButtonDimmed(pauseButton, isBoosterActive);
            SetButtonDimmed(skipSugarCrushButton, isBoosterActive);
        }

        private void CacheBackgroundRenderers()
        {
            if (_backgroundRenderers != null) return;
            _backgroundRenderers = new System.Collections.Generic.List<SpriteRenderer>();

            var fitters = FindObjectsByType<SmartBackgroundFitter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var fitter in fitters)
            {
                if (fitter != null && fitter.TryGetComponent<SpriteRenderer>(out var sr))
                {
                    if (!_backgroundRenderers.Contains(sr))
                        _backgroundRenderers.Add(sr);
                }
            }

            var allSpriteRenderers = FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var sr in allSpriteRenderers)
            {
                if (sr == null || sr.GetComponent<SmartBackgroundFitter>() != null) continue;

                string lowerName = sr.name.ToLower();
                if (lowerName.Contains("background") || lowerName.Contains("bg"))
                {
                    if (!_backgroundRenderers.Contains(sr))
                        _backgroundRenderers.Add(sr);
                }
            }
        }

        private void SetBackgroundDimmed(bool isDimmed, float duration = 0.25f)
        {
            CacheBackgroundRenderers();
            Color targetColor = isDimmed ? new Color(0.35f, 0.35f, 0.35f, 1f) : Color.white;

            foreach (var sr in _backgroundRenderers)
            {
                if (sr != null)
                {
                    sr.DOKill();
                    sr.DOColor(targetColor, duration).SetEase(Ease.OutQuad).SetUpdate(true);
                }
            }
        }

        private void SetButtonDimmed(Button button, bool isDimmed, float duration = 0.25f)
        {
            if (button == null) return;

            var cg = button.GetComponent<CanvasGroup>();
            if (cg == null)
            {
                cg = button.gameObject.AddComponent<CanvasGroup>();
            }

            cg.DOKill();
            float targetAlpha = isDimmed ? 0.35f : 1f;
            if (duration > 0f && Application.isPlaying)
            {
                cg.DOFade(targetAlpha, duration).SetEase(Ease.OutQuad).SetUpdate(true);
            }
            else
            {
                cg.alpha = targetAlpha;
            }
            cg.blocksRaycasts = !isDimmed;
        }
    }
}
