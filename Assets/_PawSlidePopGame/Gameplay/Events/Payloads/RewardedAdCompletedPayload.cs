using _PawSlidePopGame._Scripts.Services.Ads;

namespace _PawSlidePopGame._Scripts.Data.Events.Payloads
{
    public readonly struct RewardedAdCompletedPayload
    {
        public readonly RewardedAdPlacement placement;
        public readonly string context;
        public readonly string adRewardType;
        public readonly int adRewardAmount;

        public RewardedAdCompletedPayload(
            RewardedAdPlacement placement,
            string context,
            string adRewardType,
            int adRewardAmount)
        {
            this.placement = placement;
            this.context = context;
            this.adRewardType = adRewardType;
            this.adRewardAmount = adRewardAmount;
        }
    }
}
