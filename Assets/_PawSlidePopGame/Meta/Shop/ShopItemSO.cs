using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Feature.Meta.Reward;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Meta.Shop
{
    /// <summary>
    /// Cấu hình dữ liệu cho từng vật phẩm trong Shop.
    /// Game Designer chỉ cần tạo asset này và kéo thả các RewardEntrySO vào – không cần nhập ID thủ công.
    /// </summary>
    [CreateAssetMenu(fileName = "ShopItem", menuName = "_PawSlidePopGame/Meta/Shop/Shop Item")]
    public sealed class ShopItemSO : ScriptableObject
    {
        [Header("Hiển Thị")]
        [SerializeField] private string displayName;
        [SerializeField, TextArea(1, 3)] private string description;
        [SerializeField] private Sprite icon;

        [Header("Giá")]
        [SerializeField] private int priceCoins = 100;
        [Tooltip("Giá gốc trước giảm giá. Nếu lớn hơn priceCoins, sẽ hiển thị gạch ngang trên giao diện.")]
        [SerializeField] private int originalPriceCoins;

        [Header("Phần Thưởng")]
        [Tooltip("Danh sách quà tặng người chơi nhận được khi mua thành công. Hỗ trợ gói nhiều phần thưởng.")]
        [SerializeField] private List<RewardEntrySO> rewards = new List<RewardEntrySO>();

        [Tooltip("Thứ tự sắp xếp hiển thị trong Shop (thấp xếp trước).")]
        [SerializeField] private int sortOrder;
        [Tooltip("Vật phẩm đặc biệt sẽ có khung nền khác biệt và badge nổi bật hơn.")]
        [SerializeField] private bool isSpecial;
        [Tooltip("Nhãn text hiển thị trên badge (ví dụ: 'New items', 'Hot', '-20%'). Chỉ hiển thị khi isSpecial = true.")]
        [SerializeField] private string tagText;

        // ───── Properties ─────

        public int SortOrder => sortOrder;

        /// <summary>Mã ID tự động lấy từ tên file asset – không cần GD nhập tay.</summary>
        public string ItemId => name;

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(displayName))
                    return displayName;

                // Fallback: tự sinh tên nếu GD chưa điền
                return rewards is { Count: 1 } ? rewards[0].DisplayName : name;
            }
        }

        public string Description => description;

        public Sprite Icon
        {
            get
            {
                if (icon != null)
                    return icon;

                // Fallback: dùng icon từ phần thưởng đầu tiên
                return rewards is { Count: > 0 } ? rewards[0].Icon : null;
            }
        }

        public int PriceCoins => Mathf.Max(1, priceCoins);

        /// <summary>
        /// Giá gốc trước giảm giá. Nếu bằng 0 hoặc nhỏ hơn PriceCoins thì không hiển thị gạch ngang.
        /// </summary>
        public int OriginalPriceCoins => originalPriceCoins;

        /// <summary>true nếu có giảm giá để hiển thị giá gạch ngang.</summary>
        public bool HasDiscount => originalPriceCoins > PriceCoins;

        public IReadOnlyList<RewardEntrySO> Rewards => rewards;

        public bool IsSpecial => isSpecial;

        public string TagText => tagText;

        // ───── Validation ─────

        private void OnValidate()
        {
            priceCoins         = Mathf.Max(1, priceCoins);
            originalPriceCoins = Mathf.Max(0, originalPriceCoins);
        }
    }
}
