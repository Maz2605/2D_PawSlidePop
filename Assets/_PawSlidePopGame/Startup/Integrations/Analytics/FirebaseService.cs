using _PawSlidePopGame._Scripts.Core.Boostrap;
using _PawSlidePopGame._Scripts.Core.System.GameFlow;
using _PawSlidePopGame._Scripts.Data.Events;
using _PawSlidePopGame._Scripts.Data.Events.Payloads;
using _PawSlidePopGame.Scripts.DesignPattern.ObserverPattern;
using Firebase;
using Firebase.Analytics;
using Firebase.Crashlytics;
using Firebase.Extensions;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Services.Analytics
{
    [DisallowMultipleComponent]
    public sealed class FirebaseService : MonoBehaviour, IAppService
    {
        private const string EventAppBootstrap = "app_bootstrap";
        private const string EventLevelStart = "level_start";
        private const string EventLevelEnd = "level_end";
        private const string EventRewardGranted = "reward_granted";

        private const string ParamLevelId = "level_id";
        private const string ParamLevelNumber = "level_number";
        private const string ParamResult = "result";
        private const string ParamScore = "score";
        private const string ParamStars = "stars";
        private const string ParamRemainingMoves = "remaining_moves";
        private const string ParamRewardType = "reward_type";
        private const string ParamRewardAmount = "reward_amount";
        private const string ParamRewardSource = "reward_source";

        // Booster Events
        private const string EventBoosterUsed = "booster_used";
        private const string EventBoosterPurchased = "booster_purchased";

        // Booster Parameters
        private const string ParamBoosterId = "booster_id";
        private const string ParamUsagePhase = "use_phase";
        private const string ParamPriceCoins = "price_coins";
        private const string ParamQuantity = "quantity";

        [Header("Firebase")]
        [SerializeField] private bool analyticsEnabled = true;
        [SerializeField] private bool crashlyticsEnabled = true;
        [SerializeField] private bool logLifecycleEvents = true;

        private bool _initStarted;
        private bool _eventsSubscribed;

        public static FirebaseService Instance { get; private set; }
        public static bool IsReady { get; private set; }

        public static event System.Action OnReady;

        public void Init()
        {
            if (_initStarted)
            {
                return;
            }

            _initStarted = true;
            Instance = this;
            CheckDependenciesAndInitialize();
        }

        public static void LogEvent(string eventName)
        {
            if (!CanLog(eventName))
            {
                return;
            }

            FirebaseAnalytics.LogEvent(eventName);
        }

        public static void LogEvent(string eventName, params Parameter[] parameters)
        {
            if (!CanLog(eventName))
            {
                return;
            }

            FirebaseAnalytics.LogEvent(eventName, parameters);
        }

        public static void LogRewardGranted(string rewardType, int amount, string source)
        {
            LogEvent(
                EventRewardGranted,
                new Parameter(ParamRewardType, SanitizeString(rewardType, "unknown")),
                new Parameter(ParamRewardAmount, amount),
                new Parameter(ParamRewardSource, SanitizeString(source, "unknown")));
        }

        public static void LogBoosterUsed(string boosterId, string usagePhase, string levelId)
        {
            LogEvent(
                EventBoosterUsed,
                new Parameter(ParamBoosterId, SanitizeString(boosterId, "unknown")),
                new Parameter(ParamUsagePhase, SanitizeString(usagePhase, "unknown")),
                new Parameter(ParamLevelId, SanitizeString(levelId, "unknown")));
        }

        public static void LogBoosterPurchased(string boosterId, int priceCoins, int quantity)
        {
            LogEvent(
                EventBoosterPurchased,
                new Parameter(ParamBoosterId, SanitizeString(boosterId, "unknown")),
                new Parameter(ParamPriceCoins, priceCoins),
                new Parameter(ParamQuantity, quantity));
        }

        private void CheckDependenciesAndInitialize()
        {
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError($"[FirebaseService] Dependency check failed: {task.Exception}", this);
                    return;
                }

                DependencyStatus status = task.Result;
                if (status != DependencyStatus.Available)
                {
                    Debug.LogError($"[FirebaseService] Firebase dependencies are not available: {status}", this);
                    return;
                }

                FirebaseApp app = FirebaseApp.DefaultInstance;
                FirebaseAnalytics.SetAnalyticsCollectionEnabled(analyticsEnabled);
                Crashlytics.IsCrashlyticsCollectionEnabled = crashlyticsEnabled;

                IsReady = true;
                OnReady?.Invoke();
                SubscribeGameplayEvents();

                if (logLifecycleEvents)
                {
                    LogEvent(EventAppBootstrap);
                }

                _ = app;
            });
        }

        private void SubscribeGameplayEvents()
        {
            if (_eventsSubscribed)
            {
                return;
            }

            EventManager<LogicGameEvent>.AddListener<GameAppStateChangedPayload>(
                LogicGameEvent.GameAppStateChanged,
                HandleGameAppStateChanged);
            EventManager<LogicGameEvent>.AddListener<GameplayHudSnapshot>(
                LogicGameEvent.GameplayWon,
                HandleGameplayWon);
            EventManager<LogicGameEvent>.AddListener<GameplayHudSnapshot>(
                LogicGameEvent.GameplayLost,
                HandleGameplayLost);

            _eventsSubscribed = true;
        }

        private void UnsubscribeGameplayEvents()
        {
            if (!_eventsSubscribed)
            {
                return;
            }

            EventManager<LogicGameEvent>.RemoveListener<GameAppStateChangedPayload>(
                LogicGameEvent.GameAppStateChanged,
                HandleGameAppStateChanged);
            EventManager<LogicGameEvent>.RemoveListener<GameplayHudSnapshot>(
                LogicGameEvent.GameplayWon,
                HandleGameplayWon);
            EventManager<LogicGameEvent>.RemoveListener<GameplayHudSnapshot>(
                LogicGameEvent.GameplayLost,
                HandleGameplayLost);

            _eventsSubscribed = false;
        }

        private void HandleGameAppStateChanged(GameAppStateChangedPayload payload)
        {
            if (payload.Current != GameAppState.EnteringGameplay)
            {
                return;
            }

            string levelId = GameAppFlowManager.Instance != null
                ? GameAppFlowManager.Instance.CurrentLevelId
                : string.Empty;

            LogEvent(EventLevelStart, new Parameter(ParamLevelId, SanitizeString(levelId, "unknown")));
        }

        private void HandleGameplayWon(GameplayHudSnapshot snapshot)
        {
            LogLevelEnd("win", snapshot);
        }

        private void HandleGameplayLost(GameplayHudSnapshot snapshot)
        {
            LogLevelEnd("lose", snapshot);
        }

        private static void LogLevelEnd(string result, GameplayHudSnapshot snapshot)
        {
            string levelId = GameAppFlowManager.Instance != null
                ? GameAppFlowManager.Instance.CurrentLevelId
                : string.Empty;

            LogEvent(
                EventLevelEnd,
                new Parameter(ParamLevelId, SanitizeString(levelId, "unknown")),
                new Parameter(ParamLevelNumber, snapshot?.levelNumber ?? 0),
                new Parameter(ParamResult, result),
                new Parameter(ParamScore, snapshot?.currentScore ?? 0),
                new Parameter(ParamStars, snapshot?.reachedStars ?? 0),
                new Parameter(ParamRemainingMoves, snapshot?.remainingMoves ?? 0));
        }

        private static bool CanLog(string eventName)
        {
            return IsReady && !string.IsNullOrWhiteSpace(eventName);
        }

        private static string SanitizeString(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        private void OnDestroy()
        {
            UnsubscribeGameplayEvents();
            if (Instance == this)
            {
                Instance = null;
                IsReady = false;
            }
        }
    }
}
