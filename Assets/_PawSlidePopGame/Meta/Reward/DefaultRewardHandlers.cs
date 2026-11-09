using _PawSlidePopGame._Scripts.Gameplay.Meta.EconomyManager;
using _PawSlidePopGame._Scripts.Gameplay.Meta.Inventory;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Meta.Reward
{
    /// <summary>
    /// Bộ xử lý trao thưởng Coin.
    /// </summary>
    public sealed class CoinsRewardHandler : IRewardHandler
    {
        public RewardKind Kind => RewardKind.Coins;

        public bool TryGrant(RewardEntrySO reward, string reason)
        {
            if (EconomyManager.Instance == null)
            {
                Debug.LogWarning("[CoinsRewardHandler] EconomyManager.Instance is null.");
                return false;
            }
            EconomyManager.Instance.AddCoins(reward.Amount, reason);
            return true;
        }
    }

    /// <summary>
    /// Bộ xử lý trao thưởng Booster.
    /// </summary>
    public sealed class BoosterRewardHandler : IRewardHandler
    {
        public RewardKind Kind => RewardKind.Booster;

        public bool TryGrant(RewardEntrySO reward, string reason)
        {
            if (reward.BoosterDefinition == null)
            {
                Debug.LogWarning($"[BoosterRewardHandler] Gói '{reward.RewardId}' loại Booster nhưng thiếu BoosterDefinitionSO.");
                return false;
            }
            if (BoosterInventory.Instance == null)
            {
                Debug.LogWarning("[BoosterRewardHandler] BoosterInventory.Instance is null.");
                return false;
            }
            BoosterInventory.Instance.AddBooster(reward.BoosterDefinition, reward.Amount, reason);
            return true;
        }
    }

    /// <summary>
    /// Bộ xử lý trao thưởng Heart (Mạng chơi).
    /// </summary>
    public sealed class HeartRewardHandler : IRewardHandler
    {
        public RewardKind Kind => RewardKind.Heart;

        public bool TryGrant(RewardEntrySO reward, string reason)
        {
            if (HeartManager.Instance == null)
            {
                Debug.LogWarning("[HeartRewardHandler] HeartManager.Instance is null.");
                return false;
            }
            HeartManager.Instance.AddHearts(reward.Amount);
            return true;
        }
    }

    /// <summary>
    /// Bộ xử lý trao thưởng vô hạn mạng (Infinite Hearts).
    /// </summary>
    public sealed class InfiniteHeartRewardHandler : IRewardHandler
    {
        public RewardKind Kind => RewardKind.InfiniteHeart;

        public bool TryGrant(RewardEntrySO reward, string reason)
        {
            if (HeartManager.Instance == null)
            {
                Debug.LogWarning("[InfiniteHeartRewardHandler] HeartManager.Instance is null.");
                return false;
            }

            // reward.Amount tính bằng phút vô hạn mạng. Quy đổi sang giây để cộng.
            HeartManager.Instance.AddInfiniteHearts(reward.Amount * 60);
            return true;
        }
    }
}
