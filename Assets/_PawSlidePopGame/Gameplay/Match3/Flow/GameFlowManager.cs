using System;
using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Core.System.DesignPattern.Singleton;
using _PawSlidePopGame._Scripts.Core.System.GameFlow;
using _PawSlidePopGame._Scripts.Data.Events;
using _PawSlidePopGame._Scripts.Data.Events.Payloads;
using _PawSlidePopGame._Scripts.Feature.Match3.Boosters;
using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Move;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Resolution;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;
using _PawSlidePopGame._Scripts.Feature.Match3.Presentation;
using _PawSlidePopGame._Scripts.Feature.Match3.Presenter;
using _PawSlidePopGame._Scripts.Gameplay.Meta.EconomyManager;
using _PawSlidePopGame.Scripts.DesignPattern.ObserverPattern;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Flow
{
    [DisallowMultipleComponent]
    public class GameFlowManager : Singleton<GameFlowManager>
    {
        [SerializeField] private Match3GameManager gameManager;
        [SerializeField] private bool autoStartFlow = true;

        private Match3ObjectiveTracker _objectiveTracker;
        private ChargedAbilityTracker _chargedAbilityTracker;
        private GameplayHudSnapshot _lastSnapshot;
        private InGameSubState _resumeSubState = InGameSubState.PlayerTurn;
        private BoardCellPosition? _chargedComboSource;

        public GameState CurrentGameState { get; private set; } = GameState.None;
        public InGameSubState CurrentInGameSubState { get; private set; } = InGameSubState.None;
        public GameplayHudSnapshot LastSnapshot => _lastSnapshot;
        public event Action<BoardMoveExecutionResult> OnAutoShuffleTriggered;

        public bool CanAcceptGameplayCommands =>
            CurrentGameState == GameState.Gameplay &&
            CurrentInGameSubState == InGameSubState.PlayerTurn &&
            gameManager != null &&
            gameManager.IsInitialized;
        public bool CanPauseGameplay =>
            CurrentGameState == GameState.Gameplay &&
            IsInteractiveGameplaySubState(CurrentInGameSubState);

        public bool IsInChargedPlacementMode => CurrentInGameSubState == InGameSubState.TargetingChargedPlacement;
        public bool IsInChargedComboMode => CurrentInGameSubState == InGameSubState.TargetingChargedCombo;

        public static bool IsInteractiveGameplaySubState(InGameSubState subState)
        {
            return subState == InGameSubState.PlayerTurn ||
                   subState == InGameSubState.TargetingChargedPlacement ||
                   subState == InGameSubState.TargetingChargedCombo;
        }

        protected override void Awake()
        {
            DontDestroyOnLoadEnabled = false;
            base.Awake();

            if (gameManager == null)
            {
                gameManager = GetComponent<Match3GameManager>();
            }
        }

        public void EnterGameplay()
        {
            if (gameManager == null)
            {
                Debug.LogError("[GameFlowManager] Missing Match3GameManager reference.", this);
                return;
            }

            if (!gameObject.activeInHierarchy)
            {
                Debug.LogWarning("[GameFlowManager] EnterGameplay ignored because gameplay session is inactive.", this);
                return;
            }

            SetGameState(GameState.Gameplay);
            SetInGameSubState(InGameSubState.Bootstrapping);
            BeginBoardPreparation();
        }

        public void ExitGameplay()
        {
            _chargedComboSource = null;
            _objectiveTracker = null;
            _chargedAbilityTracker = null;
            _lastSnapshot = null;
            _resumeSubState = InGameSubState.PlayerTurn;
            CurrentGameState = GameState.None;
            CurrentInGameSubState = InGameSubState.None;
        }

        public void BeginBoardPreparation()
        {
            if (gameManager == null)
            {
                return;
            }

            SetInGameSubState(InGameSubState.PreparingBoard);
            gameManager.InitializeGame();
            _objectiveTracker = new Match3ObjectiveTracker(gameManager.LevelData, gameManager.TileDatabase);
            _chargedAbilityTracker = null;
            _chargedComboSource = null;
            PublishHudInitialized();
            EnterPlayerTurn();
        }

        public BoardMoveExecutionResult RequestMove(BoardMoveRequest request)
        {
            if (!CanAcceptGameplayCommands)
            {
                return new BoardMoveExecutionResult();
            }

            SetInGameSubState(InGameSubState.ResolvingBoard);
            int previousMoves = _lastSnapshot != null ? _lastSnapshot.remainingMoves : gameManager.Board.RemainingMoves;
            BoardMoveExecutionResult executionResult = gameManager.ExecuteMove(request);
            if (executionResult.IsAccepted)
            {
                PublishMovesChanged(previousMoves, gameManager.Board.RemainingMoves);
            }

            return executionResult;
        }

        public BoardMoveExecutionResult RequestTileActivation(int x, int y)
        {
            if (!CanAcceptGameplayCommands)
            {
                return new BoardMoveExecutionResult();
            }

            SetInGameSubState(InGameSubState.ResolvingBoard);
            int previousMoves = _lastSnapshot != null ? _lastSnapshot.remainingMoves : gameManager.Board.RemainingMoves;
            BoardMoveExecutionResult executionResult = gameManager.ExecuteTileActivation(x, y);
            if (executionResult.IsAccepted)
            {
                PublishMovesChanged(previousMoves, gameManager.Board.RemainingMoves);
            }

            return executionResult;
        }

        public bool CanUseBoosterAt(BoosterDefinitionSO boosterDefinition, int x, int y)
        {
            return CanAcceptGameplayCommands &&
                   BoosterResolutionService.CanUseAt(boosterDefinition, gameManager.Board, x, y, gameManager.TileDatabase);
        }

        public bool CanUseBoosterLine(BoosterDefinitionSO boosterDefinition, MoveAxis axis, int lineIndex)
        {
            return CanAcceptGameplayCommands &&
                   BoosterResolutionService.CanUseLine(boosterDefinition, gameManager.Board, axis, lineIndex);
        }

        public bool CanUseImmediateBooster(BoosterDefinitionSO boosterDefinition)
        {
            return CanAcceptGameplayCommands &&
                   BoosterResolutionService.CanUseImmediate(boosterDefinition, gameManager.Board);
        }

        public BoardMoveExecutionResult RequestBoosterAt(BoosterDefinitionSO boosterDefinition, int x, int y)
        {
            BoardMoveExecutionResult emptyResult = new BoardMoveExecutionResult
            {
                Kind = BoardExecutionKind.Booster
            };

            if (!CanUseBoosterAt(boosterDefinition, x, y))
            {
                return emptyResult;
            }

            SetInGameSubState(InGameSubState.ResolvingBoard);
            return BoosterResolutionService.ExecuteAt(
                boosterDefinition,
                gameManager.Board,
                x,
                y,
                gameManager.LevelData,
                gameManager.TileDatabase,
                gameManager.Random);
        }

        public BoardMoveExecutionResult RequestBoosterLine(BoosterDefinitionSO boosterDefinition, MoveAxis axis, int lineIndex)
        {
            BoardMoveExecutionResult emptyResult = new BoardMoveExecutionResult
            {
                Kind = BoardExecutionKind.Booster
            };

            if (!CanUseBoosterLine(boosterDefinition, axis, lineIndex))
            {
                return emptyResult;
            }

            SetInGameSubState(InGameSubState.ResolvingBoard);
            return BoosterResolutionService.ExecuteLine(
                boosterDefinition,
                gameManager.Board,
                axis,
                lineIndex,
                gameManager.LevelData,
                gameManager.TileDatabase,
                gameManager.Random);
        }

        public BoardMoveExecutionResult RequestImmediateBooster(BoosterDefinitionSO boosterDefinition)
        {
            BoardMoveExecutionResult emptyResult = new BoardMoveExecutionResult
            {
                Kind = BoardExecutionKind.Booster
            };

            if (!CanUseImmediateBooster(boosterDefinition))
            {
                return emptyResult;
            }

            SetInGameSubState(InGameSubState.ResolvingBoard);
            return BoosterResolutionService.ExecuteImmediate(
                boosterDefinition,
                gameManager.Board,
                gameManager.LevelData,
                gameManager.TileDatabase,
                gameManager.Random);
        }

        public bool EnterChargedPlacementMode()
        {
            if (CurrentGameState != GameState.Gameplay ||
                CurrentInGameSubState != InGameSubState.PlayerTurn ||
                _chargedAbilityTracker == null ||
                !_chargedAbilityTracker.CanPlace ||
                gameManager?.Board == null)
            {
                return false;
            }

            _chargedComboSource = null;
            SetInGameSubState(InGameSubState.TargetingChargedPlacement);
            return true;
        }

        public bool TryEnterChargedComboMode(int x, int y)
        {
            if (CurrentGameState != GameState.Gameplay ||
                CurrentInGameSubState != InGameSubState.PlayerTurn ||
                gameManager?.Board == null)
            {
                return false;
            }

            CellModel sourceCell = gameManager.Board.GetCell(x, y);
            if (!IsChargedBoosterCell(sourceCell))
            {
                return false;
            }

            List<CellModel> partners = GetChargedComboPartnerCells(sourceCell);
            if (partners.Count == 0)
            {
                return false;
            }

            _chargedComboSource = new BoardCellPosition(x, y);
            SetInGameSubState(InGameSubState.TargetingChargedCombo);
            return true;
        }

        public bool CancelChargedAbilityMode()
        {
            if (CurrentInGameSubState != InGameSubState.TargetingChargedPlacement &&
                CurrentInGameSubState != InGameSubState.TargetingChargedCombo)
            {
                return false;
            }

            _chargedComboSource = null;
            EnterPlayerTurn();
            return true;
        }

        public BoardMoveExecutionResult RequestChargedPlacement(int x, int y)
        {
            BoardMoveExecutionResult emptyResult = new BoardMoveExecutionResult
            {
                Kind = BoardExecutionKind.ChargedPlacement
            };

            if (CurrentGameState != GameState.Gameplay ||
                CurrentInGameSubState != InGameSubState.TargetingChargedPlacement ||
                _chargedAbilityTracker == null ||
                !_chargedAbilityTracker.CanPlace ||
                gameManager?.Board == null)
            {
                return emptyResult;
            }

            if (!BoardResolutionService.CanPlaceChargedBoosterAt(
                    gameManager.Board,
                    x,
                    y,
                    _chargedAbilityTracker.SpecialTileId,
                    gameManager.TileDatabase))
            {
                return emptyResult;
            }

            BoardMoveExecutionResult executionResult = BoardResolutionService.ExecuteChargedPlacement(
                gameManager.Board,
                x,
                y,
                _chargedAbilityTracker.SpecialTileId,
                gameManager.TileDatabase,
                gameManager.Random);
            if (!executionResult.IsAccepted)
            {
                return executionResult;
            }

            _chargedComboSource = null;
            _chargedAbilityTracker.TryConsumeCharge();
            SetInGameSubState(InGameSubState.ResolvingBoard);
            return executionResult;
        }

        public BoardMoveExecutionResult RequestChargedComboActivation(int x, int y)
        {
            BoardMoveExecutionResult emptyResult = new BoardMoveExecutionResult
            {
                Kind = BoardExecutionKind.ChargedCombo
            };

            if (CurrentGameState != GameState.Gameplay ||
                CurrentInGameSubState != InGameSubState.TargetingChargedCombo ||
                !_chargedComboSource.HasValue ||
                gameManager?.Board == null)
            {
                return emptyResult;
            }

            CellModel sourceCell = gameManager.Board.GetCell(_chargedComboSource.Value.X, _chargedComboSource.Value.Y);
            CellModel partnerCell = gameManager.Board.GetCell(x, y);
            if (!IsChargedComboPartnerCell(sourceCell, partnerCell))
            {
                return emptyResult;
            }

            int previousMoves = _lastSnapshot != null ? _lastSnapshot.remainingMoves : gameManager.Board.RemainingMoves;
            SetInGameSubState(InGameSubState.ResolvingBoard);
            BoardMoveExecutionResult executionResult = BoardResolutionService.ExecuteChargedCombo(
                gameManager.Board,
                _chargedComboSource.Value,
                new BoardCellPosition(x, y),
                gameManager.LevelData,
                gameManager.TileDatabase,
                gameManager.Random);
            if (executionResult.IsAccepted)
            {
                PublishMovesChanged(previousMoves, gameManager.Board.RemainingMoves);
            }

            return executionResult;
        }

        public List<CellModel> GetChargedPlacementCandidates()
        {
            List<CellModel> candidates = new List<CellModel>();
            if (gameManager?.Board == null || _chargedAbilityTracker == null || !_chargedAbilityTracker.CanPlace)
            {
                return candidates;
            }

            foreach (CellModel cell in gameManager.Board.GetAllCells())
            {
                if (BoardResolutionService.CanPlaceChargedBoosterAt(
                        gameManager.Board,
                        cell.X,
                        cell.Y,
                        _chargedAbilityTracker.SpecialTileId,
                        gameManager.TileDatabase))
                {
                    candidates.Add(cell);
                }
            }

            return candidates;
        }

        public bool CanPlaceChargedBoosterAt(int x, int y)
        {
            return gameManager?.Board != null &&
                   _chargedAbilityTracker != null &&
                   _chargedAbilityTracker.CanPlace &&
                   BoardResolutionService.CanPlaceChargedBoosterAt(
                       gameManager.Board,
                       x,
                       y,
                       _chargedAbilityTracker.SpecialTileId,
                       gameManager.TileDatabase);
        }

        public CellModel GetChargedComboSourceCell()
        {
            if (!_chargedComboSource.HasValue || gameManager?.Board == null)
            {
                return null;
            }

            return gameManager.Board.GetCell(_chargedComboSource.Value.X, _chargedComboSource.Value.Y);
        }

        public List<CellModel> GetChargedComboPartnerCells()
        {
            return GetChargedComboPartnerCells(GetChargedComboSourceCell());
        }

        public bool CanConfirmChargedComboAt(int x, int y)
        {
            return IsChargedComboPartnerCell(GetChargedComboSourceCell(), gameManager?.Board?.GetCell(x, y));
        }

        public void NotifyTileClearedDuringPlayback(TileClearOp clearOp, Vector3 worldPosition)
        {
            if (CurrentInGameSubState != InGameSubState.ResolvingBoard || clearOp == null || _objectiveTracker == null || _lastSnapshot == null)
            {
                return;
            }

            if (!_objectiveTracker.TryApplyClearOp(clearOp, out TargetProgressChangedPayload targetChange))
            {
                return;
            }

            UpdateSnapshotTargetProgress(targetChange);
            EventManager<VisualGameEvent>.Post(VisualGameEvent.TopHudTargetProgressFx, targetChange);
            EventManager<VisualGameEvent>.Post(
                VisualGameEvent.TopHudTargetCollectedFx,
                new TargetCollectedFxPayload(
                    clearOp.TileId,
                    clearOp.TileInstanceId,
                    clearOp.Cell,
                    worldPosition,
                    targetChange.PreviousCount,
                    targetChange.CurrentCount,
                    targetChange.RequiredCount,
                    targetChange.JustCompleted));

            if (_objectiveTracker.TryConsumeTargetsCompletedFx())
            {
                EventManager<VisualGameEvent>.Post(VisualGameEvent.TopHudTargetsCompletedFx);
            }
        }

        public void NotifyScoreGainedDuringPlayback(ScoreGainOp scoreGainOp)
        {
            if (CurrentInGameSubState != InGameSubState.ResolvingBoard || scoreGainOp == null || _lastSnapshot == null)
            {
                return;
            }

            int previousScore = _lastSnapshot.currentScore;
            if (scoreGainOp.RunningScore <= previousScore)
            {
                return;
            }

            _lastSnapshot.currentScore = scoreGainOp.RunningScore;
            EventManager<LogicGameEvent>.Post(
                LogicGameEvent.GameplayScoreChanged,
                new ScoreChangedPayload(previousScore, _lastSnapshot.currentScore, scoreGainOp.Amount));

            int previousStars = _lastSnapshot.reachedStars;
            int currentStars = GameplayHudSnapshotBuilder.CountReachedStars(_lastSnapshot.currentScore, _lastSnapshot.starScoreThresholds);
            _lastSnapshot.reachedStars = currentStars;

            for (int starIndex = previousStars + 1; starIndex <= currentStars; starIndex++)
            {
                EventManager<VisualGameEvent>.Post(
                    VisualGameEvent.TopHudStarReachedFx,
                    new StarReachedPayload(starIndex, _lastSnapshot.currentScore));
            }
        }

        public void NotifyResolutionPlaybackComplete(BoardMoveExecutionResult executionResult)
        {
            if (CurrentInGameSubState != InGameSubState.ResolvingBoard)
            {
                return;
            }

            _chargedComboSource = null;
            if (_chargedAbilityTracker != null &&
                executionResult != null &&
                executionResult.IsAccepted &&
                executionResult.Kind != BoardExecutionKind.ChargedPlacement)
            {
                _chargedAbilityTracker.ApplyAcceptedTurn(executionResult);
            }

            ReconcileRuntimeSnapshot();
            SetInGameSubState(InGameSubState.CheckingResult);
            EvaluateBoardResult();
        }

        public void PauseGameplay()
        {
            if (!CanPauseGameplay || CurrentInGameSubState == InGameSubState.Paused)
            {
                return;
            }

            _resumeSubState = IsInteractiveGameplaySubState(CurrentInGameSubState)
                ? CurrentInGameSubState
                : InGameSubState.PlayerTurn;

            SetInGameSubState(InGameSubState.Paused);
            EventManager<VisualGameEvent>.Post(VisualGameEvent.GameplayPausedFx);
        }

        public void ResumeGameplay()
        {
            if (CurrentInGameSubState != InGameSubState.Paused)
            {
                return;
            }

            InGameSubState targetState = _resumeSubState == InGameSubState.None || _resumeSubState == InGameSubState.Paused
                ? InGameSubState.PlayerTurn
                : _resumeSubState;

            SetInGameSubState(targetState);
            EventManager<VisualGameEvent>.Post(VisualGameEvent.GameplayResumedFx);
        }

        public void EnterVictory()
        {
            SetGameState(GameState.Result);
            SetInGameSubState(InGameSubState.Victory);
            HeartManager.Instance.SetMatchFinished(true);
            EventManager<LogicGameEvent>.Post(LogicGameEvent.GameplayWon, _lastSnapshot?.Clone());
        }

        public void EnterDefeat()
        {
            SetGameState(GameState.Result);
            SetInGameSubState(InGameSubState.Defeat);
            HeartManager.Instance.SetMatchFinished(false);
            EventManager<LogicGameEvent>.Post(LogicGameEvent.GameplayLost, _lastSnapshot?.Clone());
        }

        private void EnterPlayerTurn()
        {
            _chargedComboSource = null;
            SetInGameSubState(InGameSubState.PlayerTurn);
        }

        private void EvaluateBoardResult()
        {
            if (_objectiveTracker != null && _objectiveTracker.AreAllTargetsComplete)
            {
                EnterVictory();
                return;
            }

            if (gameManager?.Board != null && gameManager.Board.RemainingMoves <= 0)
            {
                EnterDefeat();
                return;
            }

            if (gameManager?.Board != null &&
                gameManager.RuleSet != null &&
                !BoardResolutionService.HasPossibleMoves(gameManager.Board, gameManager.RuleSet.MatchRule))
            {
                TriggerAutoShuffle();
                return;
            }

            EnterPlayerTurn();
        }

        private void TriggerAutoShuffle()
        {
            SetInGameSubState(InGameSubState.ResolvingBoard);

            BoardMoveExecutionResult executionResult = BoosterResolutionService.ExecuteShuffle(
                gameManager.Board,
                gameManager.LevelData,
                gameManager.TileDatabase,
                gameManager.Random,
                gameManager.RuleSet);

            executionResult.Kind = BoardExecutionKind.Booster;
            executionResult.BoosterType = BoosterType.Shuffle;

            if (!executionResult.IsAccepted)
            {
                Debug.LogWarning("[GameFlowManager] Auto-shuffle failed to produce a valid board. Entering defeat.");
                SetInGameSubState(InGameSubState.CheckingResult);
                EnterDefeat();
                return;
            }

            OnAutoShuffleTriggered?.Invoke(executionResult);
        }

        private void PublishHudInitialized()
        {
            _lastSnapshot = GameplayHudSnapshotBuilder.Build(
                gameManager.LevelData,
                gameManager.Board,
                _objectiveTracker,
                _chargedAbilityTracker,
                IsInChargedPlacementMode,
                IsInChargedComboMode);
            EventManager<LogicGameEvent>.Post(LogicGameEvent.GameplayHudInitialized, _lastSnapshot.Clone());
        }

        private bool SetGameState(GameState nextState)
        {
            if (CurrentGameState == nextState)
            {
                return false;
            }

            if (!IsValidGameStateTransition(CurrentGameState, nextState))
            {
                Debug.LogWarning($"[GameFlowManager] Invalid GameState transition {CurrentGameState} -> {nextState}.", this);
                return false;
            }

            GameState previous = CurrentGameState;
            CurrentGameState = nextState;
            EventManager<LogicGameEvent>.Post(
                LogicGameEvent.GameStateChanged,
                new GameStateChangedPayload(previous, CurrentGameState));
            return true;
        }

        private bool SetInGameSubState(InGameSubState nextState)
        {
            if (CurrentInGameSubState == nextState)
            {
                return false;
            }

            if (!IsValidInGameSubStateTransition(CurrentInGameSubState, nextState))
            {
                Debug.LogWarning($"[GameFlowManager] Invalid InGameSubState transition {CurrentInGameSubState} -> {nextState}.", this);
                return false;
            }

            InGameSubState previous = CurrentInGameSubState;
            CurrentInGameSubState = nextState;
            EventManager<LogicGameEvent>.Post(
                LogicGameEvent.InGameSubStateChanged,
                new InGameSubStateChangedPayload(previous, CurrentInGameSubState));
            PublishHudStateChanged();
            return true;
        }

        private static bool IsValidGameStateTransition(GameState currentState, GameState nextState)
        {
            switch (currentState)
            {
                case GameState.None:
                    return nextState == GameState.Bootstrapping || nextState == GameState.Gameplay || nextState == GameState.LoadingScene;
                case GameState.Bootstrapping:
                    return nextState == GameState.LoadingScene || nextState == GameState.Gameplay;
                case GameState.LoadingScene:
                    return nextState == GameState.Gameplay || nextState == GameState.Result;
                case GameState.Gameplay:
                    return nextState == GameState.Gameplay || nextState == GameState.Result || nextState == GameState.LoadingScene;
                case GameState.Result:
                    return nextState == GameState.LoadingScene || nextState == GameState.Gameplay;
                case GameState.Paused:
                    return nextState == GameState.Gameplay || nextState == GameState.Result;
                default:
                    return false;
            }
        }

        private static bool IsValidInGameSubStateTransition(InGameSubState currentState, InGameSubState nextState)
        {
            switch (currentState)
            {
                case InGameSubState.None:
                    return nextState == InGameSubState.Bootstrapping;
                case InGameSubState.Bootstrapping:
                    return nextState == InGameSubState.PreparingBoard || nextState == InGameSubState.Paused;
                case InGameSubState.PreparingBoard:
                    return nextState == InGameSubState.PlayerTurn || nextState == InGameSubState.Defeat || nextState == InGameSubState.Paused;
                case InGameSubState.PlayerTurn:
                    return nextState == InGameSubState.ResolvingBoard ||
                           nextState == InGameSubState.TargetingChargedPlacement ||
                           nextState == InGameSubState.TargetingChargedCombo ||
                           nextState == InGameSubState.Paused ||
                           nextState == InGameSubState.Victory ||
                           nextState == InGameSubState.Defeat;
                case InGameSubState.TargetingChargedPlacement:
                    return nextState == InGameSubState.PlayerTurn || nextState == InGameSubState.ResolvingBoard || nextState == InGameSubState.Paused;
                case InGameSubState.TargetingChargedCombo:
                    return nextState == InGameSubState.PlayerTurn || nextState == InGameSubState.ResolvingBoard || nextState == InGameSubState.Paused;
                case InGameSubState.ResolvingBoard:
                    return nextState == InGameSubState.CheckingResult || nextState == InGameSubState.Paused;
                case InGameSubState.CheckingResult:
                    return nextState == InGameSubState.PlayerTurn ||
                           nextState == InGameSubState.ResolvingBoard || // Auto-shuffle after no possible moves
                           nextState == InGameSubState.Victory ||
                           nextState == InGameSubState.Defeat ||
                           nextState == InGameSubState.Paused;
                case InGameSubState.Victory:
                case InGameSubState.Defeat:
                    return nextState == InGameSubState.Bootstrapping;
                case InGameSubState.Paused:
                    return nextState != InGameSubState.None;
                default:
                    return false;
            }
        }

        private void PublishMovesChanged(int previousMoves, int currentMoves)
        {
            if (_lastSnapshot == null || previousMoves == currentMoves)
            {
                return;
            }

            _lastSnapshot.remainingMoves = currentMoves;
            EventManager<LogicGameEvent>.Post(
                LogicGameEvent.GameplayMovesChanged,
                new RemainingMovesChangedPayload(previousMoves, currentMoves));
        }

        private void UpdateSnapshotTargetProgress(TargetProgressChangedPayload payload)
        {
            if (_lastSnapshot?.targets == null)
            {
                return;
            }

            for (int i = 0; i < _lastSnapshot.targets.Count; i++)
            {
                TargetProgressData target = _lastSnapshot.targets[i];
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

        private void ReconcileRuntimeSnapshot()
        {
            if (gameManager == null)
            {
                return;
            }

            _lastSnapshot = GameplayHudSnapshotBuilder.Build(
                gameManager.LevelData,
                gameManager.Board,
                _objectiveTracker,
                _chargedAbilityTracker,
                IsInChargedPlacementMode,
                IsInChargedComboMode);
        }

        private void PublishHudStateChanged()
        {
            if (_lastSnapshot == null || gameManager == null)
            {
                return;
            }

            _lastSnapshot = GameplayHudSnapshotBuilder.Build(
                gameManager.LevelData,
                gameManager.Board,
                _objectiveTracker,
                _chargedAbilityTracker,
                IsInChargedPlacementMode,
                IsInChargedComboMode);
            EventManager<LogicGameEvent>.Post(LogicGameEvent.GameplayHudStateChanged, _lastSnapshot.Clone());
        }

        public void ForcePublishHudState()
        {
            PublishHudStateChanged();
        }

        private bool IsChargedBoosterCell(CellModel cell)
        {
            return cell != null &&
                   cell.IsPlayable &&
                   cell.Tile != null &&
                   cell.Tile.LogicType == TileLogicType.ChargedSweepBooster &&
                   cell.CanTileActivate();
        }

        private List<CellModel> GetChargedComboPartnerCells(CellModel sourceCell)
        {
            List<CellModel> partners = new List<CellModel>();
            if (!IsChargedBoosterCell(sourceCell) || gameManager?.Board == null)
            {
                return partners;
            }

            TryAddChargedPartner(sourceCell.X + 1, sourceCell.Y, partners);
            TryAddChargedPartner(sourceCell.X - 1, sourceCell.Y, partners);
            TryAddChargedPartner(sourceCell.X, sourceCell.Y + 1, partners);
            TryAddChargedPartner(sourceCell.X, sourceCell.Y - 1, partners);
            return partners;
        }

        private bool IsChargedComboPartnerCell(CellModel sourceCell, CellModel partnerCell)
        {
            if (!IsChargedBoosterCell(sourceCell) || !IsChargedBoosterCell(partnerCell))
            {
                return false;
            }

            return System.Math.Abs(sourceCell.X - partnerCell.X) + System.Math.Abs(sourceCell.Y - partnerCell.Y) == 1;
        }

        private void TryAddChargedPartner(int x, int y, List<CellModel> partners)
        {
            CellModel partner = gameManager?.Board?.GetCell(x, y);
            if (IsChargedBoosterCell(partner))
            {
                partners.Add(partner);
            }
        }
    }
}
