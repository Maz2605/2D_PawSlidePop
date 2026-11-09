using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Contracts;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Gameplay.Meta.MapManager
{
    [CreateAssetMenu(fileName = "LevelGenerationProfile", menuName = "_PawSlidePopGame/Map/Level Generation Profile")]
    public sealed class LevelGenerationProfileSO : ScriptableObject
    {
        [Header("Level Range")]
        [SerializeField, Min(1)] private int firstLevelNumber = 1;
        [SerializeField, Min(1)] private int levelCount = 50;
        [SerializeField] private string levelIdPrefix = "Level_";

        [Header("Board")]
        [SerializeField, Min(3)] private int width = 8;
        [SerializeField, Min(3)] private int height = 8;
        [SerializeField, Min(1)] private int baseMovesLimit = 26;
        [SerializeField] private bool requireNoInitialMatches = true;
        [SerializeField] private bool requireAllAllowedSpawnableAppearAtLeastOnce = true;

        [Header("Tiles")]
        [SerializeField] private List<int> allowedSpawnableTileIds = new List<int> { 101, 102, 103, 104, 105, 106, 107, 108, 109 };
        [SerializeField] private List<int> disallowedInitialTileIds = new List<int> { 151, 152, 153, 154, 155, 201, 231, 301, 302, 303 };
        [SerializeField] private List<LevelTargetRequirement> baseTargets = new List<LevelTargetRequirement>
        {
            new LevelTargetRequirement(105, 10),
            new LevelTargetRequirement(301, 5)
        };
        [SerializeField] private List<LevelOverlayRequirement> baseOverlayPlacements = new List<LevelOverlayRequirement>
        {
            new LevelOverlayRequirement(301, 5)
        };

        [Header("Difficulty Scaling")]
        [SerializeField, Min(0)] private int movesAddedEveryTenLevels;
        [SerializeField, Min(0)] private int targetCountAddedEveryTenLevels = 2;

        public int FirstLevelNumber => Mathf.Max(1, firstLevelNumber);
        public int LevelCount => Mathf.Max(1, levelCount);
        public int LastLevelNumber => FirstLevelNumber + LevelCount - 1;

        public LevelGenerationRequest CreateRequest(int displayLevelNumber)
        {
            int safeDisplayNumber = Mathf.Max(1, displayLevelNumber);
            int difficultyStep = Mathf.Max(0, (safeDisplayNumber - FirstLevelNumber) / 10);

            LevelGenerationRequest request = new LevelGenerationRequest
            {
                levelId = FormatLevelId(safeDisplayNumber),
                displayLevelNumber = safeDisplayNumber,
                width = Mathf.Max(3, width),
                height = Mathf.Max(3, height),
                movesLimit = Mathf.Max(1, baseMovesLimit + (difficultyStep * movesAddedEveryTenLevels)),
                requireNoInitialMatches = requireNoInitialMatches,
                requireAllAllowedSpawnableAppearAtLeastOnce = requireAllAllowedSpawnableAppearAtLeastOnce,
                allowedSpawnableTileIds = new List<int>(allowedSpawnableTileIds),
                disallowedInitialTileIds = new List<int>(disallowedInitialTileIds),
                targets = BuildScaledTargets(difficultyStep),
                requiredOverlayPlacements = BuildScaledOverlayPlacements(difficultyStep)
            };

            return request;
        }

        public string FormatLevelId(int displayLevelNumber)
        {
            string prefix = string.IsNullOrWhiteSpace(levelIdPrefix) ? "Level_" : levelIdPrefix.Trim();
            return $"{prefix}{Mathf.Max(1, displayLevelNumber):D3}";
        }

        private List<LevelTargetRequirement> BuildScaledTargets(int difficultyStep)
        {
            List<LevelTargetRequirement> targets = new List<LevelTargetRequirement>();
            for (int i = 0; i < baseTargets.Count; i++)
            {
                LevelTargetRequirement target = baseTargets[i];
                if (target.tileId <= 0 || target.requiredCount <= 0)
                {
                    continue;
                }

                target.requiredCount += difficultyStep * targetCountAddedEveryTenLevels;
                targets.Add(target);
            }

            return targets;
        }

        private List<LevelOverlayRequirement> BuildScaledOverlayPlacements(int difficultyStep)
        {
            List<LevelOverlayRequirement> placements = new List<LevelOverlayRequirement>();
            for (int i = 0; i < baseOverlayPlacements.Count; i++)
            {
                LevelOverlayRequirement placement = baseOverlayPlacements[i];
                if (placement.tileId <= 0 || placement.count <= 0)
                {
                    continue;
                }

                placement.count += difficultyStep;
                placements.Add(placement);
            }

            return placements;
        }
    }
}
