using _PawSlidePopGame._Scripts.Feature.Match3.Data;

namespace _PawSlidePopGame._Scripts.Data.LevelProvider
{
    [System.Serializable]
    public class LevelSaveData
    {
        public string levelId;
        public int schemaVersion = 1;
        public Match3LevelData levelData = new Match3LevelData();
    }
}
