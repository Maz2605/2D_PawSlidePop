using _PawSlidePopGame._Scripts.Data.LevelProvider;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Model
{
    public sealed class LevelEditorLoadResult
    {
        public bool success;
        public string message;
        public string levelId;
        public LevelSaveData document;
        public LevelEditorSessionContext session;
    }
}
