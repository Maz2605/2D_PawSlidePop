using _PawSlidePopGame._Scripts.Data.LevelProvider;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Validation;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Contracts
{
    public sealed class LevelGenerationResult
    {
        public bool success;
        public string message;
        public Match3LevelData levelData;
        public LevelSaveData document;
        public Match3LevelDataValidationResult validationResult;
        public int placedIceCount;
        public bool hasInitialMatches;
    }
}
