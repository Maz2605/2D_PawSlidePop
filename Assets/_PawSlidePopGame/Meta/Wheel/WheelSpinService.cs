using System;
using System.Collections.Generic;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Meta.Wheel
{
    public static class WheelSpinService
    {
        public static int SelectRewardIndex(IReadOnlyList<WheelRewardEntryData> rewards, float roll01)
        {
            if (rewards == null || rewards.Count == 0)
            {
                return -1;
            }

            int totalWeight = 0;
            for (int i = 0; i < rewards.Count; i++)
            {
                if (rewards[i] != null && rewards[i].CanSpin())
                {
                    totalWeight += rewards[i].Weight;
                }
            }

            if (totalWeight <= 0)
            {
                return -1;
            }

            int target = Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(roll01) * totalWeight), 0, totalWeight - 1);
            int cumulative = 0;
            for (int i = 0; i < rewards.Count; i++)
            {
                WheelRewardEntryData reward = rewards[i];
                if (reward == null || !reward.CanSpin())
                {
                    continue;
                }

                cumulative += reward.Weight;
                if (target < cumulative)
                {
                    return i;
                }
            }

            return -1;
        }

        public static float CalculateTargetZAngle(
            int rewardIndex,
            int rewardCount,
            float currentZAngle,
            int minimumFullTurns,
            float landingPaddingDegrees,
            float random01,
            float pointerAngleDegrees = 90f,
            WheelSpinDirection direction = WheelSpinDirection.Clockwise)
        {
            if (rewardIndex < 0 || rewardCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rewardIndex));
            }

            float segmentAngle = 360f / rewardCount;
            // Force offset to 0 so the winning item lands exactly centered at the pointer (pointing straight UP)
            float offset = 0f;
            float rewardCenterAngle = pointerAngleDegrees - rewardIndex * segmentAngle + offset;
            float desiredWheelAngle = pointerAngleDegrees - rewardCenterAngle;
            float normalizedCurrent = NormalizeAngle(currentZAngle);
            float delta = direction == WheelSpinDirection.Clockwise
                ? Mathf.Repeat(desiredWheelAngle - normalizedCurrent, 360f)
                : -Mathf.Repeat(normalizedCurrent - desiredWheelAngle, 360f);

            float fullTurns = Mathf.Max(1, minimumFullTurns) * 360f;
            return direction == WheelSpinDirection.Clockwise
                ? currentZAngle + fullTurns + delta
                : currentZAngle - fullTurns + delta;
        }

        private static float NormalizeAngle(float angle)
        {
            return Mathf.Repeat(angle, 360f);
        }
    }
}
