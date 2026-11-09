using _PawSlidePopGame._Scripts.Feature.LevelEditor.Scene;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.UI
{
    public sealed class LevelEditorMainScreen : EditorScreen
    {
        [SerializeField] private LevelEditorSceneSetup sceneSetup;
        [SerializeField] private LevelEditorUIRootPresenter rootPresenter;

        protected override void OnShown()
        {
            rootPresenter?.Initialize(sceneSetup);
            rootPresenter?.RefreshNow();
        }
    }
}
