using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Contracts;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Model
{
    [System.Serializable]
    public sealed class LevelEditorMetadata
    {
        public string levelId = "Level_001";
        public int displayLevelNumber = 1;
        public int movesLimit = 26;
        public List<int> spawnableTileIds = new List<int>();
        public List<LevelTargetRequirement> targets = new List<LevelTargetRequirement>();
    }
}
