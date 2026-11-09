using UnityEngine;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.UI;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.Scene
{
    public sealed class LevelEditorSceneSetup : MonoBehaviour
    {
        [SerializeField] private LevelEditorRuntimeBridge runtimeBridge;
        [SerializeField] private LevelEditorSessionStateHolder sessionStateHolder;
        [SerializeField] private LevelEditorSelectionStateHolder selectionStateHolder;
        [SerializeField] private EditorUIManager editorUIManager;
        [SerializeField] private LevelEditorUIController applicationService;
        [SerializeField] private LevelEditorUIRootPresenter rootPresenter;

        private void Awake()
        {
            editorUIManager?.Init();
        }

        public LevelEditorRuntimeBridge RuntimeBridge => runtimeBridge;
        public LevelEditorSessionStateHolder SessionStateHolder => sessionStateHolder;
        public LevelEditorSelectionStateHolder SelectionStateHolder => selectionStateHolder;
        public EditorUIManager EditorUIManager => editorUIManager;
        public LevelEditorUIController ApplicationService => applicationService;
        public LevelEditorUIRootPresenter RootPresenter => rootPresenter;
    }
}
