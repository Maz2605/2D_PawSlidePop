using System.Collections.Generic;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Contracts
{
    [System.Serializable]
    public sealed class LevelGenerationRequest
    {
        public string levelId = "Level_001";
        public int displayLevelNumber = 1;
        public int width = 8;
        public int height = 8;
        public int movesLimit = 26;
        public bool requireNoInitialMatches = true;
        public bool requireAllAllowedSpawnableAppearAtLeastOnce = true;
        public List<int> allowedSpawnableTileIds = new List<int>();
        public List<int> disallowedInitialTileIds = new List<int>();
        public List<LevelTargetRequirement> targets = new List<LevelTargetRequirement>();
        public List<LevelOverlayRequirement> requiredOverlayPlacements = new List<LevelOverlayRequirement>();
    }
}
