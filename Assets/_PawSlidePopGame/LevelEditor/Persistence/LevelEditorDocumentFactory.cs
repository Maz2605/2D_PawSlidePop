using _PawSlidePopGame._Scripts.Data.LevelProvider;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.Persistence
{
    public static class LevelEditorDocumentFactory
    {
        public static LevelSaveData Create(Match3LevelData levelData, int schemaVersion = 1)
        {
            return new LevelSaveData
            {
                levelId = levelData?.levelID ?? string.Empty,
                schemaVersion = schemaVersion < 1 ? 1 : schemaVersion,
                levelData = levelData ?? new Match3LevelData()
            };
        }
    }
}
