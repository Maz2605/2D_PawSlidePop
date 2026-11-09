using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Core.Audio;
using _PawSlidePopGame._Scripts.Core.Vibration;
using _PawSlidePopGame._Scripts.Feature.Meta.Shop;
using _PawSlidePopGame._Scripts.Gameplay.Meta.EconomyManager;
using _PawSlidePopGame._Scripts.UI.Base;
using _PawSlidePopGame._Scripts.UI.Manager;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.UI.Screens.SubScreens
{
    /// <summary>
    /// Màn hình con Shop – hiển thị danh sách vật phẩm và xử lý tương tác mua hàng.
    /// Hỗ trợ 2 cách bố trí UI:
    ///   1. staticItemViews: Kéo thả sẵn các ShopItemView trên scene (phù hợp layout cố định).
    ///   2. itemPrefab + contentRoot: Sinh động các ShopItemView từ prefab (phù hợp danh sách dài).
    /// </summary>
    public class ShopSubScreen : BaseSubScreen
    {
        [Header("Cấu Hình UI Động")]
        [Tooltip("Prefab cho gói vật phẩm thường (Normal).")]
        [SerializeField] private ShopItemView normalItemPrefab;
        [Tooltip("Prefab cho gói vật phẩm đặc biệt (Special).")]
        [SerializeField] private ShopItemView specialItemPrefab;

        [Tooltip("Nơi chứa các gói vật phẩm thường.")]
        [SerializeField] private Transform normalContentRoot;
        [Tooltip("Nơi chứa các gói vật phẩm đặc biệt.")]
        [SerializeField] private Transform specialContentRoot;

        [Tooltip("Text hiển thị số lượng vật phẩm trong Shop.")]
        [SerializeField] private TMPro.TMP_Text itemCountText;

        private readonly List<ShopItemSO> _items = new List<ShopItemSO>();

        private readonly List<ShopItemView> _activeViews = new List<ShopItemView>();

        // ───── Lifecycle ─────

        public override void Init()
        {
            if (isInitialized)
                return;

            base.Init();
            BindViews();
            SubscribeCoinChanges();
        }

        public override void Show()
        {
            base.Show();
            // Làm mới trạng thái nút mỗi lần mở Shop (Coin có thể đã thay đổi)
            RefreshAllAffordability();
        }

        public override void Hide()
        {
            base.Hide();
        }

        private void OnDestroy()
        {
            UnsubscribeCoinChanges();
            ShopService.OnItemPurchased  -= HandlePurchaseSuccess;
            ShopService.OnPurchaseFailed -= HandlePurchaseFailed;
        }

        // ───── Setup ─────

        private void BindViews()
        {
            // Dọn dẹp các ô cũ nếu có
            foreach (var view in _activeViews)
            {
                if (view != null)
                {
                    Destroy(view.gameObject);
                }
            }
            _activeViews.Clear();
            _items.Clear();

            // Tải tất cả các ShopItemSO từ Resources/Shop/Items
            ShopItemSO[] loadedItems = Resources.LoadAll<ShopItemSO>("Shop/Items");
            if (loadedItems != null)
            {
                _items.AddRange(loadedItems);
                // Sắp xếp: SortOrder tăng dần, sau đó sắp xếp theo tên ItemId (alphabet)
                _items.Sort((a, b) =>
                {
                    int cmp = a.SortOrder.CompareTo(b.SortOrder);
                    if (cmp == 0)
                    {
                        cmp = string.Compare(a.ItemId, b.ItemId, System.StringComparison.Ordinal);
                    }
                    return cmp;
                });
            }

            if (_items.Count == 0)
            {
                Debug.LogWarning("[ShopSubScreen] Không tìm thấy file ShopItemSO nào trong thư mục Resources/Shop/Items.", this);
            }

            if (itemCountText != null)
            {
                itemCountText.SetText(_items.Count.ToString());
            }

            // Sinh động các ô vật phẩm tương ứng
            foreach (ShopItemSO item in _items)
            {
                if (item == null)
                    continue;

                ShopItemView prefab = item.IsSpecial ? specialItemPrefab : normalItemPrefab;
                Transform parent = item.IsSpecial ? specialContentRoot : normalContentRoot;

                if (prefab == null || parent == null)
                {
                    Debug.LogWarning($"[ShopSubScreen] Thiếu prefab hoặc content root cho vật phẩm '{item.ItemId}' (IsSpecial: {item.IsSpecial}).", this);
                    continue;
                }

                ShopItemView view = Instantiate(prefab, parent);
                bool canAfford = CanPlayerAfford(item);
                view.Bind(item, HandleItemPurchaseClicked, canAfford);
                _activeViews.Add(view);
            }

            // Đăng ký lắng nghe kết quả mua hàng
            ShopService.OnItemPurchased  += HandlePurchaseSuccess;
            ShopService.OnPurchaseFailed += HandlePurchaseFailed;
        }

        // ───── Affordability ─────

        private void RefreshAllAffordability()
        {
            if (_items == null)
                return;

            for (int i = 0; i < _activeViews.Count && i < _items.Count; i++)
            {
                ShopItemSO item = _items[i];
                if (item == null)
                    continue;

                _activeViews[i]?.RefreshAffordability(CanPlayerAfford(item));
            }
        }

        private static bool CanPlayerAfford(ShopItemSO item)
        {
            return EconomyManager.Instance != null && EconomyManager.Instance.CanSpendCoins(item.PriceCoins);
        }

        // ───── Event Subscriptions ─────

        private void SubscribeCoinChanges()
        {
            if (EconomyManager.Instance != null)
                EconomyManager.Instance.OnCoinsChanged += HandleCoinsChanged;
        }

        private void UnsubscribeCoinChanges()
        {
            if (EconomyManager.Instance != null)
                EconomyManager.Instance.OnCoinsChanged -= HandleCoinsChanged;
        }

        private void HandleCoinsChanged(int previous, int current, string reason)
        {
            RefreshAllAffordability();
        }

        // ───── Purchase Handlers ─────

        private void HandleItemPurchaseClicked(ShopItemSO item)
        {
            ShopService.TryPurchaseItem(item);
        }

        private void HandlePurchaseSuccess(ShopItemSO item)
        {
            if (AudioController.Instance != null)
            {
                AudioController.Instance.PlayUISound(UISoundType.PurchaseSuccess);
            }
            if (VibrationManager.Instance != null)
            {
                VibrationManager.Instance.PlayMediumImpact();
            }

            UIManager.Instance?.ShowToast($"Successfully purchased: {item.DisplayName}!");
            RefreshAllAffordability();
        }

        private void HandlePurchaseFailed(ShopItemSO item, PurchaseFailReason reason)
        {
            if (AudioController.Instance != null)
            {
                AudioController.Instance.PlayUISound(UISoundType.ToastError);
            }
            if (VibrationManager.Instance != null)
            {
                VibrationManager.Instance.PlayReject();
            }

            string message = reason switch
            {
                PurchaseFailReason.InsufficientCoins    => "Not enough Coins!",
                PurchaseFailReason.InvalidRewardConfig  => "Invalid reward configuration. Please contact GD!",
                PurchaseFailReason.SystemError          => "System error. Please try again.",
                _                                       => "Cannot purchase this item."
            };

            UIManager.Instance?.ShowToast(message);
        }
    }
}