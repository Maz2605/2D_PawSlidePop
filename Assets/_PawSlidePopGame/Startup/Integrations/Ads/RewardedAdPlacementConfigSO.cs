using _PawSlidePopGame._Scripts.Feature.Match3.Boosters;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Services.Ads
{
    [CreateAssetMenu(
        fileName = "RewardedAdPlacementConfig",
        menuName = "_PawSlidePopGame/Ads/Rewarded Placement Config")]
    public sealed class RewardedAdPlacementConfigSO : ScriptableObject
    {
        [Header("Placement")]
        [SerializeField] private RewardedAdPlacement placement = RewardedAdPlacement.CoinWidget;
        [SerializeField] private bool enabled = true;

        [Header("Reward")]
        [SerializeField] private RewardedRewardKind rewardKind = RewardedRewardKind.Coins;
        [SerializeField] private int rewardAmount = 100;
        [SerializeField] private BoosterDefinitionSO boosterReward;

        [Header("Analytics")]
        [SerializeField] private string analyticsPlacementName = "coin_widget";
        [SerializeField] private string rewardReason = "rewarded_ad_coin_widget";

        [Header("Limits")]
        [SerializeField] private int unlockLevel = 1;
        [SerializeField] private int cooldownSeconds;
        [SerializeField] private int dailyLimit;

        public RewardedAdPlacement Placement => placement;
        public bool Enabled => enabled;
        public RewardedRewardKind RewardKind => rewardKind;
        public int RewardAmount => rewardAmount;
        public BoosterDefinitionSO BoosterReward => boosterReward;
        public string AnalyticsPlacementName => analyticsPlacementName;
        public string RewardReason => rewardReason;
        public int UnlockLevel => unlockLevel;
        public int CooldownSeconds => cooldownSeconds;
        public int DailyLimit => dailyLimit;

        private void OnValidate()
        {
            rewardAmount = Mathf.Max(0, rewardAmount);
            unlockLevel = Mathf.Max(1, unlockLevel);
            cooldownSeconds = Mathf.Max(0, cooldownSeconds);
            dailyLimit = Mathf.Max(0, dailyLimit);

            if (string.IsNullOrWhiteSpace(analyticsPlacementName))
            {
                analyticsPlacementName = placement.ToString();
            }

            if (string.IsNullOrWhiteSpace(rewardReason))
            {
                rewardReason = $"rewarded_ad_{analyticsPlacementName}";
            }
        }
    }
}
