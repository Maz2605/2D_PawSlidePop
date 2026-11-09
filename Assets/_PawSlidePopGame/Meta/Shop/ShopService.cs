using System;
using _PawSlidePopGame._Scripts.Feature.Meta.Reward;
using _PawSlidePopGame._Scripts.Gameplay.Meta.EconomyManager;
using _PawSlidePopGame._Scripts.Services.Analytics;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Meta.Shop
{
    /// <summary>
    /// Dịch vụ xử lý logic mua hàng trong Shop.
    /// Các thành phần UI gọi TryPurchaseItem() và lắng nghe OnItemPurchased / OnPurchaseFailed.
    /// </summary>
    public static class ShopService
    {
        /// <summary>Phát khi một vật phẩm được mua thành công. Tham số: item đã mua.</summary>
        public static event Action<ShopItemSO> OnItemPurchased;

        /// <summary>Phát khi mua thất bại. Tham số: (item, lý do thất bại).</summary>
        public static event Action<ShopItemSO, PurchaseFailReason> OnPurchaseFailed;

        /// <summary>Thực hiện giao dịch mua vật phẩm.</summary>
        /// <param name="item">Vật phẩm muốn mua.</param>
        /// <returns>true nếu mua thành công.</returns>
        public static bool TryPurchaseItem(ShopItemSO item)
        {
            if (item == null)
            {
                Debug.LogWarning("[ShopService] TryPurchaseItem được gọi với item = null.");
                return false;
            }

            // ── Kiểm tra số dư Coin ──
            if (EconomyManager.Instance == null)
            {
                Debug.LogWarning("[ShopService] EconomyManager.Instance là null.");
                OnPurchaseFailed?.Invoke(item, PurchaseFailReason.SystemError);
                return false;
            }

            if (!EconomyManager.Instance.CanSpendCoins(item.PriceCoins))
            {
                Debug.Log($"[ShopService] Không đủ Coin để mua '{item.ItemId}'. Cần {item.PriceCoins}, hiện có {EconomyManager.Instance.Coins}.");
                OnPurchaseFailed?.Invoke(item, PurchaseFailReason.InsufficientCoins);
                return false;
            }

            // ── Kiểm tra phần thưởng hợp lệ ──
            if (item.Rewards == null || item.Rewards.Count == 0)
            {
                Debug.LogWarning($"[ShopService] Vật phẩm '{item.ItemId}' không có phần thưởng nào được cấu hình.");
                OnPurchaseFailed?.Invoke(item, PurchaseFailReason.InvalidRewardConfig);
                return false;
            }

            // ── Trừ Coin ──
            if (!EconomyManager.Instance.TrySpendCoins(item.PriceCoins, $"shop_purchase_{item.ItemId}"))
            {
                OnPurchaseFailed?.Invoke(item, PurchaseFailReason.InsufficientCoins);
                return false;
            }

            // ── Trao thưởng ──
            bool allGranted = RewardGrantService.TryGrantMultiple(item.Rewards, $"shop_purchase_{item.ItemId}");
            if (!allGranted)
            {
                // Một số phần thưởng trao thất bại nhưng Coin đã bị trừ → log warning để GD biết
                Debug.LogWarning($"[ShopService] Một hoặc nhiều phần thưởng trong '{item.ItemId}' không thể trao. Kiểm tra cấu hình RewardEntrySO.");
            }

            Debug.Log($"[ShopService] Mua thành công: '{item.ItemId}', giá {item.PriceCoins} Coin.");

            // ── Log Firebase for Booster Purchases ──
            if (item.Rewards != null)
            {
                for (int i = 0; i < item.Rewards.Count; i++)
                {
                    RewardEntrySO reward = item.Rewards[i];
                    if (reward != null && reward.RewardKind == RewardKind.Booster && reward.BoosterDefinition != null)
                    {
                        FirebaseService.LogBoosterPurchased(
                            reward.BoosterDefinition.BoosterId,
                            item.PriceCoins,
                            reward.Amount);
                    }
                }
            }

            OnItemPurchased?.Invoke(item);
            return true;
        }
    }

    /// <summary>Lý do thất bại của một giao dịch mua hàng.</summary>
    public enum PurchaseFailReason
    {
        InsufficientCoins,
        InvalidRewardConfig,
        SystemError
    }
}
