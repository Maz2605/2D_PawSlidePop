using System;
using System.Collections.Generic;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Data.Events.Payloads
{
    [Serializable]
    public sealed class TargetProgressData
    {
        public int tileId;
        public Sprite icon;
        public int currentCount;
        public int requiredCount;
        public bool isCompleted;

        public TargetProgressData Clone()
        {
            return new TargetProgressData
            {
                tileId = tileId,
                icon = icon,
                currentCount = currentCount,
                requiredCount = requiredCount,
                isCompleted = isCompleted
            };
        }
    }

    [Serializable]
    public sealed class GameplayHudSnapshot
    {
        [Serializable]
        public sealed class ChargedAbilityHudData
        {
            public bool isEnabled;
            public int tileId;
            public Sprite icon;
            public int currentEnergy;
            public int requiredEnergy;
            public int availableCharges;
            public int maxStoredCharges;
            public bool isPlacementMode;
            public bool isComboSelectionMode;

            public ChargedAbilityHudData Clone()
            {
                return new ChargedAbilityHudData
                {
                    isEnabled = isEnabled,
                    tileId = tileId,
                    icon = icon,
                    currentEnergy = currentEnergy,
                    requiredEnergy = requiredEnergy,
                    availableCharges = availableCharges,
                    maxStoredCharges = maxStoredCharges,
                    isPlacementMode = isPlacementMode,
                    isComboSelectionMode = isComboSelectionMode
                };
            }
        }

        public int levelNumber;
        public int remainingMoves;
        public int currentScore;
        public int[] starScoreThresholds;
        public int reachedStars;
        public List<TargetProgressData> targets = new List<TargetProgressData>();
        public ChargedAbilityHudData chargedAbility;

        public bool HasTargets => targets != null && targets.Count > 0;
        public bool AreAllTargetsCompleted => HasTargets && targets.TrueForAll(target => target != null && target.isCompleted);

        public GameplayHudSnapshot Clone()
        {
            GameplayHudSnapshot clone = new GameplayHudSnapshot
            {
                levelNumber = levelNumber,
                remainingMoves = remainingMoves,
                currentScore = currentScore,
                reachedStars = reachedStars,
                starScoreThresholds = starScoreThresholds != null ? (int[])starScoreThresholds.Clone() : Array.Empty<int>()
            };

            if (targets != null)
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    clone.targets.Add(targets[i]?.Clone());
                }
            }

            clone.chargedAbility = chargedAbility?.Clone();

            return clone;
        }
    }
}
