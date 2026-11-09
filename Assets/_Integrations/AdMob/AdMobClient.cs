using System;
using GoogleMobileAds.Api;
using UnityEngine;

namespace MazFramework.Integrations.AdMob
{
    /// <summary>AdMob transport and ad lifetime. Placements/rewards belong to the game.</summary>
    public sealed class AdMobClient : MonoBehaviour
    {
        private RewardedAd _rewarded;
        private InterstitialAd _interstitial;
        private string _rewardedId, _interstitialId;
        private bool _initializing, _loadingRewarded, _loadingInterstitial, _showing;
        private bool _rewardEarned;
        private int _rewardedRetry, _interstitialRetry;
        private Action<bool> _completion;
        public bool IsInitialized { get; private set; }
        public bool IsShowing => _showing;
        public bool IsRewardedReady => _rewarded != null && _rewarded.CanShowAd();
        public bool IsInterstitialReady => _interstitial != null && _interstitial.CanShowAd();
        public event Action<bool> RewardedAvailabilityChanged;
        public event Action<string,string,string> Telemetry;
        public event Action<string,int> RewardEarned;

        public void Initialize(string rewardedId, string interstitialId, bool loadRewarded, bool loadInterstitial)
        {
            if (_initializing) return;
            _initializing = true;
            _rewardedId = rewardedId;
            _interstitialId = interstitialId;
#pragma warning disable CS0618
            MobileAds.RaiseAdEventsOnUnityMainThread = true;
#pragma warning restore CS0618
            MobileAds.Initialize(_ =>
            {
                if (this == null) return;
                IsInitialized = true;
                Telemetry?.Invoke("ads_initialize", "sdk", "success");
                if (loadRewarded) LoadRewarded();
                if (loadInterstitial) LoadInterstitial();
            });
        }

        public void LoadRewarded()
        {
            if (!IsInitialized || _showing || _loadingRewarded || string.IsNullOrEmpty(_rewardedId)) return;
            CancelInvoke(nameof(LoadRewarded));
            _loadingRewarded = true;
            _rewarded?.Destroy(); _rewarded = null;
            RewardedAvailabilityChanged?.Invoke(false);
            RewardedAd.Load(_rewardedId, new AdRequest(), (ad, error) =>
            {
                if (this == null) { ad?.Destroy(); return; }
                _loadingRewarded = false;
                if (error != null || ad == null)
                {
                    Telemetry?.Invoke("ad_failed", "rewarded", "load");
                    Invoke(nameof(LoadRewarded), Mathf.Min(30f, Mathf.Pow(2, ++_rewardedRetry)));
                    return;
                }
                _rewardedRetry = 0; _rewarded = ad;
                ad.OnAdFullScreenContentOpened += () => Telemetry?.Invoke("ad_show", "rewarded", "opened");
                ad.OnAdImpressionRecorded += () => Telemetry?.Invoke("ad_show", "rewarded", "impression");
                ad.OnAdFullScreenContentClosed += () => { Complete(_rewardEarned); LoadRewarded(); };
                ad.OnAdFullScreenContentFailed += _ => { Complete(false); LoadRewarded(); };
                Telemetry?.Invoke("ad_load", "rewarded", "success");
                RewardedAvailabilityChanged?.Invoke(true);
            });
        }

        public void LoadInterstitial()
        {
            if (!IsInitialized || _showing || _loadingInterstitial || string.IsNullOrEmpty(_interstitialId)) return;
            CancelInvoke(nameof(LoadInterstitial));
            _loadingInterstitial = true;
            _interstitial?.Destroy(); _interstitial = null;
            InterstitialAd.Load(_interstitialId, new AdRequest(), (ad, error) =>
            {
                if (this == null) { ad?.Destroy(); return; }
                _loadingInterstitial = false;
                if (error != null || ad == null)
                {
                    Telemetry?.Invoke("ad_failed", "interstitial", "load");
                    Invoke(nameof(LoadInterstitial), Mathf.Min(30f, Mathf.Pow(2, ++_interstitialRetry)));
                    return;
                }
                _interstitialRetry = 0; _interstitial = ad;
                ad.OnAdFullScreenContentClosed += () => { Complete(true); LoadInterstitial(); };
                ad.OnAdFullScreenContentFailed += _ => { Complete(false); LoadInterstitial(); };
                ad.OnAdFullScreenContentOpened += () => Telemetry?.Invoke("ad_show", "interstitial", "opened");
                ad.OnAdImpressionRecorded += () => Telemetry?.Invoke("ad_show", "interstitial", "impression");
                Telemetry?.Invoke("ad_load", "interstitial", "success");
            });
        }

        public bool ShowRewarded(Action<bool> completed)
        {
            if (_showing || !IsRewardedReady) { completed?.Invoke(false); return false; }
            _showing = true; _rewardEarned = false; _completion = completed;
            RewardedAvailabilityChanged?.Invoke(false);
            Telemetry?.Invoke("ad_show", "rewarded", "requested");
            try
            {
                _rewarded.Show(reward =>
                {
                    if (this == null || _rewardEarned) return;
                    _rewardEarned = true;
                    Telemetry?.Invoke("ad_reward_earned", "rewarded", reward.Type);
                    RewardEarned?.Invoke(reward.Type, Mathf.RoundToInt((float)reward.Amount));
                    Action<bool> callback = _completion; _completion = null;
                    callback?.Invoke(true);
                });
            }
            catch (Exception e) { Debug.LogWarning("[AdMob] Show failed: " + e.Message); Complete(false); LoadRewarded(); return false; }
            return true;
        }

        public bool ShowInterstitial(Action<bool> completed)
        {
            if (_showing || !IsInterstitialReady) { completed?.Invoke(false); return false; }
            _showing = true; _completion = completed;
            try { _interstitial.Show(); }
            catch (Exception e) { Debug.LogWarning("[AdMob] Show failed: " + e.Message); Complete(false); LoadInterstitial(); return false; }
            return true;
        }

        private void Complete(bool success)
        {
            _showing = false;
            Action<bool> callback = _completion; _completion = null;
            callback?.Invoke(success);
        }

        private void OnDestroy()
        {
            CancelInvoke();
            _rewarded?.Destroy(); _interstitial?.Destroy();
            Complete(false);
        }
    }
}
