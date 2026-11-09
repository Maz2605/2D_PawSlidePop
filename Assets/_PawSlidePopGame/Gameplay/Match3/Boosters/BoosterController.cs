using System;
using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Data.Events;
using _PawSlidePopGame._Scripts.Data.Events.Payloads;
using _PawSlidePopGame._Scripts.Feature.Match3.Flow;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Move;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;
using _PawSlidePopGame._Scripts.Feature.Match3.Presentation;
using _PawSlidePopGame._Scripts.Gameplay.Meta.EconomyManager;
using _PawSlidePopGame._Scripts.Gameplay.Meta.Inventory;
using _PawSlidePopGame._Scripts.Services.Analytics;
using _PawSlidePopGame.Scripts.DesignPattern.ObserverPattern;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Boosters
{
    [DisallowMultipleComponent]
    public sealed class BoosterController : MonoBehaviour
    {
        public static BoosterController Instance { get; private set; }

        [SerializeField] private List<BoosterDefinitionSO> boosterDefinitions = new List<BoosterDefinitionSO>();

        [SerializeField] private bool logDebugMessages = true;

        private BoosterDefinitionSO _activeBooster;
        private BoosterPaymentSource _activePaymentSource;

        public BoosterDefinitionSO ActiveBooster => _activeBooster;
        public bool HasActiveTargetingBooster => _activeBooster != null && _activeBooster.TargetingMode != BoosterTargetingMode.Immediate;

        public event Action<Func<BoardMoveExecutionResult>> OnExecutionRequested;
        public event Action<BoosterDefinitionSO> OnActiveBoosterChanged;
        public event Action<BoosterDefinitionSO> OnBoosterUseRejected;

        private void Awake()
        {
            Instance = this;
            BoosterInventory.Instance.RegisterDefinitions(boosterDefinitions);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }



        public bool SelectBooster(BoosterType boosterType)
        {
            return TrySelectBooster(boosterType);
        }

        public bool TrySelectBooster(BoosterType boosterType)
        {
            if (_activeBooster != null && _activeBooster.BoosterType == boosterType)
            {
                LogDebug($"Cancelled selection for {_activeBooster.BoosterType} because it was clicked again.");
                CancelSelection();
                return true;
            }

            BoosterDefinitionSO definition = GetDefinition(boosterType);
            if (definition == null)
            {
                Debug.LogWarning($"[BoosterController] Missing booster definition for {boosterType}.", this);
                return false;
            }

            if (!CanSelectBooster(definition))
            {
                LogDebug($"Rejected {definition.BoosterType}. Count={BoosterInventory.Instance.GetCount(definition)}, Coins={EconomyManager.Instance.Coins}, Price={definition.CoinPrice}.");
                EventManager<FeedbackEvent>.Post(FeedbackEvent.BoosterReject);
                OnBoosterUseRejected?.Invoke(definition);
                return false;
            }

            if (definition.TargetingMode == BoosterTargetingMode.Immediate)
            {
                CancelSelection();
                RequestImmediateExecution(definition);
                return true;
            }

            _activeBooster = definition;
            _activePaymentSource = ResolvePaymentSource(definition);
            LogDebug($"Selected {definition.BoosterType}. PaymentSource={_activePaymentSource}.");
            EventManager<FeedbackEvent>.Post(FeedbackEvent.BoosterSelect);
            OnActiveBoosterChanged?.Invoke(_activeBooster);
            return true;
        }

        public bool CanSelectBooster(BoosterDefinitionSO definition)
        {
            if (definition == null)
            {
                return false;
            }

            if (definition.IsUnlimitedForDev)
            {
                return true;
            }

            if (BoosterInventory.Instance.GetCount(definition) > 0)
            {
                return true;
            }

            return EconomyManager.Instance.CanSpendCoins(definition.CoinPrice);
        }

        public void CancelSelection()
        {
            if (_activeBooster == null)
            {
                return;
            }

            _activeBooster = null;
            _activePaymentSource = BoosterPaymentSource.None;
            OnActiveBoosterChanged?.Invoke(null);
        }

        public bool TryHandleTileTap(CellModel cell)
        {
            if (_activeBooster == null || _activeBooster.TargetingMode != BoosterTargetingMode.TapCell)
            {
                return false;
            }
            if (cell == null || GameFlowManager.Instance == null || !GameFlowManager.Instance.CanUseBoosterAt(_activeBooster, cell.X, cell.Y))
            {
                LogDebug($"Tap booster rejected. Booster={_activeBooster.BoosterType}, Cell={cell?.X},{cell?.Y}, State={GameFlowManager.Instance?.CurrentInGameSubState}.");
                EventManager<FeedbackEvent>.Post(FeedbackEvent.BoosterReject);
                return true;
            }

            BoosterDefinitionSO selectedBooster = _activeBooster;
            BoosterPaymentSource paymentSource = _activePaymentSource;
            int x = cell.X;
            int y = cell.Y;
            CancelSelection();
            OnExecutionRequested?.Invoke(() => GameFlowManager.Instance != null
                ? ExecuteAndConsume(selectedBooster, paymentSource, () => GameFlowManager.Instance.RequestBoosterAt(selectedBooster, x, y))
                : new BoardMoveExecutionResult());
            return true;
        }

        public bool TryHandleLineSwipe(BoardMoveRequest request)
        {
            if (_activeBooster == null || _activeBooster.TargetingMode != BoosterTargetingMode.SwipeLine)
            {
                return false;
            }

            if (GameFlowManager.Instance == null || !GameFlowManager.Instance.CanUseBoosterLine(_activeBooster, request.Axis, request.LineIndex))
            {
                LogDebug($"Line booster rejected. Booster={_activeBooster.BoosterType}, Axis={request.Axis}, Line={request.LineIndex}, State={GameFlowManager.Instance?.CurrentInGameSubState}, CanCommands={GameFlowManager.Instance?.CanAcceptGameplayCommands}.");
                EventManager<FeedbackEvent>.Post(FeedbackEvent.BoosterReject);
                return true;
            }

            BoosterDefinitionSO selectedBooster = _activeBooster;
            BoosterPaymentSource paymentSource = _activePaymentSource;
            CancelSelection();
            OnExecutionRequested?.Invoke(() => GameFlowManager.Instance != null
                ? ExecuteAndConsume(selectedBooster, paymentSource, () => GameFlowManager.Instance.RequestBoosterLine(selectedBooster, request.Axis, request.LineIndex))
                : new BoardMoveExecutionResult());
            return true;
        }

        private void RequestImmediateExecution(BoosterDefinitionSO definition)
        {
            if (definition == null || GameFlowManager.Instance == null || !CanSelectBooster(definition) || !GameFlowManager.Instance.CanUseImmediateBooster(definition))
            {
                LogDebug($"Immediate booster rejected. Booster={definition?.BoosterType}, State={GameFlowManager.Instance?.CurrentInGameSubState}, CanCommands={GameFlowManager.Instance?.CanAcceptGameplayCommands}.");
                EventManager<FeedbackEvent>.Post(FeedbackEvent.BoosterReject);
                OnBoosterUseRejected?.Invoke(definition);
                return;
            }

            OnExecutionRequested?.Invoke(() => GameFlowManager.Instance != null
                ? ExecuteAndConsume(definition, ResolvePaymentSource(definition), () => GameFlowManager.Instance.RequestImmediateBooster(definition))
                : new BoardMoveExecutionResult());
        }

        private static BoardMoveExecutionResult ExecuteAndConsume(BoosterDefinitionSO definition, BoosterPaymentSource paymentSource, Func<BoardMoveExecutionResult> execute)
        {
            BoardMoveExecutionResult result = execute != null ? execute.Invoke() : new BoardMoveExecutionResult();
            if (result.IsAccepted)
            {
                result.BoosterDefinition = definition;
                if (definition != null)
                {
                    result.BoosterType = definition.BoosterType;
                }

                EventManager<FeedbackEvent>.Post(
                    FeedbackEvent.BoosterUse,
                    new BoosterFeedbackPayload(definition != null ? definition.BoosterType : BoosterType.None));
                if (paymentSource == BoosterPaymentSource.Inventory)
                {
                    BoosterInventory.Instance.TryConsumeBooster(definition, "booster_used");
                }
                else if (paymentSource == BoosterPaymentSource.Coins)
                {
                    EconomyManager.Instance.TrySpendCoins(definition.CoinPrice, "booster_used");
                }

                if (definition != null)
                {
                    string currentLevelId = _PawSlidePopGame._Scripts.Core.System.GameFlow.GameAppFlowManager.Instance != null
                        ? _PawSlidePopGame._Scripts.Core.System.GameFlow.GameAppFlowManager.Instance.CurrentLevelId
                        : string.Empty;
                    FirebaseService.LogBoosterUsed(definition.BoosterId, "gameplay", currentLevelId);
                }

                UnityEngine.Debug.Log($"[BoosterController] Used {definition.BoosterType}. PaymentSource={paymentSource}, Coins={EconomyManager.Instance.Coins}, Count={BoosterInventory.Instance.GetCount(definition)}.");
            }

            return result;
        }

        private BoosterDefinitionSO GetDefinition(BoosterType boosterType)
        {
            for (int i = 0; i < boosterDefinitions.Count; i++)
            {
                BoosterDefinitionSO definition = boosterDefinitions[i];
                if (definition != null && definition.BoosterType == boosterType)
                {
                    return definition;
                }
            }

            return null;
        }



        private static BoosterPaymentSource ResolvePaymentSource(BoosterDefinitionSO definition)
        {
            if (definition == null)
            {
                return BoosterPaymentSource.None;
            }

            if (definition.IsUnlimitedForDev)
            {
                return BoosterPaymentSource.Free;
            }

            return BoosterInventory.Instance.GetCount(definition) > 0
                ? BoosterPaymentSource.Inventory
                : BoosterPaymentSource.Coins;
        }

        private void LogDebug(string message)
        {
            if (logDebugMessages)
            {
                Debug.Log($"[BoosterController] {message}", this);
            }
        }

        private enum BoosterPaymentSource
        {
            None = 0,
            Free = 1,
            Inventory = 2,
            Coins = 3
        }
    }
}
