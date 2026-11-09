using System;
using _PawSlidePopGame._Scripts.Core.Boostrap;
using _PawSlidePopGame._Scripts.Data.Events;
using _PawSlidePopGame._Scripts.Data.Events.Payloads;
using _PawSlidePopGame._Scripts.Gameplay.Meta.EconomyManager;
using _PawSlidePopGame._Scripts.Gameplay.Meta.Inventory;
using _PawSlidePopGame._Scripts.Services.Analytics;
using _PawSlidePopGame.Scripts.DesignPattern.ObserverPattern;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Services.Ads
{
    [DisallowMultipleComponent]
    public sealed class RewardedAdRewardService : MonoBehaviour, IAppService
    {
        private const string PlayerPrefsPrefix = "rewarded_ad_placement";

        [SerializeField] private RewardedAdPlacementCatalogSO placementCatalog;
        [SerializeField] private bool logRewardGrants = true;

        private bool _initStarted;

        public void Init()
        {
            if (_initStarted)
            {
                return;
            }

            _initStarted = true;
            EventManager<AdsGameEvent>.AddListener<RewardedAdCompletedPayload>(
                AdsGameEvent.RewardedAdCompleted,
                HandleRewardedAdCompleted);
        }

        private void HandleRewardedAdCompleted(RewardedAdCompletedPayload payload)
        {
            if (payload.placement == RewardedAdPlacement.FreeBooster || payload.placement == RewardedAdPlacement.FreeHeart)
            {
                return;
            }

            if (placementCatalog == null)
            {
                Debug.LogWarning("[RewardedAdRewardService] Missing placement catalog.", this);
                return;
            }

            if (!placementCatalog.TryGetConfig(payload.placement, out RewardedAdPlacementConfigSO config))
            {
                Debug.LogWarning($"[RewardedAdRewardService] Missing rewarded placement config for {payload.placement}.", this);
                return;
            }

            if (!CanGrant(config))
            {
                return;
            }

            if (!TryGrantReward(config))
            {
                return;
            }

            MarkGranted(config);

            if (logRewardGrants)
            {
                FirebaseService.LogRewardGranted(
                    config.RewardKind.ToString(),
                    config.RewardAmount,
                    config.RewardReason);
            }
        }

        private bool CanGrant(RewardedAdPlacementConfigSO config)
        {
            if (config == null || !config.Enabled)
            {
                return false;
            }

            if (GetCurrentPlayerLevel() < config.UnlockLevel)
            {
                Debug.Log($"[RewardedAdRewardService] Placement {config.Placement} is locked until level {config.UnlockLevel}.");
                return false;
            }

            if (IsCoolingDown(config))
            {
                Debug.Log($"[RewardedAdRewardService] Placement {config.Placement} is cooling down.");
                return false;
            }

            if (HasReachedDailyLimit(config))
            {
                Debug.Log($"[RewardedAdRewardService] Placement {config.Placement} reached daily limit.");
                return false;
            }

            return true;
        }

        private bool TryGrantReward(RewardedAdPlacementConfigSO config)
        {
            switch (config.RewardKind)
            {
                case RewardedRewardKind.None:
                    return true;

                case RewardedRewardKind.Coins:
                    EconomyManager.Instance?.AddCoins(config.RewardAmount, config.RewardReason);
                    return EconomyManager.Instance != null;

                case RewardedRewardKind.Hearts:
                    HeartManager.Instance?.AddHearts(config.RewardAmount);
                    return HeartManager.Instance != null;

                case RewardedRewardKind.Booster:
                    if (config.BoosterReward == null)
                    {
                        Debug.LogWarning($"[RewardedAdRewardService] Placement {config.Placement} has no booster reward configured.", this);
                        return false;
                    }

                    BoosterInventory.Instance?.AddBooster(config.BoosterReward, config.RewardAmount, config.RewardReason);
                    return BoosterInventory.Instance != null;

                case RewardedRewardKind.Moves:
                case RewardedRewardKind.WheelSpin:
                case RewardedRewardKind.Revive:
                case RewardedRewardKind.DoublePendingReward:
                    Debug.LogWarning($"[RewardedAdRewardService] Reward kind {config.RewardKind} is configured but not implemented yet.", this);
                    return false;

                default:
                    Debug.LogWarning($"[RewardedAdRewardService] Unsupported reward kind {config.RewardKind}.", this);
                    return false;
            }
        }

        private static int GetCurrentPlayerLevel()
        {
            return 1;
        }

        private static bool IsCoolingDown(RewardedAdPlacementConfigSO config)
        {
            if (config.CooldownSeconds <= 0)
            {
                return false;
            }

            long lastGrantUnix = Convert.ToInt64(PlayerPrefs.GetString(GetCooldownKey(config.Placement), "0"));
            long nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return nowUnix - lastGrantUnix < config.CooldownSeconds;
        }

        private static bool HasReachedDailyLimit(RewardedAdPlacementConfigSO config)
        {
            if (config.DailyLimit <= 0)
            {
                return false;
            }

            string today = DateTime.UtcNow.ToString("yyyyMMdd");
            string dateKey = GetDailyDateKey(config.Placement);
            string countKey = GetDailyCountKey(config.Placement);
            if (PlayerPrefs.GetString(dateKey, string.Empty) != today)
            {
                return false;
            }

            return PlayerPrefs.GetInt(countKey, 0) >= config.DailyLimit;
        }

        private static void MarkGranted(RewardedAdPlacementConfigSO config)
        {
            if (config.CooldownSeconds > 0)
            {
                PlayerPrefs.SetString(
                    GetCooldownKey(config.Placement),
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
            }

            if (config.DailyLimit > 0)
            {
                string today = DateTime.UtcNow.ToString("yyyyMMdd");
                string dateKey = GetDailyDateKey(config.Placement);
                string countKey = GetDailyCountKey(config.Placement);
                if (PlayerPrefs.GetString(dateKey, string.Empty) != today)
                {
                    PlayerPrefs.SetString(dateKey, today);
                    PlayerPrefs.SetInt(countKey, 0);
                }

                PlayerPrefs.SetInt(countKey, PlayerPrefs.GetInt(countKey, 0) + 1);
            }

            PlayerPrefs.Save();
        }

        private static string GetCooldownKey(RewardedAdPlacement placement)
        {
            return $"{PlayerPrefsPrefix}_{placement}_last_grant";
        }

        private static string GetDailyDateKey(RewardedAdPlacement placement)
        {
            return $"{PlayerPrefsPrefix}_{placement}_daily_date";
        }

        private static string GetDailyCountKey(RewardedAdPlacement placement)
        {
            return $"{PlayerPrefsPrefix}_{placement}_daily_count";
        }

        private void OnDestroy()
        {
            if (!_initStarted)
            {
                return;
            }

            EventManager<AdsGameEvent>.RemoveListener<RewardedAdCompletedPayload>(
                AdsGameEvent.RewardedAdCompleted,
                HandleRewardedAdCompleted);
        }
    }
}
