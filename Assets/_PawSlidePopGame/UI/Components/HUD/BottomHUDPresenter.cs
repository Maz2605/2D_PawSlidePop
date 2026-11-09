using _PawSlidePopGame._Scripts.Core.System.GameFlow;
using _PawSlidePopGame._Scripts.Data.Events;
using _PawSlidePopGame._Scripts.Data.Events.Payloads;
using _PawSlidePopGame._Scripts.Feature.Match3.Flow;
using _PawSlidePopGame.Scripts.DesignPattern.ObserverPattern;
using _PawSlidePopGame._Scripts.Feature.Match3.Boosters;
using UnityEngine;
using DG.Tweening;

namespace _PawSlidePopGame._Scripts.UI.Components.HUD
{
    public class BottomHUDPresenter : MonoBehaviour
    {
        [SerializeField] private ChargedAbilityView chargedAbilityView;
        private bool _inputEnabled = true;

        private Vector3 _originalLocalPos;
        private bool _hasOriginalLocalPos;

        private void Awake()
        {
            EnsureView();
            CacheOriginalPosition();
        }

        private void CacheOriginalPosition()
        {
            if (!_hasOriginalLocalPos)
            {
                _originalLocalPos = transform.localPosition;
                _hasOriginalLocalPos = true;
            }
        }

        private void OnValidate()
        {
            if (chargedAbilityView == null)
            {
                chargedAbilityView = GetComponentInChildren<ChargedAbilityView>(true);
            }
        }

        private void OnEnable()
        {
            EventManager<LogicGameEvent>.AddListener<GameplayHudSnapshot>(LogicGameEvent.GameplayHudInitialized, HandleHudInitialized);
            EventManager<LogicGameEvent>.AddListener<GameplayHudSnapshot>(LogicGameEvent.GameplayHudStateChanged, HandleHudSnapshot);
            EventManager<LogicGameEvent>.AddListener<InGameSubStateChangedPayload>(
                LogicGameEvent.InGameSubStateChanged,
                HandleSubStateChanged);

            if (chargedAbilityView != null)
            {
                chargedAbilityView.OnUseRequested += HandleChargedUseRequested;
                chargedAbilityView.OnCancelRequested += HandleChargedCancelRequested;
            }

            SetInputEnabled(GameFlowManager.Instance != null &&
                            GameFlowManager.IsInteractiveGameplaySubState(GameFlowManager.Instance.CurrentInGameSubState));
        }

        private void OnDisable()
        {
            EventManager<LogicGameEvent>.RemoveListener<GameplayHudSnapshot>(LogicGameEvent.GameplayHudInitialized, HandleHudInitialized);
            EventManager<LogicGameEvent>.RemoveListener<GameplayHudSnapshot>(LogicGameEvent.GameplayHudStateChanged, HandleHudSnapshot);
            EventManager<LogicGameEvent>.RemoveListener<InGameSubStateChangedPayload>(
                LogicGameEvent.InGameSubStateChanged,
                HandleSubStateChanged);

            if (chargedAbilityView != null)
            {
                chargedAbilityView.OnUseRequested -= HandleChargedUseRequested;
                chargedAbilityView.OnCancelRequested -= HandleChargedCancelRequested;
            }
        }

        public void ResetView()
        {
            _inputEnabled = true;

            if (_hasOriginalLocalPos)
            {
                transform.DOKill();
                transform.localPosition = _originalLocalPos;
            }

            chargedAbilityView?.ResetView();

            if (chargedAbilityView != null)
            {
                var cg = chargedAbilityView.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.DOKill();
                    cg.alpha = 1f;
                    cg.blocksRaycasts = true;
                }
            }

            BoosterWidget boosterWidget = GetComponentInChildren<BoosterWidget>(true);
            if (boosterWidget != null)
            {
                boosterWidget.SetButtonsDimmed(false, null, 0f);
            }
        }

        public void SetDimmed(bool isDimmed, BoosterDefinitionSO selectedBooster, float duration = 0.25f)
        {
            if (chargedAbilityView != null)
            {
                var cg = chargedAbilityView.GetComponent<CanvasGroup>();
                if (cg == null)
                {
                    cg = chargedAbilityView.gameObject.AddComponent<CanvasGroup>();
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

            BoosterWidget boosterWidget = GetComponentInChildren<BoosterWidget>(true);
            if (boosterWidget != null)
            {
                boosterWidget.SetButtonsDimmed(isDimmed, selectedBooster, duration);
            }
        }

        private void HandleHudSnapshot(GameplayHudSnapshot snapshot)
        {
            chargedAbilityView?.SetData(snapshot?.chargedAbility);
            chargedAbilityView?.SetInputEnabled(_inputEnabled);
        }

        private void HandleChargedUseRequested()
        {
            GameFlowManager.Instance?.EnterChargedPlacementMode();
        }

        private void HandleChargedCancelRequested()
        {
            GameFlowManager.Instance?.CancelChargedAbilityMode();
        }

        private void HandleSubStateChanged(InGameSubStateChangedPayload payload)
        {
            SetInputEnabled(GameFlowManager.IsInteractiveGameplaySubState(payload.Current));
            if (payload.Current == InGameSubState.Victory)
            {
                PlayOutroAnimation();
            }
        }

        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;
            chargedAbilityView?.SetInputEnabled(enabled);
        }

        public void PlayOutroAnimation()
        {
            CacheOriginalPosition();
            transform.DOKill();
            Vector3 targetPos = _originalLocalPos - new Vector3(0f, 500f, 0f);
            transform.DOLocalMove(targetPos, 0.6f)
                .SetEase(Ease.InBack)
                .SetLink(gameObject);
        }

        private void HandleHudInitialized(GameplayHudSnapshot snapshot)
        {
            HandleHudSnapshot(snapshot);
            PlayIntroAnimation();
        }

        public void PlayIntroAnimation()
        {
            CacheOriginalPosition();
            transform.DOKill();
            transform.localPosition = _originalLocalPos - new Vector3(0f, 400f, 0f);
            transform.DOLocalMove(_originalLocalPos, 0.8f)
                .SetEase(Ease.OutBack)
                .SetLink(gameObject);

            BoosterWidget boosterWidget = GetComponentInChildren<BoosterWidget>(true);
            if (boosterWidget != null)
            {
                boosterWidget.PlayIntroAnimation();
            }
        }

        private void EnsureView()
        {
            if (chargedAbilityView != null)
            {
                return;
            }

            GameObject chargedHudObject = new GameObject("ChargedAbilityView", typeof(RectTransform));
            chargedHudObject.transform.SetParent(transform, false);
            chargedAbilityView = chargedHudObject.AddComponent<ChargedAbilityView>();
        }
    }
}
