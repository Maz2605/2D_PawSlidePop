using System.Collections.Generic;
using TMPro;
using _PawSlidePopGame._Scripts.UI.Components.HUD;
using _PawSlidePopGame._Scripts.Core.Vibration;
using _PawSlidePopGame._Scripts.UI.Screens.Gameplay;
using _PawSlidePopGame._Scripts.Core.System.GameFlow;
using _PawSlidePopGame._Scripts.Data.Events;
using _PawSlidePopGame._Scripts.Data.Events.Payloads;
using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;
using _PawSlidePopGame._Scripts.Feature.Match3.Boosters;
using _PawSlidePopGame._Scripts.Feature.Match3.Flow;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Move;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Resolution;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Entities;
using _PawSlidePopGame._Scripts.Feature.Match3.Presentation;
using _PawSlidePopGame._Scripts.Feature.Match3.View;
using _PawSlidePopGame.Scripts.DesignPattern.ObserverPattern;
using DG.Tweening;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Presenter
{
    public class Match3BoardPresenter : MonoBehaviour
    {
        [SerializeField] private Match3GameManager gameManager;
        [SerializeField] private Match3BoardView boardView;
        [SerializeField] private Match3InputController inputController;
        [SerializeField] private BoosterController boosterController;

        private Coroutine _movePlaybackRoutine;
        private bool _isAnimatingMove;
        private bool _isAnimatingIntro;
        private bool _isBoardBound;

        public bool IsVictoryOutroPlaying { get; private set; }
        private bool _skipSugarCrush;

        [Header("Sugar Crush Settings")]
        [SerializeField] private float sugarCrushSpawnDelay = 0.05f;
        [SerializeField] private float sugarCrushExplodeDelay = 0.05f;
        [SerializeField] private int scorePerRemainingMove = 1000;

        [SerializeField] private float idleHintDelay = 15f;
        private float _idleTimer = 0f;
        private bool _isTrackingIdle = false;
        private bool _hintShown = false;

        public static Match3BoardPresenter Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            if (gameManager == null)
            {
                gameManager = GetComponent<Match3GameManager>();
            }

            if (boardView == null)
            {
                boardView = GetComponentInChildren<Match3BoardView>();
            }

            if (inputController == null)
            {
                inputController = GetComponent<Match3InputController>();
            }

            if (boosterController == null)
            {
                boosterController = BoosterController.Instance;
            }
        }

        private void OnEnable()
        {
            if (gameManager != null)
            {
                gameManager.OnBoardInitialized += HandleBoardInitialized;
            }

            if (boardView != null)
            {
                boardView.OnTileActivatePlaybackStarted += HandleTileActivatePlaybackStarted;
                boardView.OnTileClearPlaybackStarted += HandleTileClearPlaybackStarted;
                boardView.OnSpecialCreatePlaybackStarted += HandleSpecialCreatePlaybackStarted;
                boardView.OnScoreGainPlaybackStarted += HandleScoreGainPlaybackStarted;
            }

            if (inputController != null)
            {
                inputController.OnMoveRequested += HandleMoveRequested;
                inputController.OnTileTapped += HandleTileTapped;
                inputController.OnPreviewStarted += HandlePreviewStarted;
                inputController.OnPreviewUpdated += HandlePreviewUpdated;
                inputController.OnPreviewCleared += HandlePreviewCleared;
                inputController.OnLongPressStarted += HandleLongPressStarted;
            }

            if (boosterController != null)
            {
                boosterController.OnExecutionRequested += HandleBoosterExecutionRequested;
                boosterController.OnActiveBoosterChanged += HandleActiveBoosterChanged;
            }

            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.OnAutoShuffleTriggered += HandleAutoShuffleTriggered;
            }

            EventManager<LogicGameEvent>.AddListener<InGameSubStateChangedPayload>(
                LogicGameEvent.InGameSubStateChanged,
                HandleSubStateChanged);
        }

        private void Start()
        {
            if (gameManager != null && gameManager.IsInitialized)
            {
                HandleBoardInitialized(gameManager.Board);
            }

            if (GameFlowManager.Instance != null)
            {
                ApplyInputState(GameFlowManager.Instance.CurrentInGameSubState);
                GameFlowManager.Instance.OnAutoShuffleTriggered -= HandleAutoShuffleTriggered;
                GameFlowManager.Instance.OnAutoShuffleTriggered += HandleAutoShuffleTriggered;
            }
        }

        private void OnDisable()
        {
            if (gameManager != null)
            {
                gameManager.OnBoardInitialized -= HandleBoardInitialized;
            }

            if (boardView != null)
            {
                boardView.OnTileActivatePlaybackStarted -= HandleTileActivatePlaybackStarted;
                boardView.OnTileClearPlaybackStarted -= HandleTileClearPlaybackStarted;
                boardView.OnSpecialCreatePlaybackStarted -= HandleSpecialCreatePlaybackStarted;
                boardView.OnScoreGainPlaybackStarted -= HandleScoreGainPlaybackStarted;
            }

            if (inputController != null)
            {
                inputController.OnMoveRequested -= HandleMoveRequested;
                inputController.OnTileTapped -= HandleTileTapped;
                inputController.OnPreviewStarted -= HandlePreviewStarted;
                inputController.OnPreviewUpdated -= HandlePreviewUpdated;
                inputController.OnPreviewCleared -= HandlePreviewCleared;
                inputController.OnLongPressStarted -= HandleLongPressStarted;
            }

            if (boosterController != null)
            {
                boosterController.OnExecutionRequested -= HandleBoosterExecutionRequested;
                boosterController.OnActiveBoosterChanged -= HandleActiveBoosterChanged;
            }

            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.OnAutoShuffleTriggered -= HandleAutoShuffleTriggered;
            }

            EventManager<LogicGameEvent>.RemoveListener<InGameSubStateChangedPayload>(
                LogicGameEvent.InGameSubStateChanged,
                HandleSubStateChanged);
        }

        private void HandleBoardInitialized(BoardModel board)
        {
            if (boardView == null || gameManager == null || _isBoardBound)
            {
                return;
            }

            _isBoardBound = true;
            boardView.Bind(board, gameManager.LevelData, gameManager.TileDatabase);

            if (inputController != null)
            {
                inputController.Bind(boardView);
            }

            StartCoroutine(PlayIntroAnimationRoutine());
        }

        private System.Collections.IEnumerator PlayIntroAnimationRoutine()
        {
            _isAnimatingIntro = true;
            ApplyInputState(GameFlowManager.Instance != null ? GameFlowManager.Instance.CurrentInGameSubState : InGameSubState.PreparingBoard);

            if (boardView != null)
            {
                yield return StartCoroutine(boardView.PlayIntroAnimation());
            }

            yield return StartCoroutine(PlayPreLevelBoostersIntroAnimation());

            _isAnimatingIntro = false;
            if (GameFlowManager.Instance != null)
            {
                ApplyInputState(GameFlowManager.Instance.CurrentInGameSubState);
            }
        }

        private System.Collections.IEnumerator PlayPreLevelBoostersIntroAnimation()
        {
            if (gameManager == null || gameManager.PreLevelBoosters == null || gameManager.PreLevelBoosters.Count == 0)
            {
                yield break;
            }

            TopHUDPresenter topHud = TopHUDPresenter.Instance;
            if (topHud == null)
            {
                yield break;
            }

            RectTransform flyRoot = topHud.FlyContainer;
            Canvas canvas = flyRoot != null ? flyRoot.GetComponentInParent<Canvas>() : null;
            if (canvas == null)
            {
                yield break;
            }

            MovesCounterView movesCounter = TopHUDPresenter.Instance != null ? TopHUDPresenter.Instance.MovesCounterView : null;
            Vector3 movesTargetPosWorld = movesCounter != null ? movesCounter.transform.position : Vector3.zero;
            Vector3 movesTargetPosLocal = Vector3.zero;
            if (movesCounter != null)
            {
                movesTargetPosLocal = flyRoot.InverseTransformPoint(movesTargetPosWorld);
            }

            HashSet<CellModel> occupiedCells = new HashSet<CellModel>();

            for (int i = 0; i < gameManager.PreLevelBoosters.Count; i++)
            {
                BoosterDefinitionSO definition = gameManager.PreLevelBoosters[i];
                if (definition == null) continue;

                Vector3 targetLocalPos = Vector3.zero;
                CellModel targetCell = null;

                if (definition.PreLevelEffectType == PreLevelBoosterEffectType.ExtraMoves)
                {
                    if (movesCounter == null) continue;
                    targetLocalPos = movesTargetPosLocal;
                }
                else if (definition.PreLevelEffectType == PreLevelBoosterEffectType.PlaceStartingTile)
                {
                    targetCell = gameManager.FindStartingTileCell(occupiedCells);
                    if (targetCell == null) continue;
                    occupiedCells.Add(targetCell);

                    Vector3 cellWorldPos = boardView.GetCellWorldPosition(targetCell.X, targetCell.Y);
                    Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(Camera.main, cellWorldPos);
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        flyRoot,
                        screenPoint,
                        canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                        out Vector2 localPos);
                    targetLocalPos = localPos;
                }
                else
                {
                    continue;
                }

                // Create the fly icon UI object
                GameObject flyObj = new GameObject("PreLevelBoosterFly", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                flyObj.transform.SetParent(flyRoot, false);

                UnityEngine.UI.Image image = flyObj.GetComponent<UnityEngine.UI.Image>();
                image.sprite = definition.Icon;
                image.raycastTarget = false;

                RectTransform rectTransform = flyObj.GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(120f, 120f);

                // Spawn at screen center in UI local space
                Vector2 screenCenterLocal;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    flyRoot,
                    new Vector2(Screen.width * 0.5f, Screen.height * 0.5f),
                    canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                    out screenCenterLocal);
                rectTransform.anchoredPosition = screenCenterLocal;
                rectTransform.localScale = Vector3.zero;

                // Play Audio and pop up at center
                EventManager<FeedbackEvent>.Post(FeedbackEvent.BoosterSelect);

                bool reached = false;
                Sequence seq = DOTween.Sequence().SetLink(flyObj);
                seq.Append(rectTransform.DOScale(1.5f, 0.35f).SetEase(Ease.OutBack))
                   .AppendInterval(0.3f)
                   .Append(rectTransform.DOLocalMove(targetLocalPos, 0.55f).SetEase(Ease.InBack))
                   .Join(rectTransform.DOScale(0.7f, 0.55f).SetEase(Ease.InQuad))
                   .OnComplete(() => reached = true);

                while (!reached)
                {
                    yield return null;
                }

                if (definition.PreLevelEffectType == PreLevelBoosterEffectType.ExtraMoves)
                {
                    int previousMoves = gameManager.Board.RemainingMoves;
                    gameManager.Board.AddMoves(definition.ExtraMovesAmount);
                    
                    EventManager<FeedbackEvent>.Post(FeedbackEvent.MoveSuccess);

                    EventManager<LogicGameEvent>.Post(
                        LogicGameEvent.GameplayMovesChanged,
                        new RemainingMovesChangedPayload(previousMoves, gameManager.Board.RemainingMoves));
                    GameFlowManager.Instance?.ForcePublishHudState();
                }
                else if (definition.PreLevelEffectType == PreLevelBoosterEffectType.PlaceStartingTile)
                {
                    gameManager.PlaceStartingTileAt(targetCell, definition.StartingTileId);
                    boardView.SyncToBoardState();

                    // Find corresponding LogicType for feedback
                    TileLogicType tileLogicType = TileLogicType.BombBooster;
                    if (gameManager.TileDatabase != null)
                    {
                        var contentDef = gameManager.TileDatabase.GetContentDefinition(definition.StartingTileId);
                        if (contentDef is TileDefinitionSO tileDef)
                        {
                            tileLogicType = tileDef.LogicType;
                        }
                    }

                    EventManager<FeedbackEvent>.Post(FeedbackEvent.SpecialTileCreated, new SpecialTileFeedbackPayload(tileLogicType));

                    TileModel tile = targetCell.Tile;
                    if (tile != null)
                    {
                        Match3TileView tileView = boardView.GetTileView(tile.InstanceId);
                        if (tileView != null)
                        {
                            tileView.transform.DOKill(true);
                            tileView.transform.localScale = Vector3.zero;
                            tileView.transform.DOScale(Vector3.one, 0.45f).SetEase(Ease.OutBack);
                        }
                    }
                }

                Destroy(flyObj);
                yield return new WaitForSeconds(0.2f);
            }
        }

        public void ResetPresentation()
        {
            _isBoardBound = false;
            if (_movePlaybackRoutine != null)
            {
                StopCoroutine(_movePlaybackRoutine);
                _movePlaybackRoutine = null;
            }

            _isAnimatingMove = false;
            inputController?.SetInputMode(Match3InputController.BoardInputMode.Disabled);
            inputController?.Bind(null);
            boardView?.ClearPreview();
            boardView?.SetIdleEnabled(false);
            boardView?.ClearBoardVisuals();
        }

        private void Update()
        {
            if (_isTrackingIdle && !_hintShown)
            {
                _idleTimer += Time.deltaTime;
                if (_idleTimer >= idleHintDelay)
                {
                    ShowIdleHint();
                }
            }
        }

        private void ShowIdleHint()
        {
            if (gameManager == null || gameManager.Board == null || gameManager.RuleSet == null || boardView == null)
            {
                return;
            }

            BoardMoveRequest? hint = BoardResolutionService.FindPossibleMove(gameManager.Board, gameManager.RuleSet.MatchRule);
            if (hint.HasValue)
            {
                _hintShown = true;
                boardView.HighlightHint(hint.Value);
            }
        }

        private void ResetIdleTimer()
        {
            _idleTimer = 0f;
            if (_hintShown)
            {
                _hintShown = false;
                boardView?.ClearPreview();
            }
        }

        private void HandleMoveRequested(BoardMoveRequest request)
        {
            ResetIdleTimer();
            if (_isAnimatingMove || GameFlowManager.Instance == null)
            {
                return;
            }

            if (boosterController != null && boosterController.TryHandleLineSwipe(request))
            {
                return;
            }

            if (_movePlaybackRoutine != null)
            {
                StopCoroutine(_movePlaybackRoutine);
            }

            _movePlaybackRoutine = StartCoroutine(PlayExecutionRoutine(() => GameFlowManager.Instance != null
                ? GameFlowManager.Instance.RequestMove(request)
                : new BoardMoveExecutionResult()));
        }

        private void HandleTileTapped(CellModel cell)
        {
            ResetIdleTimer();
            if (_isAnimatingMove || cell == null || GameFlowManager.Instance == null)
            {
                return;
            }

            if (boosterController != null && boosterController.TryHandleTileTap(cell))
            {
                return;
            }

            if (GameFlowManager.Instance.IsInChargedPlacementMode)
            {
                if (!GameFlowManager.Instance.CanPlaceChargedBoosterAt(cell.X, cell.Y))
                {
                    EventManager<FeedbackEvent>.Post(FeedbackEvent.MoveReject);
                    return;
                }

                if (_movePlaybackRoutine != null)
                {
                    StopCoroutine(_movePlaybackRoutine);
                }

                int placementX = cell.X;
                int placementY = cell.Y;
                _movePlaybackRoutine = StartCoroutine(PlayExecutionRoutine(() => GameFlowManager.Instance != null
                    ? GameFlowManager.Instance.RequestChargedPlacement(placementX, placementY)
                    : new BoardMoveExecutionResult()));
                return;
            }

            if (GameFlowManager.Instance.IsInChargedComboMode)
            {
                if (!GameFlowManager.Instance.CanConfirmChargedComboAt(cell.X, cell.Y))
                {
                    EventManager<FeedbackEvent>.Post(FeedbackEvent.MoveReject);
                    GameFlowManager.Instance.CancelChargedAbilityMode();
                    return;
                }

                if (_movePlaybackRoutine != null)
                {
                    StopCoroutine(_movePlaybackRoutine);
                }

                int comboX = cell.X;
                int comboY = cell.Y;
                _movePlaybackRoutine = StartCoroutine(PlayExecutionRoutine(() => GameFlowManager.Instance != null
                    ? GameFlowManager.Instance.RequestChargedComboActivation(comboX, comboY)
                    : new BoardMoveExecutionResult()));
                return;
            }

            if (cell.Tile == null || !cell.CanTileActivate())
            {
                return;
            }

            if (cell.Tile.LogicType == Core.Enum.TileLogicType.ChargedSweepBooster &&
                GameFlowManager.Instance.TryEnterChargedComboMode(cell.X, cell.Y))
            {
                return;
            }

            if (_movePlaybackRoutine != null)
            {
                StopCoroutine(_movePlaybackRoutine);
            }

            int x = cell.X;
            int y = cell.Y;
            _movePlaybackRoutine = StartCoroutine(PlayExecutionRoutine(() => GameFlowManager.Instance != null
                ? GameFlowManager.Instance.RequestTileActivation(x, y)
                : new BoardMoveExecutionResult()));
        }

        private void HandleBoosterExecutionRequested(System.Func<BoardMoveExecutionResult> executeAction)
        {
            ResetIdleTimer();
            if (_isAnimatingMove || executeAction == null)
            {
                return;
            }

            if (_movePlaybackRoutine != null)
            {
                StopCoroutine(_movePlaybackRoutine);
            }

            _movePlaybackRoutine = StartCoroutine(PlayExecutionRoutine(executeAction));
        }

        private void HandleAutoShuffleTriggered(BoardMoveExecutionResult result)
        {
            ResetIdleTimer();
            if (_movePlaybackRoutine != null)
            {
                StopCoroutine(_movePlaybackRoutine);
            }

            _movePlaybackRoutine = StartCoroutine(PlayExecutionRoutine(() => result));
        }

        private void HandleActiveBoosterChanged(BoosterDefinitionSO boosterDefinition)
        {
            if (_isAnimatingMove)
            {
                return;
            }

            boardView?.ClearPreview();

            if (boosterDefinition != null && boardView != null)
            {
                boardView.transform.DOKill();
                boardView.transform.localScale = Vector3.one;
                boardView.transform.DOPunchScale(Vector3.one * 0.02f, 0.25f, 5, 0.5f);
            }
        }

        private System.Collections.IEnumerator PlayExecutionRoutine(System.Func<BoardMoveExecutionResult> executeAction)
        {
            _isAnimatingMove = true;
            inputController?.SetInputLocked(true);
            boardView?.ClearPreview();
            boardView?.SetIdleEnabled(false);

            BoardMoveExecutionResult executionResult = executeAction != null
                ? executeAction.Invoke()
                : new BoardMoveExecutionResult();

            if (executionResult.IsApplied && boardView != null)
            {
                EventManager<FeedbackEvent>.Post(FeedbackEvent.MoveSuccess);
                yield return StartCoroutine(boardView.PlayMoveExecution(executionResult));
                boardView.SyncToBoardState();
            }
            else if (!executionResult.IsAccepted)
            {
                EventManager<FeedbackEvent>.Post(FeedbackEvent.MoveReject);
            }

            GameFlowManager.Instance?.NotifyResolutionPlaybackComplete(executionResult);
            _isAnimatingMove = false;
            _movePlaybackRoutine = null;
        }

        private void HandlePreviewStarted(CellModel cell)
        {
            ResetIdleTimer();
            if (_isAnimatingMove)
            {
                return;
            }

            boardView?.ShowPressPreview(cell);
        }

        private void HandlePreviewUpdated(BoardLinePreview preview)
        {
            ResetIdleTimer();
            if (_isAnimatingMove)
            {
                return;
            }

            boardView?.ShowLinePreview(preview);
        }

        private void HandlePreviewCleared()
        {
            ResetIdleTimer();
            if (_isAnimatingMove)
            {
                return;
            }

            boardView?.ClearPreview();
        }

        private void HandleLongPressStarted(CellModel cell)
        {
            ResetIdleTimer();
            if (_isAnimatingMove)
            {
                return;
            }

            boardView?.ShowRowColumnHighlight(cell);
        }

        private void HandleTileActivatePlaybackStarted(TileActivateOp activateOp)
        {
            if (activateOp == null)
            {
                return;
            }

            EventManager<FeedbackEvent>.Post(
                FeedbackEvent.SpecialTileActivated,
                new SpecialTileFeedbackPayload(activateOp.LogicType));
        }

        private void HandleTileClearPlaybackStarted(TileClearOp clearOp, Vector3 worldPosition)
        {
            GameFlowManager.Instance?.NotifyTileClearedDuringPlayback(clearOp, worldPosition);
        }

        private void HandleSpecialCreatePlaybackStarted(SpecialCreateOp specialCreateOp)
        {
            if (specialCreateOp == null)
            {
                return;
            }

            EventManager<FeedbackEvent>.Post(
                FeedbackEvent.SpecialTileCreated,
                new SpecialTileFeedbackPayload(specialCreateOp.LogicType));
        }

        private void HandleScoreGainPlaybackStarted(ScoreGainOp scoreGainOp)
        {
            GameFlowManager.Instance?.NotifyScoreGainedDuringPlayback(scoreGainOp);
        }

        private void HandleSubStateChanged(InGameSubStateChangedPayload payload)
        {
            ApplyInputState(payload.Current);

            if (payload.Current == InGameSubState.Defeat)
            {
                StartCoroutine(PlayDefeatOutroRoutine());
            }
            else if (payload.Current == InGameSubState.Victory)
            {
                StartCoroutine(PlayVictoryOutroRoutine());
            }
        }

        public void SkipSugarCrush()
        {
            _skipSugarCrush = true;
        }

        private System.Collections.IEnumerator PlayDefeatOutroRoutine()
        {
            _isAnimatingMove = true;
            inputController?.SetInputLocked(true);
            boardView?.SetIdleEnabled(false);
            boardView?.ClearPreview();

            if (boardView != null)
            {
                yield return StartCoroutine(boardView.PlayLoseOutro());
            }

            _isAnimatingMove = false;
        }

        private System.Collections.IEnumerator PlayVictoryOutroRoutine()
        {
            IsVictoryOutroPlaying = true;
            _isAnimatingMove = true;
            _skipSugarCrush = false;

            inputController?.SetInputLocked(true);
            boardView?.SetIdleEnabled(false);
            boardView?.ClearPreview();

            if (gameManager?.Board == null)
            {
                IsVictoryOutroPlaying = false;
                _isAnimatingMove = false;
                yield break;
            }

            int remainingMoves = gameManager.Board.RemainingMoves;
            System.Random random = gameManager.Random ?? new System.Random();

            // Lấy danh sách booster từ database
            List<BoosterTileDefinitionSO> boosterDefinitions = new List<BoosterTileDefinitionSO>();
            if (gameManager.TileDatabase != null)
            {
                var bombDef = gameManager.TileDatabase.GetBoosterDefinition(TileLogicType.BombBooster);
                var crossDef = gameManager.TileDatabase.GetBoosterDefinition(TileLogicType.CrossBomb);
                var squareDef = gameManager.TileDatabase.GetBoosterDefinition(TileLogicType.SquareBomb);
                if (bombDef != null) boosterDefinitions.Add(bombDef);
                if (crossDef != null) boosterDefinitions.Add(crossDef);
                if (squareDef != null) boosterDefinitions.Add(squareDef);
            }

            if (boosterDefinitions.Count == 0 && gameManager.TileDatabase != null && gameManager.TileDatabase.BoosterTileCount > 0)
            {
                var tiles = gameManager.TileDatabase.Tiles;
                for (int i = 0; i < tiles.Count; i++)
                {
                    if (tiles[i] is BoosterTileDefinitionSO booster)
                    {
                        boosterDefinitions.Add(booster);
                        break;
                    }
                }
            }

            // GIAI ĐOẠN 1: Biến đổi Moves thành Special Tiles
            while (remainingMoves > 0 && !_skipSugarCrush)
            {
                List<CellModel> normalCells = new List<CellModel>();
                foreach (CellModel cell in gameManager.Board.GetAllCells())
                {
                    if (cell != null && cell.IsPlayable && cell.Tile != null && cell.Tile.TileKind == TileKind.Normal)
                    {
                        normalCells.Add(cell);
                    }
                }

                if (normalCells.Count == 0)
                {
                    break;
                }

                CellModel chosenCell = normalCells[random.Next(0, normalCells.Count)];
                BoosterTileDefinitionSO chosenBooster = boosterDefinitions.Count > 0 
                    ? boosterDefinitions[random.Next(0, boosterDefinitions.Count)]
                    : null;

                if (chosenBooster != null)
                {
                    TileModel oldTile = chosenCell.Tile;
                    TileModel createdTile = gameManager.Board.CreateTileFromDefinitionId(chosenBooster.TileId, gameManager.TileDatabase);
                    if (createdTile != null)
                    {
                        int previousMoves = gameManager.Board.RemainingMoves;
                        gameManager.Board.SetTile(chosenCell, TileStackLayer.Base, createdTile);
                        gameManager.Board.AddScore(scorePerRemainingMove);
                        gameManager.Board.ConsumeMove();

                        BoardPresentationTraceBuilder traceBuilder = new BoardPresentationTraceBuilder();
                        CascadeTrace spawnCascade = traceBuilder.BeginCascade();
                        BoardFxContext fxContext = new BoardFxContext(spawnCascade, random);
                        fxContext.RecordSpecialCreate(oldTile, createdTile, chosenCell, TileStackLayer.Base, true);

                        BoardMoveExecutionResult transformResult = new BoardMoveExecutionResult
                        {
                            Kind = BoardExecutionKind.ChargedPlacement,
                            IsApplied = true,
                            IsAccepted = true,
                            PresentationTrace = traceBuilder.Build()
                        };

                        if (boardView != null)
                        {
                            // Chạy diễn hoạt sinh Special Tile với tốc độ nhân 4.5 lần để diễn ra nhanh gọn nhưng vẫn lần lượt
                            yield return StartCoroutine(boardView.PlayMoveExecution(transformResult, 4.5f));
                            boardView.SyncToBoardState();

                            // Đợi khi tạo special cell xong thì mới chạy hiệu ứng (delay 0.15s)
                            yield return new WaitForSeconds(0.15f);

                            Vector3 cellWorldPos = boardView.GetCellWorldPosition(chosenCell.X, chosenCell.Y);
                            CreateScoreFloatText(cellWorldPos, scorePerRemainingMove);
                        }

                        EventManager<LogicGameEvent>.Post(
                            LogicGameEvent.GameplayMovesChanged,
                            new RemainingMovesChangedPayload(previousMoves, gameManager.Board.RemainingMoves));

                        GameFlowManager.Instance?.ForcePublishHudState();
                    }
                }

                remainingMoves = gameManager.Board.RemainingMoves;
                yield return new WaitForSeconds(sugarCrushSpawnDelay);
            }

            // Trì hoãn 0.6s sau khi sinh xong special cells rồi mới kích nổ liên hoàn
            if (!_skipSugarCrush)
            {
                yield return new WaitForSeconds(0.6f);
            }

            // GIAI ĐOẠN 2: Kích nổ liên hoàn
            while (!_skipSugarCrush)
            {
                CellModel targetCell = null;
                for (int y = gameManager.Board.Height - 1; y >= 0; y--)
                {
                    for (int x = 0; x < gameManager.Board.Width; x++)
                    {
                        CellModel cell = gameManager.Board.GetCell(x, y);
                        if (cell != null && cell.IsPlayable && cell.Tile != null && cell.Tile.TileKind == TileKind.Booster)
                        {
                            targetCell = cell;
                            break;
                        }
                    }
                    if (targetCell != null) break;
                }

                if (targetCell == null)
                {
                    break;
                }

                BoardMoveExecutionResult result = BoardResolutionService.ExecuteSugarCrushActivation(
                    gameManager.Board, targetCell.X, targetCell.Y, gameManager.LevelData, gameManager.TileDatabase, random, gameManager.RuleSet);

                if (boardView != null)
                {
                    // Rung và lắc camera khi bắt đầu kích nổ tạo cảm giác mạnh mẽ
                    if (Camera.main != null)
                    {
                        Camera.main.transform.DOComplete();
                        Camera.main.transform.DOShakePosition(0.18f, 0.15f, 22, 90f, false, true)
                            .SetLink(Camera.main.gameObject);
                    }

                    if (VibrationManager.Instance != null)
                    {
                        VibrationManager.Instance.PlayMediumImpact(true);
                    }

                    yield return StartCoroutine(boardView.PlayMoveExecution(result));
                    boardView.SyncToBoardState();
                }

                GameFlowManager.Instance?.ForcePublishHudState();
                yield return new WaitForSeconds(sugarCrushExplodeDelay);
            }

            // GIAI ĐOẠN 3: NỔ NHANH LẬP TỨC (FAST RESOLVE) NẾU SKIP
            if (_skipSugarCrush)
            {
                boardView?.ClearPreview();

                remainingMoves = gameManager.Board.RemainingMoves;
                while (remainingMoves > 0)
                {
                    List<CellModel> normalCells = new List<CellModel>();
                    foreach (CellModel cell in gameManager.Board.GetAllCells())
                    {
                        if (cell != null && cell.IsPlayable && cell.Tile != null && cell.Tile.TileKind == TileKind.Normal)
                        {
                            normalCells.Add(cell);
                        }
                    }

                    if (normalCells.Count == 0) break;

                    CellModel chosenCell = normalCells[random.Next(0, normalCells.Count)];
                    BoosterTileDefinitionSO chosenBooster = boosterDefinitions.Count > 0 
                        ? boosterDefinitions[random.Next(0, boosterDefinitions.Count)]
                        : null;

                    if (chosenBooster != null)
                    {
                        TileModel createdTile = gameManager.Board.CreateTileFromDefinitionId(chosenBooster.TileId, gameManager.TileDatabase);
                        if (createdTile != null)
                        {
                            gameManager.Board.SetTile(chosenCell, TileStackLayer.Base, createdTile);
                            gameManager.Board.AddScore(scorePerRemainingMove);
                            gameManager.Board.ConsumeMove();
                        }
                    }
                    remainingMoves = gameManager.Board.RemainingMoves;
                }

                while (true)
                {
                    CellModel targetCell = null;
                    for (int y = gameManager.Board.Height - 1; y >= 0; y--)
                    {
                        for (int x = 0; x < gameManager.Board.Width; x++)
                        {
                            CellModel cell = gameManager.Board.GetCell(x, y);
                            if (cell != null && cell.IsPlayable && cell.Tile != null && cell.Tile.TileKind == TileKind.Booster)
                            {
                                targetCell = cell;
                                break;
                            }
                        }
                        if (targetCell != null) break;
                    }

                    if (targetCell == null) break;

                    BoardResolutionService.ExecuteSugarCrushActivation(
                        gameManager.Board, targetCell.X, targetCell.Y, gameManager.LevelData, gameManager.TileDatabase, random, gameManager.RuleSet);
                }

                boardView?.SyncToBoardState();
                GameFlowManager.Instance?.ForcePublishHudState();
            }

            IsVictoryOutroPlaying = false;
            _isAnimatingMove = false;
            GameFlowManager.Instance?.ForcePublishHudState();
        }

        private void ApplyInputState(InGameSubState subState)
        {
            if (_isAnimatingIntro)
            {
                inputController?.SetInputMode(Match3InputController.BoardInputMode.Disabled);
                boardView?.SetIdleEnabled(false);
                boardView?.ClearPreview();
                return;
            }

            Match3InputController.BoardInputMode inputMode = Match3InputController.BoardInputMode.Disabled;
            bool idleEnabled = false;

            switch (subState)
            {
                case InGameSubState.PlayerTurn:
                    inputMode = Match3InputController.BoardInputMode.Normal;
                    idleEnabled = true;
                    _isTrackingIdle = true;
                    _idleTimer = 0f;
                    _hintShown = false;
                    break;
                case InGameSubState.TargetingChargedPlacement:
                case InGameSubState.TargetingChargedCombo:
                    inputMode = Match3InputController.BoardInputMode.TapOnly;
                    idleEnabled = true;
                    _isTrackingIdle = false;
                    _idleTimer = 0f;
                    _hintShown = false;
                    break;
                default:
                    _isTrackingIdle = false;
                    _idleTimer = 0f;
                    _hintShown = false;
                    break;
            }

            inputController?.SetInputMode(inputMode);
            boardView?.SetIdleEnabled(idleEnabled);
            boardView?.ClearPreview();

            if (subState == InGameSubState.TargetingChargedPlacement)
            {
                boardView?.ShowPlacementCandidates(GameFlowManager.Instance != null
                    ? GameFlowManager.Instance.GetChargedPlacementCandidates()
                    : null);
            }
            else if (subState == InGameSubState.TargetingChargedCombo)
            {
                boardView?.ShowChargedComboSelection(
                    GameFlowManager.Instance != null ? GameFlowManager.Instance.GetChargedComboSourceCell() : null,
                    GameFlowManager.Instance != null ? GameFlowManager.Instance.GetChargedComboPartnerCells() : null);
            }
        }

        private void CreateScoreFloatText(Vector3 startWorldPos, int scoreAmount)
        {
            Canvas mainCanvas = FindFirstObjectByType<Canvas>();
            if (mainCanvas == null) return;

            FloatingScoreText targetPrefab = null;
            var gameplayScreen = GameplayScreen.Instance;
            if (gameplayScreen != null)
            {
                targetPrefab = gameplayScreen.FloatingScoreTextPrefab;
            }

            if (targetPrefab != null)
            {
                FloatingScoreText scoreTextInstance = Instantiate(targetPrefab, mainCanvas.transform, false);
                if (scoreTextInstance != null)
                {
                    Vector3 targetPos = startWorldPos + new Vector3(0f, 4f, 0f);
                    bool isTargetUI = false;

                    var progressView = TopHUDPresenter.Instance != null ? TopHUDPresenter.Instance.LevelProgressView : null;
                    if (progressView != null)
                    {
                        targetPos = (progressView.ProgressSlider != null) 
                            ? progressView.ProgressSlider.transform.position 
                            : progressView.transform.position;
                        isTargetUI = true;
                    }

                    scoreTextInstance.Initialize(startWorldPos, targetPos, scoreAmount, isTargetUI, () => {
                        if (progressView != null)
                        {
                            progressView.AddScoreVisually(scoreAmount);
                            progressView.PlayBounceFx();
                        }
                    });
                    return;
                }
            }

            // Fallback to manual creation if prefab is not assigned
            GameObject textObj = new GameObject("SugarCrush_ScoreFloatText", typeof(RectTransform));
            textObj.transform.SetParent(mainCanvas.transform, false);

            var textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = $"+{scoreAmount}";
            textComponent.fontSize = 54;
            textComponent.fontStyle = FontStyles.Bold;
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.color = new Color(1.0f, 0.78f, 0.12f, 1.0f);

            RectTransform rect = textObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(300, 100);

            Vector2 screenPos = Camera.main != null 
                ? (Vector2)Camera.main.WorldToScreenPoint(startWorldPos)
                : (Vector2)startWorldPos;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                mainCanvas.transform as RectTransform, 
                screenPos, 
                mainCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : Camera.main, 
                out Vector2 localPos);
            rect.anchoredPosition = localPos;

            Vector2 targetScreenPos = screenPos + new Vector2(0f, 500f);
            var progressViewForFallback = TopHUDPresenter.Instance != null ? TopHUDPresenter.Instance.LevelProgressView : null;
            if (progressViewForFallback != null)
            {
                Vector3 fallbackTargetWorldPos = (progressViewForFallback.ProgressSlider != null)
                    ? progressViewForFallback.ProgressSlider.transform.position
                    : progressViewForFallback.transform.position;

                targetScreenPos = RectTransformUtility.WorldToScreenPoint(
                    mainCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : mainCanvas.worldCamera,
                    fallbackTargetWorldPos);
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                mainCanvas.transform as RectTransform, 
                targetScreenPos, 
                mainCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : Camera.main, 
                out Vector2 targetLocalPos);

            textObj.transform.localScale = Vector3.zero;
            
            Sequence seq = DOTween.Sequence();
            seq.Append(textObj.transform.DOScale(1.2f, 0.25f).SetEase(Ease.OutBack))
               .AppendInterval(0.05f)
               .Append(rect.DOAnchorPos(targetLocalPos, 0.75f).SetEase(Ease.InQuad))
               .Join(textObj.transform.DOScale(0.5f, 0.75f).SetEase(Ease.InQuad))
               .Join(textComponent.DOFade(0.1f, 0.75f).SetEase(Ease.InQuad))
               .OnComplete(() => {
                   if (progressViewForFallback != null)
                   {
                       progressViewForFallback.AddScoreVisually(scoreAmount);
                       progressViewForFallback.PlayBounceFx();
                   }
                   Destroy(textObj);
               })
               .SetLink(textObj);
        }
    }
}
