using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Contracts;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.Temporary
{
    public static class TempLevel001GenerationProfile
    {
        public static LevelGenerationRequest CreateRequest()
        {
            return new LevelGenerationRequest
            {
                levelId = "Level_001",
                displayLevelNumber = 1,
                width = 8,
                height = 8,
                movesLimit = 26,
                requireNoInitialMatches = true,
                requireAllAllowedSpawnableAppearAtLeastOnce = true,
                allowedSpawnableTileIds = new List<int> { 101, 102, 103, 104, 105, 106, 107, 108, 109 },
                disallowedInitialTileIds = new List<int> { 151, 152, 153, 154, 155, 201, 231, 301, 302, 303 },
                targets = new List<LevelTargetRequirement>
                {
                    new LevelTargetRequirement(105, 10),
                    new LevelTargetRequirement(301, 5)
                },
                requiredOverlayPlacements = new List<LevelOverlayRequirement>
                {
                    new LevelOverlayRequirement(301, 5)
                }
            };
        }
    }
}
