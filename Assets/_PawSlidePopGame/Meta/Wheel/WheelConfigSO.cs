using System.Collections.Generic;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Meta.Wheel
{
    [CreateAssetMenu(fileName = "WheelConfig", menuName = "_PawSlidePopGame/Meta/Wheel Config")]
    public sealed class WheelConfigSO : ScriptableObject
    {
        [SerializeField] private List<WheelRewardEntryData> rewards = new List<WheelRewardEntryData>();
        [SerializeField] private float spinDuration = 3.2f;
        [SerializeField] private int minimumFullTurns = 5;
        [SerializeField] private int cooldownSeconds = 86400;
        [SerializeField] private float segmentLandingPaddingDegrees = 4f;

        public IReadOnlyList<WheelRewardEntryData> Rewards => rewards;
        public float SpinDuration => Mathf.Max(0.1f, spinDuration);
        public int MinimumFullTurns => Mathf.Max(1, minimumFullTurns);
        public int CooldownSeconds => Mathf.Max(0, cooldownSeconds);
        public float SegmentLandingPaddingDegrees => Mathf.Max(0f, segmentLandingPaddingDegrees);

        public WheelConfigSnapshot CreateSnapshot()
        {
            return new WheelConfigSnapshot(rewards, SpinDuration, MinimumFullTurns, CooldownSeconds, SegmentLandingPaddingDegrees);
        }

        private void OnValidate()
        {
            spinDuration = Mathf.Max(0.1f, spinDuration);
            minimumFullTurns = Mathf.Max(1, minimumFullTurns);
            cooldownSeconds = Mathf.Max(0, cooldownSeconds);
            segmentLandingPaddingDegrees = Mathf.Max(0f, segmentLandingPaddingDegrees);

            if (rewards == null)
            {
                rewards = new List<WheelRewardEntryData>();
                return;
            }

            for (int i = 0; i < rewards.Count; i++)
            {
                rewards[i]?.Sanitize();
            }
        }
    }
}
