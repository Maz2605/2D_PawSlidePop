using System;
using _PawSlidePopGame._Scripts.Feature.Meta.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.UI.Screens.SubScreens
{
    /// <summary>
    /// View component cho một ô vật phẩm trong Shop.
    /// Hỗ trợ hiển thị 2 loại: Vật phẩm thường (Normal) và Vật phẩm đặc biệt (Special).
    /// GD gán các trường UI từ Inspector, code tự bind dữ liệu.
    /// </summary>
    public sealed class ShopItemView : MonoBehaviour
    {
        [Header("Thông Tin Vật Phẩm")]
        [SerializeField] private Image itemIconImage;
        [SerializeField] private TMP_Text itemNameText;
        [SerializeField] private TMP_Text itemDescriptionText;

        [Header("Giá")]
        [SerializeField] private TMP_Text priceText;
        [Tooltip("Container chứa giá gốc bị gạch ngang. Bật/Tắt tùy theo HasDiscount.")]
        [SerializeField] private GameObject originalPriceContainer;
        [SerializeField] private TMP_Text originalPriceText;

        [Header("Nút Mua")]
        [SerializeField] private Button purchaseButton;
        [Tooltip("Object hiển thị khi không đủ tiền (ví dụ icon khoá, tô màu đỏ).")]
        [SerializeField] private GameObject notEnoughCoinsIndicator;

        [Header("Cấu Hình Màu Sắc Khi Thiếu Tiền")]
        [SerializeField] private Color normalPriceColor = Color.white;
        [SerializeField] private Color notEnoughCoinsPriceColor = Color.red;

        [Header("Badge Đặc Biệt (Special Item Only)")]
        [Tooltip("Badge nổi bật – chỉ hiển thị khi item.IsSpecial = true.")]
        [SerializeField] private GameObject specialBadge;
        [SerializeField] private TMP_Text specialBadgeText;

        private ShopItemSO _item;
        private Action<ShopItemSO> _onPurchaseClicked;

        // ───── Public API ─────

        /// <summary>Bind dữ liệu vật phẩm vào giao diện.</summary>
        /// <param name="item">Vật phẩm cần hiển thị.</param>
        /// <param name="onPurchaseClicked">Callback khi người chơi bấm nút Mua.</param>
        /// <param name="canAfford">true nếu người chơi đủ Coin để mua.</param>
        public void Bind(ShopItemSO item, Action<ShopItemSO> onPurchaseClicked, bool canAfford)
        {
            _item              = item;
            _onPurchaseClicked = onPurchaseClicked;

            if (item == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            // Icon
            if (itemIconImage != null)
            {
                itemIconImage.sprite  = item.Icon;
                itemIconImage.enabled = item.Icon != null;
            }

            // Tên & Mô tả
            if (itemNameText != null)
                itemNameText.SetText(item.DisplayName);

            if (itemDescriptionText != null)
                itemDescriptionText.SetText(item.Description);

            // Giá
            if (priceText != null)
                priceText.SetText("{0}", item.PriceCoins);

            // Giá gốc gạch ngang
            bool showDiscount = item.HasDiscount;
            if (originalPriceContainer != null)
                originalPriceContainer.SetActive(showDiscount);

            if (showDiscount && originalPriceText != null)
                originalPriceText.SetText("{0}", item.OriginalPriceCoins);

            // Badge đặc biệt
            bool showBadge = item.IsSpecial;
            if (specialBadge != null)
                specialBadge.SetActive(showBadge);

            if (showBadge && specialBadgeText != null)
                specialBadgeText.SetText(item.TagText);

            // Trạng thái nút mua
            RefreshAffordability(canAfford);

            // Gán sự kiện nút Mua
            if (purchaseButton != null)
            {
                purchaseButton.onClick.RemoveAllListeners();
                purchaseButton.onClick.AddListener(OnPurchaseButtonClicked);
            }
        }

        /// <summary>Cập nhật trạng thái nút mua khi số dư Coin thay đổi (không cần Bind lại toàn bộ).</summary>
        public void RefreshAffordability(bool canAfford)
        {
            // Nút mua luôn luôn hoạt động để người chơi có thể click nhận thông báo "Không đủ Coin"
            if (purchaseButton != null)
            {
                purchaseButton.interactable = true;
            }

            // Chỉ thay đổi màu chữ của giá tiền
            if (priceText != null)
            {
                priceText.color = canAfford ? normalPriceColor : notEnoughCoinsPriceColor;
            }

            if (notEnoughCoinsIndicator != null)
                notEnoughCoinsIndicator.SetActive(!canAfford);
        }

        // ───── Private ─────

        private void OnPurchaseButtonClicked()
        {
            _onPurchaseClicked?.Invoke(_item);
        }
    }
}
