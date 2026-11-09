using UnityEngine;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.Utils.UI
{
    [RequireComponent(typeof(CanvasScaler))]
    [ExecuteAlways] 
    public class OptimalCanvasScaler : MonoBehaviour
    {
        private CanvasScaler _canvasScaler;
    
        // Cache lại resolution để tránh tính toán lại không cần thiết
        private float _lastScreenWidth = -1f;
        private float _lastScreenHeight = -1f;

        private void Awake()
        {
            _canvasScaler = GetComponent<CanvasScaler>();
            UpdateCanvasScale();
        }

        /// <summary>
        /// Hàm này được Unity gọi tự động khi RectTransform thay đổi kích thước.
        /// Nó sẽ bắt được event khi người chơi xoay màn hình (Orientation thay đổi) 
        /// hoặc khi bật chế độ Split-screen trên Android.
        /// Không dùng Update() để tối ưu CPU.
        /// </summary>
        private void OnRectTransformDimensionsChange()
        {
            UpdateCanvasScale();
        }

#if UNITY_EDITOR
        // Trong Editor, đôi khi OnRectTransformDimensionsChange không bắt được ngay lúc bạn chọn 
        // resolution từ dropdown, nên tôi bọc Update() chỉ cho Editor để preview mượt mà.
        private void Update()
        {
            if (!Application.isPlaying)
            {
                UpdateCanvasScale();
            }
        }
#endif

        private void UpdateCanvasScale()
        {
            if (_canvasScaler == null || _canvasScaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
                return;

            float currentWidth = Screen.width;
            float currentHeight = Screen.height;

            // Tránh lỗi chia cho 0 hoặc khi chưa khởi tạo xong
            if (currentWidth <= 0 || currentHeight <= 0) return;

            // Nếu kích thước không đổi, bỏ qua tính toán 
            if (Mathf.Approximately(currentWidth, _lastScreenWidth) && 
                Mathf.Approximately(currentHeight, _lastScreenHeight))
            {
                return;
            }

            _lastScreenWidth = currentWidth;
            _lastScreenHeight = currentHeight;

            Vector2 refResolution = _canvasScaler.referenceResolution;
        
            float refRatio = refResolution.x / refResolution.y;
            float currentRatio = currentWidth / currentHeight;

            // CORE LOGIC:
            if (currentRatio > refRatio)
            {
                _canvasScaler.matchWidthOrHeight = 1f;
            }
            else
            {
                _canvasScaler.matchWidthOrHeight = 0f;
            }
        }
    }
}