using UnityEngine;

using _PawSlidePopGame._Scripts.Feature.LevelEditor.Scene;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.UI.Panels;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.UI
{
    public sealed class LevelEditorUIRootPresenter : MonoBehaviour
    {
        [SerializeField] private LevelEditorSceneSetup sceneSetup;
        [SerializeField] private LevelEditorUIController applicationService;
        [SerializeField] private LevelEditorToolbarPresenter toolbarPresenter;
        [SerializeField] private LevelEditorPalettePresenter palettePresenter;
        [SerializeField] private LevelEditorBoardPresenter boardPresenter;
        [SerializeField] private LevelEditorSelectionPresenter selectionPresenter;
        [SerializeField] private LevelEditorRightActionPresenter rightActionPresenter;
        [SerializeField] private LevelEditorGoalsPresenter goalsPresenter;
        [SerializeField] private LevelEditorStatusPresenter statusPresenter;
        
        private bool _isInitialized;

        public void Initialize(LevelEditorSceneSetup setup)
        {
            if (setup != null)
            {
                sceneSetup = setup;
            }

            if (applicationService == null && sceneSetup != null)
            {
                applicationService = sceneSetup.ApplicationService;
            }

            if (applicationService == null || sceneSetup == null)
            {
                return;
            }

            applicationService.Configure(sceneSetup.RuntimeBridge, sceneSetup.SessionStateHolder, sceneSetup.SelectionStateHolder);

            if (_isInitialized)
            {
                return;
            }

            BindPresenters();
            applicationService.Changed += Refresh;
            _isInitialized = true;
        }

        public void RefreshNow()
        {
            Refresh();
        }

        private void OnDestroy()
        {
            if (_isInitialized && applicationService != null)
            {
                applicationService.Changed -= Refresh;
            }
        }

        private void BindPresenters()
        {
            toolbarPresenter?.Bind(applicationService);
            palettePresenter?.Bind(applicationService);
            boardPresenter?.Bind(applicationService);
            selectionPresenter?.Bind(applicationService);
            rightActionPresenter?.Bind(applicationService);
            goalsPresenter?.gameObject.SetActive(false);
        }

        private void Refresh()
        {
            toolbarPresenter?.Refresh();
            palettePresenter?.Refresh();
            boardPresenter?.Refresh();
            selectionPresenter?.Refresh();
            rightActionPresenter?.Refresh();
            statusPresenter?.Refresh(applicationService);
        }
    }
}
