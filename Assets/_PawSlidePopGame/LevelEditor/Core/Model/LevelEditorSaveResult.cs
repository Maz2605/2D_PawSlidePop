using _PawSlidePopGame._Scripts.Data.LevelProvider;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Validation;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Model
{
    public sealed class LevelEditorSaveResult
    {
        public bool success;
        public string message;
        public string levelId;
        public LevelSaveData document;
        public Match3LevelDataValidationResult validationResult;
    }
}
