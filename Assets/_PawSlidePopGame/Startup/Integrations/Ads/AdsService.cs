using System;
using _PawSlidePopGame._Scripts.Core.Boostrap;
using _PawSlidePopGame._Scripts.Data.Events;
using _PawSlidePopGame._Scripts.Data.Events.Payloads;
using _PawSlidePopGame._Scripts.Services.Analytics;
using MazFramework.Foundation.Events;
using MazFramework.Integrations.AdMob;
using Firebase.Analytics;
using UnityEngine;
namespace _PawSlidePopGame._Scripts.Services.Ads
{
    /// <summary>Game placements, events and analytics; transport lives in AdMobClient.</summary>
    public sealed class AdsService : MonoBehaviour,IAppService
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

        private AdMobClient _client;
        private RewardedAdRequestPayload _activeRequest;
        public static AdsService Instance { get; private set; }
        public bool IsInitialized => _client!=null && _client.IsInitialized;
        public bool IsRewardedReady => _client!=null && _client.IsRewardedReady && !_client.IsShowing;
        public bool IsInterstitialReady => _client!=null && _client.IsInterstitialReady && !_client.IsShowing;
        public void Init()
        {
            if(_client!=null) return;
            Instance=this;
            _client=gameObject.AddComponent<AdMobClient>();
            _client.RewardedAvailabilityChanged+=PostAvailability;
            _client.Telemetry+=LogAdEvent;
            _client.RewardEarned+=HandleReward;
            EventManager<AdsGameEvent>.AddListener<RewardedAdRequestPayload>(AdsGameEvent.RewardedAdRequested,HandleRequest);
            _client.Initialize(GetRewardedAdUnitId(),GetInterstitialAdUnitId(),loadRewardedOnInit,loadInterstitialOnInit);
        }
        public void LoadRewardedAd() => _client?.LoadRewarded();
        public void LoadInterstitialAd() => _client?.LoadInterstitial();
        public bool ShowRewardedAd(Action<bool> onComplete=null) => Show(new RewardedAdRequestPayload(RewardedAdPlacement.CoinWidget,"direct_api"),onComplete);
        private void HandleRequest(RewardedAdRequestPayload request) => Show(request,null);
        private bool Show(RewardedAdRequestPayload request,Action<bool> callback)
        {
            if(_client!=null && _client.IsShowing)
            {
                EventManager<AdsGameEvent>.Post(AdsGameEvent.RewardedAdFailed,new RewardedAdFailedPayload(request.placement,request.context,"already_showing"));
                callback?.Invoke(false); return false;
            }
            if(!IsRewardedReady)
            {
                EventManager<AdsGameEvent>.Post(AdsGameEvent.RewardedAdFailed,new RewardedAdFailedPayload(request.placement,request.context,"not_ready"));
                callback?.Invoke(false); LoadRewardedAd(); return false;
            }
            _activeRequest=request;
            EventManager<AdsGameEvent>.Post(AdsGameEvent.RewardedAdStarted,request);
            return _client.ShowRewarded(success=>
            {
                if(!success) EventManager<AdsGameEvent>.Post(AdsGameEvent.RewardedAdFailed,new RewardedAdFailedPayload(request.placement,request.context,"closed_without_reward_or_show_failed"));
                callback?.Invoke(success);
            });
        }
        private void HandleReward(string type,int amount)
        {
            EventManager<AdsGameEvent>.Post(AdsGameEvent.RewardedAdCompleted,
                new RewardedAdCompletedPayload(_activeRequest.placement,_activeRequest.context,type,amount));
        }
        public bool ShowInterstitialAd(Action<bool> onClosed=null)
        {
            if(_client!=null) return _client.ShowInterstitial(onClosed);
            onClosed?.Invoke(false); return false;
        }
        private static void PostAvailability(bool ready)
        {
            foreach(RewardedAdPlacement placement in Enum.GetValues(typeof(RewardedAdPlacement)))
                EventManager<AdsGameEvent>.Post(AdsGameEvent.RewardedAdAvailabilityChanged,new RewardedAdAvailabilityPayload(placement,ready));
        }
        private static void LogAdEvent(string name,string format,string status) => FirebaseService.LogEvent(name,
            new Parameter("ad_network","admob"),new Parameter("ad_format",format),new Parameter("status",status));
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

        private void OnDestroy()
        {
            if(_client!=null)
            {
                _client.RewardedAvailabilityChanged-=PostAvailability;
                _client.Telemetry-=LogAdEvent;
                _client.RewardEarned-=HandleReward;
            }
            EventManager<AdsGameEvent>.RemoveListener<RewardedAdRequestPayload>(AdsGameEvent.RewardedAdRequested,HandleRequest);
            if(Instance==this) Instance=null;
        }
    }
}
