using System;
using _PawSlidePopGame._Scripts.Data.Events.Payloads;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Flow
{
    public static class GameplayHudSnapshotBuilder
    {
        public static GameplayHudSnapshot Build(
            Match3LevelData levelData,
            BoardModel board,
            Match3ObjectiveTracker objectiveTracker,
            ChargedAbilityTracker chargedAbilityTracker = null,
            bool isPlacementMode = false,
            bool isComboSelectionMode = false)
        {
            GameplayHudSnapshot snapshot = new GameplayHudSnapshot
            {
                levelNumber = levelData != null ? levelData.DisplayLevelNumber : 1,
                remainingMoves = board != null ? board.RemainingMoves : 0,
                currentScore = board != null ? board.CurrentScore : 0,
                starScoreThresholds = levelData != null ? levelData.GetSafeStarThresholds() : Array.Empty<int>()
            };

            snapshot.reachedStars = CountReachedStars(snapshot.currentScore, snapshot.starScoreThresholds);
            if (objectiveTracker != null)
            {
                for (int i = 0; i < objectiveTracker.Targets.Count; i++)
                {
                    snapshot.targets.Add(objectiveTracker.Targets[i]?.Clone());
                }
            }

            snapshot.chargedAbility = chargedAbilityTracker != null
                ? chargedAbilityTracker.BuildHudData(isPlacementMode, isComboSelectionMode)
                : null;

            return snapshot;
        }

        public static int CountReachedStars(int score, int[] thresholds)
        {
            if (thresholds == null || thresholds.Length == 0)
            {
                return 0;
            }

            int reachedStars = 0;
            for (int i = 0; i < thresholds.Length; i++)
            {
                if (score >= thresholds[i])
                {
                    reachedStars++;
                }
            }

            return reachedStars;
        }
    }
}

