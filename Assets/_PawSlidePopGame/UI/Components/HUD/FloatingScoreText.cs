using DG.Tweening;
using TMPro;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.UI.Components.HUD
{
    public class FloatingScoreText : MonoBehaviour
    {
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private float appearDuration = 0.25f;
        [SerializeField] private float flyDuration = 0.75f;
        [SerializeField] private Ease flyEase = Ease.InQuad;

        public void Initialize(Vector3 startWorldPos, Vector3 targetPosition, int scoreAmount, bool isTargetUI = true, System.Action onReachTarget = null)
        {
            if (scoreText != null)
            {
                scoreText.SetText($"+{scoreAmount}");
            }

            Canvas mainCanvas = GetComponentInParent<Canvas>();
            if (mainCanvas == null)
            {
                Destroy(gameObject);
                return;
            }

            RectTransform rect = transform as RectTransform;
            if (rect == null) return;

            // Chuyển vị trí start World sang Local trong Canvas
            Vector2 screenPos = Camera.main != null 
                ? (Vector2)Camera.main.WorldToScreenPoint(startWorldPos)
                : (Vector2)startWorldPos;

            Camera canvasCam = mainCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : (mainCanvas.worldCamera != null ? mainCanvas.worldCamera : Camera.main);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                mainCanvas.transform as RectTransform,
                screenPos,
                canvasCam,
                out Vector2 localPos);
            rect.anchoredPosition = localPos;

            // Chuyển vị trí target sang Local trong Canvas
            Vector2 targetScreenPos;
            if (isTargetUI)
            {
                if (mainCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    // Trong Overlay, transform.position của một UI element chính là tọa độ screen space (pixel) của nó.
                    // Không được chạy qua WorldToScreenPoint nữa vì sẽ bị tính sai qua Camera.main.
                    targetScreenPos = targetPosition; 
                }
                else
                {
                    targetScreenPos = RectTransformUtility.WorldToScreenPoint(mainCanvas.worldCamera != null ? mainCanvas.worldCamera : Camera.main, targetPosition);
                }
            }
            else
            {
                // Nếu target là World 3D
                targetScreenPos = Camera.main != null
                    ? (Vector2)Camera.main.WorldToScreenPoint(targetPosition)
                    : (Vector2)targetPosition;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                mainCanvas.transform as RectTransform,
                targetScreenPos,
                canvasCam,
                out Vector2 targetLocalPos);

            // Chạy Tween bay lên
            transform.localScale = Vector3.zero;

            Sequence seq = DOTween.Sequence();
            seq.Append(transform.DOScale(1.2f, appearDuration).SetEase(Ease.OutBack))
               .AppendInterval(0.05f)
               .Append(rect.DOAnchorPos(targetLocalPos, flyDuration).SetEase(flyEase))
               .Join(transform.DOScale(0.5f, flyDuration).SetEase(flyEase))
               .Join(scoreText != null ? scoreText.DOFade(0.1f, flyDuration).SetEase(flyEase) : null)
               .OnComplete(() => {
                   onReachTarget?.Invoke();
                   Destroy(gameObject);
               })
               .SetLink(gameObject);
        }
    }
}
