using _PawSlidePopGame._Scripts.Core.System.GameFlow;
using _PawSlidePopGame._Scripts.Data.Events;
using _PawSlidePopGame._Scripts.Data.Events.Payloads;
using _PawSlidePopGame._Scripts.Feature.Match3.Flow;
using _PawSlidePopGame._Scripts.Feature.Match3.Presenter;
using _PawSlidePopGame.Scripts.DesignPattern.ObserverPattern;
using DG.Tweening;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.UI.Components.HUD
{
    public class TopHUDPresenter : MonoBehaviour
    {
        [SerializeField] private MovesCounterView movesCounterView;
        [SerializeField] private TargetListView targetListView;
        [SerializeField] private LevelProgressView levelProgressView;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform flyContainer;

        public RectTransform FlyContainer => flyContainer != null ? flyContainer : (transform as RectTransform);

        private GameplayHudSnapshot _currentSnapshot;
        private Vector3 _originalLocalPos;
        private bool _hasOriginalLocalPos;

        public static TopHUDPresenter Instance { get; private set; }

        public MovesCounterView MovesCounterView => movesCounterView;
        public LevelProgressView LevelProgressView => levelProgressView;

        private void Awake()
        {
            Instance = this;
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            CacheOriginalPosition();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
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
            if (movesCounterView == null)
            {
                movesCounterView = GetComponentInChildren<MovesCounterView>(true);
            }

            if (targetListView == null)
            {
                targetListView = GetComponentInChildren<TargetListView>(true);
            }

            if (levelProgressView == null)
            {
                levelProgressView = GetComponentInChildren<LevelProgressView>(true);
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }
        }

        private void OnEnable()
        {
            EventManager<LogicGameEvent>.AddListener<GameplayHudSnapshot>(LogicGameEvent.GameplayHudInitialized, HandleHudInitialized);
            EventManager<LogicGameEvent>.AddListener<GameplayHudSnapshot>(LogicGameEvent.GameplayHudStateChanged, HandleHudSnapshot);
            EventManager<LogicGameEvent>.AddListener<RemainingMovesChangedPayload>(LogicGameEvent.GameplayMovesChanged, HandleMovesChanged);
            EventManager<LogicGameEvent>.AddListener<ScoreChangedPayload>(LogicGameEvent.GameplayScoreChanged, HandleScoreChanged);
            EventManager<LogicGameEvent>.AddListener<InGameSubStateChangedPayload>(LogicGameEvent.InGameSubStateChanged, HandleSubStateChanged);
            EventManager<VisualGameEvent>.AddListener<TargetProgressChangedPayload>(VisualGameEvent.TopHudTargetProgressFx, HandleTargetProgressFx);
            EventManager<VisualGameEvent>.AddListener<StarReachedPayload>(VisualGameEvent.TopHudStarReachedFx, HandleStarReachedFx);
            EventManager<VisualGameEvent>.AddListener(VisualGameEvent.TopHudTargetsCompletedFx, HandleTargetsCompletedFx);
            EventManager<VisualGameEvent>.AddListener<TargetCollectedFxPayload>(VisualGameEvent.TopHudTargetCollectedFx, HandleTargetCollectedFx);
        }

        private void HandleHudSnapshot(GameplayHudSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            _currentSnapshot = snapshot.Clone();
            movesCounterView?.SetValue(_currentSnapshot.remainingMoves);
            targetListView?.SetTargets(_currentSnapshot.targets);

            var boardPresenter = Match3BoardPresenter.Instance;
            if (boardPresenter != null && boardPresenter.IsVictoryOutroPlaying)
            {
                return;
            }

            levelProgressView?.SetData(_currentSnapshot);
        }

        private void OnDisable()
        {
            EventManager<LogicGameEvent>.RemoveListener<GameplayHudSnapshot>(LogicGameEvent.GameplayHudInitialized, HandleHudInitialized);
            EventManager<LogicGameEvent>.RemoveListener<GameplayHudSnapshot>(LogicGameEvent.GameplayHudStateChanged, HandleHudSnapshot);
            EventManager<LogicGameEvent>.RemoveListener<RemainingMovesChangedPayload>(LogicGameEvent.GameplayMovesChanged, HandleMovesChanged);
            EventManager<LogicGameEvent>.RemoveListener<ScoreChangedPayload>(LogicGameEvent.GameplayScoreChanged, HandleScoreChanged);
            EventManager<LogicGameEvent>.RemoveListener<InGameSubStateChangedPayload>(LogicGameEvent.InGameSubStateChanged, HandleSubStateChanged);
            EventManager<VisualGameEvent>.RemoveListener<TargetProgressChangedPayload>(VisualGameEvent.TopHudTargetProgressFx, HandleTargetProgressFx);
            EventManager<VisualGameEvent>.RemoveListener<StarReachedPayload>(VisualGameEvent.TopHudStarReachedFx, HandleStarReachedFx);
            EventManager<VisualGameEvent>.RemoveListener(VisualGameEvent.TopHudTargetsCompletedFx, HandleTargetsCompletedFx);
            EventManager<VisualGameEvent>.RemoveListener<TargetCollectedFxPayload>(VisualGameEvent.TopHudTargetCollectedFx, HandleTargetCollectedFx);
        }

        public void ResetView()
        {
            _currentSnapshot = null;

            if (_hasOriginalLocalPos)
            {
                transform.DOKill();
                transform.localPosition = _originalLocalPos;
            }

            if (canvasGroup != null)
            {
                canvasGroup.DOKill();
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }

            movesCounterView?.ResetView();
            targetListView?.ResetView();
            levelProgressView?.ResetView();
        }

        public void SetDimmed(bool isDimmed, float duration = 0.25f)
        {
            if (canvasGroup != null)
            {
                canvasGroup.DOKill();
                float targetAlpha = isDimmed ? 0.35f : 1f;
                if (duration > 0f && Application.isPlaying)
                {
                    canvasGroup.DOFade(targetAlpha, duration).SetEase(Ease.OutQuad).SetUpdate(true);
                }
                else
                {
                    canvasGroup.alpha = targetAlpha;
                }
                canvasGroup.blocksRaycasts = !isDimmed;
            }
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
            transform.localPosition = _originalLocalPos + new Vector3(0f, 400f, 0f);
            transform.DOLocalMove(_originalLocalPos, 0.8f)
                .SetEase(Ease.OutBack)
                .SetLink(gameObject);
        }

        

        private void HandleSubStateChanged(InGameSubStateChangedPayload payload)
        {
            bool paused = payload.Current == InGameSubState.Paused;
            canvasGroup.DOKill();
            canvasGroup.DOFade(paused ? 0.85f : 1f, 0.15f)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        private void HandleTargetProgressFx(TargetProgressChangedPayload payload)
        {
            UpdateSnapshotTargetProgress(payload);
            // targetListView?.PlayProgressFx(payload); // Trì hoãn cho đến khi clone bay tới đích
        }

        private void HandleTargetCollectedFx(TargetCollectedFxPayload payload)
        {
            if (targetListView == null)
            {
                return;
            }

            TargetItemView targetItem = targetListView.GetTargetItemView(payload.TileId);
            if (targetItem == null)
            {
                return;
            }

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return;
            }

            RectTransform flyRoot = flyContainer != null ? flyContainer : (transform as RectTransform);

            // Tạo clone động
            GameObject cloneObj = new GameObject("TargetCollectedClone", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            cloneObj.transform.SetParent(flyRoot, false);

            UnityEngine.UI.Image cloneImage = cloneObj.GetComponent<UnityEngine.UI.Image>();
            cloneImage.sprite = targetItem.IconSprite;
            cloneImage.raycastTarget = false;

            RectTransform cloneRect = cloneObj.GetComponent<RectTransform>();
            cloneRect.sizeDelta = targetItem.IconSize;

            // Chuyển vị trí world của tile bị phá sang vị trí local của flyRoot
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(Camera.main, payload.WorldPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                flyRoot,
                screenPoint,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                out Vector2 startLocalPos);

            cloneRect.anchoredPosition = startLocalPos;
            cloneRect.localScale = Vector3.zero;

            // Chuyển vị trí icon mục tiêu sang vị trí local của flyRoot
            Vector3 targetWorldPos = targetItem.IconWorldPosition;
            Vector3 targetLocalPos = flyRoot.InverseTransformPoint(targetWorldPos);

            // Chạy hiệu ứng bay
            Sequence seq = DOTween.Sequence().SetLink(cloneObj);

            // Hiệu ứng Pop nhẹ
            seq.Append(cloneRect.DOScale(1.3f, 0.22f).SetEase(Ease.OutBack));
            seq.Join(cloneRect.DOBlendableLocalMoveBy(new Vector3(UnityEngine.Random.Range(-30f, 30f), UnityEngine.Random.Range(40f, 70f), 0f), 0.25f).SetEase(Ease.OutQuad));

            // Bay tới mục tiêu trên HUD
            seq.AppendInterval(0.04f);
            seq.Append(cloneRect.DOLocalMove(targetLocalPos, 0.48f).SetEase(Ease.InQuad));
            seq.Join(cloneRect.DOScale(0.85f, 0.48f).SetEase(Ease.InQuad));

            seq.OnComplete(() =>
            {
                // Cập nhật số lượng và nảy mục tiêu trên HUD khi clone chạm tới
                if (targetItem != null)
                {
                    targetItem.PlayProgressFx(new TargetProgressChangedPayload(
                        payload.TileId,
                        payload.PreviousCount,
                        payload.CurrentCount,
                        payload.RequiredCount,
                        payload.JustCompleted));
                }
                Destroy(cloneObj);
            });
        }

        private void HandleStarReachedFx(StarReachedPayload payload)
        {
            var boardPresenter = Match3BoardPresenter.Instance;
            if (boardPresenter != null && boardPresenter.IsVictoryOutroPlaying)
            {
                return;
            }

            levelProgressView?.PlayStarReachedFx(payload);
        }

        private void HandleTargetsCompletedFx()
        {
            targetListView?.PlayCompletedFx();
        }

        private void HandleMovesChanged(RemainingMovesChangedPayload payload)
        {
            if (_currentSnapshot == null)
            {
                return;
            }

            _currentSnapshot.remainingMoves = payload.CurrentMoves;
            movesCounterView?.SetValue(payload.CurrentMoves);
            movesCounterView?.PlayValueChangedFx();
        }

        private void HandleScoreChanged(ScoreChangedPayload payload)
        {
            if (_currentSnapshot == null)
            {
                return;
            }

            _currentSnapshot.currentScore = payload.CurrentScore;
            _currentSnapshot.reachedStars = GameplayHudSnapshotBuilder.CountReachedStars(payload.CurrentScore, _currentSnapshot.starScoreThresholds);

            var boardPresenter = Match3BoardPresenter.Instance;
            if (boardPresenter != null && boardPresenter.IsVictoryOutroPlaying)
            {
                return;
            }

            levelProgressView?.PlayScoreChangedFx(payload);
        }

        private void UpdateSnapshotTargetProgress(TargetProgressChangedPayload payload)
        {
            if (_currentSnapshot?.targets == null)
            {
                return;
            }

            for (int i = 0; i < _currentSnapshot.targets.Count; i++)
            {
                TargetProgressData target = _currentSnapshot.targets[i];
                if (target == null || target.tileId != payload.TileId)
                {
                    continue;
                }

                target.currentCount = payload.CurrentCount;
                target.requiredCount = payload.RequiredCount;
                target.isCompleted = payload.CurrentCount >= payload.RequiredCount;
                return;
            }
        }
    }
}
