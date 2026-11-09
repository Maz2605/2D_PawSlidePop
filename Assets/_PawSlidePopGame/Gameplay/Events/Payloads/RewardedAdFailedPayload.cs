using _PawSlidePopGame._Scripts.Services.Ads;

namespace _PawSlidePopGame._Scripts.Data.Events.Payloads
{
    public readonly struct RewardedAdFailedPayload
    {
        public readonly RewardedAdPlacement placement;
        public readonly string context;
        public readonly string reason;

        public RewardedAdFailedPayload(RewardedAdPlacement placement, string context, string reason)
        {
            this.placement = placement;
            this.context = context;
            this.reason = reason;
        }
    }
}
