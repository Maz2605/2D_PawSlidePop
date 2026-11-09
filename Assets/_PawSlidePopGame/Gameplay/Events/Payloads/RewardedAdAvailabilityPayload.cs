using _PawSlidePopGame._Scripts.Services.Ads;

namespace _PawSlidePopGame._Scripts.Data.Events.Payloads
{
    public readonly struct RewardedAdAvailabilityPayload
    {
        public readonly RewardedAdPlacement placement;
        public readonly bool isAvailable;

        public RewardedAdAvailabilityPayload(RewardedAdPlacement placement, bool isAvailable)
        {
            this.placement = placement;
            this.isAvailable = isAvailable;
        }
    }
}
