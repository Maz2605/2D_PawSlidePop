using System;
using _PawSlidePopGame._Scripts.Core.Boostrap;
using _PawSlidePopGame._Scripts.Data.Events;
using _PawSlidePopGame._Scripts.Data.Events.Payloads;
using _PawSlidePopGame._Scripts.Services.Analytics;
using _PawSlidePopGame.Scripts.DesignPattern.ObserverPattern;
using Firebase.Analytics;
using GoogleMobileAds.Api;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Services.Ads
{
    [DisallowMultipleComponent]
    public sealed class AdsService : MonoBehaviour, IAppService
    {
        private const string AndroidRewardedTestAdUnitId = "ca-app-pub-3940256099942544/5224354917";
        private const string AndroidInterstitialTestAdUnitId = "ca-app-pub-3940256099942544/1033173712";
        private const string IosRewardedTestAdUnitId = "ca-app-pub-3940256099942544/1712485313";
        private const string IosInterstitialTestAdUnitId = "ca-app-pub-3940256099942544/4411468910";

        private const string EventAdsInitialize = "ads_initialize";
        private const string EventAdLoad = "ad_load";
        private const string EventAdShow = "ad_show";
        private const string EventAdRewardEarned = "ad_reward_earned";
        private const string EventAdFailed = "ad_failed";

        [Header("AdMob Configuration")]
        [SerializeField] private bool useGoogleTestAdUnits = true;

        [Header("Android AdMob Units")]
        [SerializeField] private string androidRewardedAdUnitId = "ca-app-pub-9449221549788378/3393389056";
        [SerializeField] private string androidInterstitialAdUnitId = "";

        [Header("iOS AdMob Units")]
        [SerializeField] private string iosRewardedAdUnitId = "";
        [SerializeField] private string iosInterstitialAdUnitId = "";

        [Header("Loading")]
        [SerializeField] private bool loadRewardedOnInit = true;
        [SerializeField] private bool loadInterstitialOnInit;

        private RewardedAd _rewardedAd;
        private InterstitialAd _interstitialAd;
        private bool _initStarted;
        private bool _isInitialized;
        private bool _rewardEarnedThisShow;
        private RewardedAdRequestPayload _activeRewardedRequest;
        private Action<bool> _activeRewardedCallback;

        private int _rewardedRetryAttempt;
        private int _interstitialRetryAttempt;
        private bool _isShowingAd;
        private bool _isRewardedLoading;
        private bool _isInterstitialLoading;

        public static AdsService Instance { get; private set; }

        public bool IsInitialized => _isInitialized;
        public bool IsRewardedReady => _rewardedAd != null && _rewardedAd.CanShowAd();
        public bool IsInterstitialReady => _interstitialAd != null && _interstitialAd.CanShowAd();

        public void Init()
        {
            if (_initStarted)
            {
                return;
            }

            _initStarted = true;
            Instance = this;
            
            _isShowingAd = false;
            _rewardedRetryAttempt = 0;
            _interstitialRetryAttempt = 0;
            _isRewardedLoading = false;
            _isInterstitialLoading = false;

            EventManager<AdsGameEvent>.AddListener<RewardedAdRequestPayload>(
                AdsGameEvent.RewardedAdRequested,
                HandleRewardedAdRequested);

#pragma warning disable CS0618
            MobileAds.RaiseAdEventsOnUnityMainThread = true;
#pragma warning restore CS0618
            MobileAds.Initialize(_ =>
            {
                _isInitialized = true;
                LogAdEvent(EventAdsInitialize, "sdk", "success");

                if (loadRewardedOnInit)
                {
                    LoadRewardedAd();
                }

                if (loadInterstitialOnInit)
                {
                    LoadInterstitialAd();
                }
            });
        }

        public void LoadRewardedAd()
        {
            if (!_isInitialized || _isRewardedLoading)
            {
                return;
            }

            CancelInvoke(nameof(RetryLoadRewardedAd));
            _isRewardedLoading = true;
            _rewardedAd?.Destroy();
            _rewardedAd = null;

            string adUnitId = GetRewardedAdUnitId();
            RewardedAd.Load(adUnitId, new AdRequest(), (ad, error) =>
            {
                _isRewardedLoading = false;
                if (error != null || ad == null)
                {
                    Debug.LogWarning($"[AdsService] Failed to load rewarded ad: {error}");
                    LogAdEvent(EventAdFailed, "rewarded", "load");
                    PostRewardedAvailabilityForAllPlacements(false);

                    // Exponential backoff retry
                    _rewardedRetryAttempt++;
                    float delay = Mathf.Min(30f, Mathf.Pow(2, _rewardedRetryAttempt));
                    Invoke(nameof(RetryLoadRewardedAd), delay);
                    return;
                }

                _rewardedRetryAttempt = 0;
                _rewardedAd = ad;
                RegisterRewardedEvents(_rewardedAd);
                LogAdEvent(EventAdLoad, "rewarded", "success");
                PostRewardedAvailabilityForAllPlacements(true);
            });
        }

        private void RetryLoadRewardedAd()
        {
            LoadRewardedAd();
        }

        public bool ShowRewardedAd(Action<bool> onComplete = null)
        {
            return ShowRewardedAd(new RewardedAdRequestPayload(RewardedAdPlacement.CoinWidget, "direct_api"), onComplete);
        }

        private void HandleRewardedAdRequested(RewardedAdRequestPayload payload)
        {
            ShowRewardedAd(payload, null);
        }

        private bool ShowRewardedAd(RewardedAdRequestPayload payload, Action<bool> onComplete = null)
        {
            if (_isShowingAd)
            {
                onComplete?.Invoke(false);
                LogAdEvent(EventAdFailed, "rewarded", "already_showing");
                PostRewardedFailed(payload, "already_showing");
                return false;
            }
            if (!IsRewardedReady)
            {
                onComplete?.Invoke(false);
                LogAdEvent(EventAdFailed, "rewarded", "not_ready");
                PostRewardedFailed(payload, "not_ready");
                PostRewardedAvailabilityForAllPlacements(false);
                LoadRewardedAd();
                return false;
            }
            _isShowingAd = true;
            _activeRewardedRequest = payload;
            _activeRewardedCallback = onComplete;
            _rewardEarnedThisShow = false;
            LogAdEvent(EventAdShow, "rewarded", "requested");
            EventManager<AdsGameEvent>.Post(AdsGameEvent.RewardedAdStarted, payload);
            _rewardedAd.Show(reward =>
            {
                _rewardEarnedThisShow = true;
                int amount = Mathf.RoundToInt((float)reward.Amount);
                LogAdEvent(EventAdRewardEarned, "rewarded", reward.Type);
                EventManager<AdsGameEvent>.Post(
                    AdsGameEvent.RewardedAdCompleted,
                    new RewardedAdCompletedPayload(
                        _activeRewardedRequest.placement,
                        _activeRewardedRequest.context,
                        reward.Type,
                        amount));
                CompleteRewardedCallback(true);
            });
            return true;
        }

        public void LoadInterstitialAd()
        {
            if (!_isInitialized || _isInterstitialLoading)
            {
                return;
            }

            string adUnitId = GetInterstitialAdUnitId();
            if (string.IsNullOrWhiteSpace(adUnitId))
            {
                return;
            }

            CancelInvoke(nameof(RetryLoadInterstitialAd));
            _isInterstitialLoading = true;
            _interstitialAd?.Destroy();
            _interstitialAd = null;

            InterstitialAd.Load(adUnitId, new AdRequest(), (ad, error) =>
            {
                _isInterstitialLoading = false;
                if (error != null || ad == null)
                {
                    Debug.LogWarning($"[AdsService] Failed to load interstitial ad: {error}");
                    LogAdEvent(EventAdFailed, "interstitial", "load");

                    // Exponential backoff retry
                    _interstitialRetryAttempt++;
                    float delay = Mathf.Min(30f, Mathf.Pow(2, _interstitialRetryAttempt));
                    Invoke(nameof(RetryLoadInterstitialAd), delay);
                    return;
                }

                _interstitialRetryAttempt = 0;
                _interstitialAd = ad;
                RegisterInterstitialEvents(_interstitialAd);
                LogAdEvent(EventAdLoad, "interstitial", "success");
            });
        }

        private void RetryLoadInterstitialAd()
        {
            LoadInterstitialAd();
        }

        public bool ShowInterstitialAd(Action<bool> onClosed = null)
        {
            if (_isShowingAd)
            {
                onClosed?.Invoke(false);
                LogAdEvent(EventAdFailed, "interstitial", "already_showing");
                return false;
            }

            if (!IsInterstitialReady)
            {
                onClosed?.Invoke(false);
                LogAdEvent(EventAdFailed, "interstitial", "not_ready");
                LoadInterstitialAd();
                return false;
            }

            _isShowingAd = true;

            InterstitialAd targetAd = _interstitialAd;
            Action closedHandler = null;
            Action<AdError> failedHandler = null;

            closedHandler = () =>
            {
                _isShowingAd = false;
                if (targetAd != null)
                {
                    targetAd.OnAdFullScreenContentClosed -= closedHandler;
                    targetAd.OnAdFullScreenContentFailed -= failedHandler;
                }
                onClosed?.Invoke(true);
            };

            failedHandler = error =>
            {
                _isShowingAd = false;
                if (targetAd != null)
                {
                    targetAd.OnAdFullScreenContentClosed -= closedHandler;
                    targetAd.OnAdFullScreenContentFailed -= failedHandler;
                }
                onClosed?.Invoke(false);
            };

            targetAd.OnAdFullScreenContentClosed += closedHandler;
            targetAd.OnAdFullScreenContentFailed += failedHandler;

            LogAdEvent(EventAdShow, "interstitial", "requested");
            targetAd.Show();
            return true;
        }

        private void RegisterRewardedEvents(RewardedAd ad)
        {
            ad.OnAdFullScreenContentOpened += () => LogAdEvent(EventAdShow, "rewarded", "opened");
            ad.OnAdImpressionRecorded += () => LogAdEvent(EventAdShow, "rewarded", "impression");
            ad.OnAdFullScreenContentClosed += () =>
            {
                _isShowingAd = false;
                if (!_rewardEarnedThisShow)
                {
                    PostRewardedFailed(_activeRewardedRequest, "closed_without_reward");
                    CompleteRewardedCallback(false);
                }

                LoadRewardedAd();
            };
            ad.OnAdFullScreenContentFailed += error =>
            {
                _isShowingAd = false;
                Debug.LogWarning($"[AdsService] Rewarded ad failed while showing: {error}");
                LogAdEvent(EventAdFailed, "rewarded", "show");
                PostRewardedFailed(_activeRewardedRequest, "show_failed");
                PostRewardedAvailabilityForAllPlacements(false);
                CompleteRewardedCallback(false);
                LoadRewardedAd();
            };
        }

        private void RegisterInterstitialEvents(InterstitialAd ad)
        {
            ad.OnAdFullScreenContentOpened += () => LogAdEvent(EventAdShow, "interstitial", "opened");
            ad.OnAdImpressionRecorded += () => LogAdEvent(EventAdShow, "interstitial", "impression");
            ad.OnAdFullScreenContentClosed += () =>
            {
                _isShowingAd = false;
                LoadInterstitialAd();
            };
            ad.OnAdFullScreenContentFailed += error =>
            {
                _isShowingAd = false;
                Debug.LogWarning($"[AdsService] Interstitial ad failed while showing: {error}");
                LogAdEvent(EventAdFailed, "interstitial", "show");
                LoadInterstitialAd();
            };
        }

        private void CompleteRewardedCallback(bool rewarded)
        {
            Action<bool> callback = _activeRewardedCallback;
            _activeRewardedCallback = null;
            callback?.Invoke(rewarded);
        }

        private string GetRewardedAdUnitId()
        {
#if UNITY_EDITOR
            return AndroidRewardedTestAdUnitId;
#elif UNITY_ANDROID
            return useGoogleTestAdUnits ? AndroidRewardedTestAdUnitId : androidRewardedAdUnitId;
#elif UNITY_IOS
            return useGoogleTestAdUnits ? IosRewardedTestAdUnitId : iosRewardedAdUnitId;
#else
            return string.Empty;
#endif
        }

        private string GetInterstitialAdUnitId()
        {
#if UNITY_EDITOR
            return AndroidInterstitialTestAdUnitId;
#elif UNITY_ANDROID
            return useGoogleTestAdUnits ? AndroidInterstitialTestAdUnitId : androidInterstitialAdUnitId;
#elif UNITY_IOS
            return useGoogleTestAdUnits ? IosInterstitialTestAdUnitId : iosInterstitialAdUnitId;
#else
            return string.Empty;
#endif
        }

        private static void LogAdEvent(string eventName, string adFormat, string status)
        {
            FirebaseService.LogEvent(
                eventName,
                new Parameter("ad_network", "admob"),
                new Parameter("ad_format", adFormat),
                new Parameter("status", status));
        }

        private static void PostRewardedFailed(RewardedAdRequestPayload payload, string reason)
        {
            EventManager<AdsGameEvent>.Post(
                AdsGameEvent.RewardedAdFailed,
                new RewardedAdFailedPayload(payload.placement, payload.context, reason));
        }

        private static void PostRewardedAvailabilityForAllPlacements(bool isAvailable)
        {
            Array values = Enum.GetValues(typeof(RewardedAdPlacement));
            for (int i = 0; i < values.Length; i++)
            {
                EventManager<AdsGameEvent>.Post(
                    AdsGameEvent.RewardedAdAvailabilityChanged,
                    new RewardedAdAvailabilityPayload((RewardedAdPlacement)values.GetValue(i), isAvailable));
            }
        }

        private void OnDestroy()
        {
            CancelInvoke(nameof(RetryLoadRewardedAd));
            CancelInvoke(nameof(RetryLoadInterstitialAd));

            _rewardedAd?.Destroy();
            _interstitialAd?.Destroy();

            if (_initStarted)
            {
                EventManager<AdsGameEvent>.RemoveListener<RewardedAdRequestPayload>(
                    AdsGameEvent.RewardedAdRequested,
                    HandleRewardedAdRequested);
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
