using _PawSlidePopGame._Scripts.Feature.Match3.Data;

namespace _PawSlidePopGame._Scripts.Data.LevelProvider
{
    public interface ILevelDataProvider
    {
        bool TryGetLevelData(string levelId, out Match3LevelData levelData);
        bool TryGetLevelDocument(string levelId, out LevelSaveData document);
    }
}
