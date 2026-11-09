using _PawSlidePopGame._Scripts.Data.Events.Payloads;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.UI.Components.HUD
{
    public class TargetItemView : MonoBehaviour
    {
        [Header("--- UI References ---")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Image shadowImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TMP_Text currentCountText;
        [SerializeField] private TMP_Text targetCountText;
        [SerializeField] private Image checkmarkImage; // Component ảnh dấu tích

        [Header("--- State Colors ---")]
        [SerializeField] private Color defaultBackgroundColor = Color.white;
        [SerializeField] private Color completedBackgroundColor = new Color(0.72f, 0.94f, 0.61f, 1f);

        [Header("--- Animation Settings ---")]
        [SerializeField] private float countDuration = 0.35f;
        [SerializeField] private float bounceScale = 1.3f;
        [SerializeField] private float bounceDuration = 0.3f;

        public int BoundTileId { get; private set; }
        public Sprite IconSprite => iconImage != null ? iconImage.sprite : null;
        public Vector3 IconWorldPosition => iconImage != null ? iconImage.transform.position : transform.position;
        public Vector2 IconSize => iconImage != null ? iconImage.rectTransform.sizeDelta : new Vector2(60f, 60f);

        private int _currentDisplayValue = -1;
        private Tween _textScaleTween;
        private Tween _checkmarkTween;

        public void SetData(TargetProgressData data)
        {
            if (data == null)
            {
                gameObject.SetActive(false);
                BoundTileId = 0;
                return;
            }

            gameObject.SetActive(true);
            BoundTileId = data.tileId;

            if (iconImage != null)
            {
                iconImage.sprite = data.icon;
                iconImage.enabled = data.icon != null;
            }

            if (shadowImage != null)
            {
                shadowImage.sprite = data.icon;
                shadowImage.enabled = data.icon != null;
            }

            // Khởi tạo text sử dụng Zero-GC format
            _currentDisplayValue = data.currentCount;
            if (currentCountText != null)
            {
                currentCountText.SetText("{0}", _currentDisplayValue);
                currentCountText.color = new Color(currentCountText.color.r, currentCountText.color.g, currentCountText.color.b, 1f); // Reset Alpha
            }

            if (targetCountText != null)
            {
                targetCountText.SetText("/{0}", data.requiredCount);
                targetCountText.color = new Color(targetCountText.color.r, targetCountText.color.g, targetCountText.color.b, 1f); // Reset Alpha
            }

            ApplyCompletedState(data.isCompleted, true);
        }

        public void ApplyCompletedState(bool isCompleted, bool instant)
        {
            if (backgroundImage != null)
            {
                Color targetColor = isCompleted ? completedBackgroundColor : defaultBackgroundColor;
                backgroundImage.DOKill();
                
                if (instant)
                {
                    backgroundImage.color = targetColor;
                }
                else
                {
                    backgroundImage.DOColor(targetColor, 0.2f).SetLink(gameObject, LinkBehaviour.KillOnDisable);
                }
            }

            // Xử lý bật/tắt và animation cho Checkmark
            if (checkmarkImage != null)
            {
                _checkmarkTween?.Kill();
                
                if (instant)
                {
                    checkmarkImage.gameObject.SetActive(isCompleted);
                    checkmarkImage.transform.localScale = Vector3.one;
                    
                    // Nếu đã hoàn thành từ đầu (vd: load lại save), ẩn text đi
                    if (isCompleted)
                    {
                        if (currentCountText != null) currentCountText.alpha = 0f;
                        if (targetCountText != null) targetCountText.alpha = 0f;
                    }
                }
                else if (isCompleted)
                {
                    checkmarkImage.gameObject.SetActive(true);
                    checkmarkImage.transform.localScale = Vector3.zero;
                    
                    // Nảy checkmark lên
                    _checkmarkTween = checkmarkImage.transform.DOScale(Vector3.one, 0.4f)
                        .SetEase(Ease.OutBack)
                        .SetDelay(0.1f) // Delay nhẹ để user kịp thấy text chạm mốc target
                        .SetLink(checkmarkImage.gameObject, LinkBehaviour.KillOnDisable);

                    // Fade out cụm text đếm số
                    if (currentCountText != null) currentCountText.DOFade(0f, 0.2f).SetLink(currentCountText.gameObject, LinkBehaviour.KillOnDisable);
                    if (targetCountText != null) targetCountText.DOFade(0f, 0.2f).SetLink(targetCountText.gameObject, LinkBehaviour.KillOnDisable);
                }
            }
        }

        public void PlayProgressFx(TargetProgressChangedPayload payload)
        {
            if (payload.TileId != BoundTileId)
            {
                return;
            }

            // 1. Chạy đếm số mượt & Nảy text
            if (currentCountText != null && _currentDisplayValue != payload.CurrentCount)
            {
                currentCountText.DOKill();
                _textScaleTween?.Kill();

                // Luồng chạy số
                DOTween.To(() => _currentDisplayValue, x =>
                {
                    _currentDisplayValue = x;
                    currentCountText.SetText("{0}", _currentDisplayValue);
                }, payload.CurrentCount, countDuration)
                .SetEase(Ease.OutQuad)
                .SetLink(currentCountText.gameObject, LinkBehaviour.KillOnDisable);

                // Luồng nảy Text
                Transform textTransform = currentCountText.transform;
                textTransform.localScale = Vector3.one;

                Sequence seq = DOTween.Sequence();
                seq.Append(textTransform.DOScale(bounceScale, bounceDuration * 0.4f).SetEase(Ease.OutQuad))
                   .Append(textTransform.DOScale(1f, bounceDuration * 0.6f).SetEase(Ease.OutBack));

                _textScaleTween = seq.SetLink(currentCountText.gameObject, LinkBehaviour.KillOnDisable);
            }

            // 2. Icon vật phẩm giật nhẹ (Squash & Stretch cơ bản) để báo hiệu va chạm
            if (iconImage != null)
            {
                iconImage.transform.DOKill();
                iconImage.transform.localScale = Vector3.one;
                iconImage.transform.DOPunchScale(new Vector3(0.1f, -0.1f, 0f), 0.25f, 4)
                    .SetLink(iconImage.gameObject, LinkBehaviour.KillOnDisable);
            }

            // 3. Kích hoạt trạng thái hoàn thành nếu vừa đạt mốc
            if (payload.JustCompleted)
            {
                ApplyCompletedState(true, false);
            }
        }
    }
}
