using System.Collections.Generic;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Meta.Wheel
{
    public sealed class WheelConfigSnapshot
    {
        private readonly List<WheelRewardEntryData> _rewards;

        public WheelConfigSnapshot(
            IEnumerable<WheelRewardEntryData> rewards,
            float spinDuration,
            int minimumFullTurns,
            int cooldownSeconds,
            float segmentLandingPaddingDegrees)
        {
            _rewards = rewards != null ? new List<WheelRewardEntryData>(rewards) : new List<WheelRewardEntryData>();
            SpinDuration = Mathf.Max(0.1f, spinDuration);
            MinimumFullTurns = Mathf.Max(1, minimumFullTurns);
            CooldownSeconds = Mathf.Max(0, cooldownSeconds);
            SegmentLandingPaddingDegrees = Mathf.Max(0f, segmentLandingPaddingDegrees);
        }

        public IReadOnlyList<WheelRewardEntryData> Rewards => _rewards;
        public float SpinDuration { get; }
        public int MinimumFullTurns { get; }
        public int CooldownSeconds { get; }
        public float SegmentLandingPaddingDegrees { get; }

        public bool HasSpinableReward()
        {
            for (int i = 0; i < _rewards.Count; i++)
            {
                if (_rewards[i] != null && _rewards[i].CanSpin())
                {
                    return true;
                }
            }

            return false;
        }
    }
}
