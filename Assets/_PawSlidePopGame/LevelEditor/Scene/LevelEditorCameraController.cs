using UnityEngine;
using UnityEngine.InputSystem;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.Scene
{
    [RequireComponent(typeof(Camera))]
    public sealed class LevelEditorCameraController : MonoBehaviour
    {
        [Header("Zoom Settings")]
        [SerializeField] private float minOrthoSize = 2f;
        [SerializeField] private float maxOrthoSize = 15f;
        [SerializeField] private float zoomSensitivity = 1f; // Orthographic size units per scroll notch

        [Header("Grid Layout Config")]
        [SerializeField] private float cellSpacingX = 1.1f;
        [SerializeField] private float cellSpacingY = 1.1f;

        [Header("Safe Margins (0-1 Normalized)")]
        [SerializeField] private float safeLeft = 0.38f; // Palette panels cover about 38%
        [SerializeField] private float safeRight = 0.97f; // 3% margin right
        [SerializeField] private float safeTop = 0.85f; // TopBar covers top
        [SerializeField] private float safeBottom = 0.12f; // BottomPanel covers bottom

        private Camera _camera;
        private Vector3 _lastDragPosition;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        private void Update()
        {
            HandleZoom();
            HandlePan();
        }

        private void HandleZoom()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            float scrollY = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scrollY) > 0.001f)
            {
                // In New Input System, scroll value is usually multiples of 120 per notch.
                float scrollNotches = scrollY / 120f;
                float newSize = _camera.orthographicSize - (scrollNotches * zoomSensitivity);
                _camera.orthographicSize = Mathf.Clamp(newSize, minOrthoSize, maxOrthoSize);
            }
        }

        private void HandlePan()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 mousePos = mouse.position.ReadValue();

            if (mouse.middleButton.wasPressedThisFrame)
            {
                _lastDragPosition = new Vector3(mousePos.x, mousePos.y, 0f);
            }
            else if (mouse.middleButton.isPressed)
            {
                Vector3 currentMousePos = new Vector3(mousePos.x, mousePos.y, 0f);
                Vector3 delta = currentMousePos - _lastDragPosition;
                
                // Convert screen delta to world delta
                float screenHeight = Screen.height;
                float worldHeight = 2f * _camera.orthographicSize;
                float worldWidth = worldHeight * _camera.aspect;

                float translationX = -(delta.x / Screen.width) * worldWidth;
                float translationY = -(delta.y / screenHeight) * worldHeight;

                transform.Translate(new Vector3(translationX, translationY, 0f), Space.World);
                _lastDragPosition = currentMousePos;
            }
        }

        public void AutoFit(int boardWidth, int boardHeight, Vector3 boardOffset)
        {
            if (_camera == null)
            {
                _camera = GetComponent<Camera>();
            }

            float worldBoardWidth = boardWidth * cellSpacingX;
            float worldBoardHeight = boardHeight * cellSpacingY;

            float aspect = _camera.aspect;

            float safeWidthFraction = safeRight - safeLeft;
            float safeHeightFraction = safeTop - safeBottom;

            float requiredOrthoHeight = (worldBoardHeight / safeHeightFraction) * 0.5f;
            float requiredOrthoWidth = (worldBoardWidth / safeWidthFraction) * 0.5f / aspect;

            float targetOrtho = Mathf.Clamp(Mathf.Max(requiredOrthoHeight, requiredOrthoWidth), minOrthoSize, maxOrthoSize);
            _camera.orthographicSize = targetOrtho;

            float safeCenterX = (safeLeft + safeRight) * 0.5f;
            float safeCenterY = (safeBottom + safeTop) * 0.5f;

            float shiftFractionX = safeCenterX - 0.5f;
            float shiftFractionY = safeCenterY - 0.5f;

            float worldShiftX = -shiftFractionX * (2f * targetOrtho * aspect);
            float worldShiftY = -shiftFractionY * (2f * targetOrtho);

            transform.position = new Vector3(worldShiftX + boardOffset.x, worldShiftY + boardOffset.y, -10f);
        }
    }
}
