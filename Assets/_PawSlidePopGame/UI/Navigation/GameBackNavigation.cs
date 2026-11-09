using MazFramework.UI;
using UnityEngine;
using _PawSlidePopGame._Scripts.Feature.Match3.Flow;

namespace _PawSlidePopGame.UI.Navigation
{
    [DisallowMultipleComponent]
    public sealed class GameBackNavigation : MonoBehaviour
    {
        private UIBackInput _input;
        private void OnEnable() => Bind();
        private void LateUpdate() => Bind();
        private void Bind()
        {
            var input = UIManager.ExistingInstance != null ? UIManager.ExistingInstance.GetComponent<UIBackInput>() : null;
            if (input == _input) return;
            if (_input != null) _input.UnhandledBack -= HandleBack;
            _input = input;
            if (_input != null) _input.UnhandledBack += HandleBack;
        }
        private void HandleBack()
        {
            var flow = GameFlowManager.ExistingInstance;
            if (flow != null && flow.CanPauseGameplay) flow.PauseGameplay();
        }
        private void OnDisable()
        {
            if (_input != null) _input.UnhandledBack -= HandleBack;
            _input = null;
        }
    }
}
