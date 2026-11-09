using UnityEngine;

namespace MazFramework.UI
{
    /// <summary>Apply to a stretched content container under a full-screen, unrotated parent.</summary>
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(RectTransform))]
    public sealed class SafeArea : MonoBehaviour
    {
        private RectTransform _rect;
        private Rect _lastArea;
        private Vector2 _lastSize;
        private ScreenOrientation _lastOrientation;
        private bool _applied;

        private void OnEnable()
        {
            _rect = GetComponent<RectTransform>();
            Refresh();
        }

        // ExecuteAlways also previews the current Game View / Device Simulator in the Editor.
        private void LateUpdate() => Apply(false);
        private void OnApplicationFocus(bool focused)
        {
            if (focused) Refresh();
        }

        public void Refresh() => Apply(true);

        private void Apply(bool force)
        {
            if (_rect == null) _rect = GetComponent<RectTransform>();
            Rect area = Screen.safeArea;
            Vector2 size = new Vector2(Screen.width, Screen.height);
            if (_rect == null || !TryCalculateAnchors(area, size, out Vector2 min, out Vector2 max)) return;
            if (!force && _applied && area == _lastArea && size == _lastSize && Screen.orientation == _lastOrientation) return;

            _lastArea = area;
            _lastSize = size;
            _lastOrientation = Screen.orientation;
            _applied = true;

            // Avoid dirtying prefabs and triggering layout when the result is unchanged.
            if (_rect.anchorMin != min) _rect.anchorMin = min;
            if (_rect.anchorMax != max) _rect.anchorMax = max;
            if (_rect.offsetMin != Vector2.zero) _rect.offsetMin = Vector2.zero;
            if (_rect.offsetMax != Vector2.zero) _rect.offsetMax = Vector2.zero;
        }

        public static bool TryCalculateAnchors(Rect safeArea, Vector2 screenSize, out Vector2 min, out Vector2 max)
        {
            min = Vector2.zero;
            max = Vector2.one;
            if (!Finite(screenSize.x) || !Finite(screenSize.y) || screenSize.x <= 0f || screenSize.y <= 0f ||
                !Finite(safeArea.x) || !Finite(safeArea.y) || !Finite(safeArea.width) || !Finite(safeArea.height) ||
                safeArea.width <= 0f || safeArea.height <= 0f || !Finite(safeArea.xMax) || !Finite(safeArea.yMax)) return false;

            min = new Vector2(Mathf.Clamp01(safeArea.xMin / screenSize.x), Mathf.Clamp01(safeArea.yMin / screenSize.y));
            max = new Vector2(Mathf.Clamp01(safeArea.xMax / screenSize.x), Mathf.Clamp01(safeArea.yMax / screenSize.y));
            return max.x > min.x && max.y > min.y;
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
