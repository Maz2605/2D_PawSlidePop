using _PawSlidePopGame._Scripts.Services.Ads;

namespace _PawSlidePopGame._Scripts.Data.Events.Payloads
{
    public readonly struct RewardedAdRequestPayload
    {
        public readonly RewardedAdPlacement placement;
        public readonly string context;

        public RewardedAdRequestPayload(RewardedAdPlacement placement, string context = null)
        {
            this.placement = placement;
            this.context = context;
        }
    }
}
