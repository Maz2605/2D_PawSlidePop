using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MazFramework.UI
{
    [DisallowMultipleComponent]
    public sealed class UIBackInput : MonoBehaviour
    {
        private InputAction _back;
        private int _lastFrame = -1;
        public event Action UnhandledBack;
        private void OnEnable()
        {
            if (_back == null)
            {
                _back = new InputAction("UI Back", InputActionType.Button, "<Keyboard>/escape");
                _back.performed += HandleBack;
            }
            _back.Enable();
        }
        private void HandleBack(InputAction.CallbackContext context)
        {
            if (_lastFrame == Time.frameCount) return;
            _lastFrame = Time.frameCount;
            var manager = GetComponent<UIManager>();
            if (manager != null && !manager.TryHandleBack()) UnhandledBack?.Invoke();
        }
        private void OnDisable() => _back?.Disable();
        private void OnDestroy()
        {
            if (_back == null) return;
            _back.performed -= HandleBack;
            _back.Dispose();
        }
    }
}
