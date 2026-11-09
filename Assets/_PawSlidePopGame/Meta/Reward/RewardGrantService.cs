using System.Collections.Generic;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Meta.Reward
{
    /// <summary>
    /// Dịch vụ trao thưởng dùng chung cho toàn game (Wheel, Shop, Star Rewards, v.v.).
    /// Hỗ trợ cơ chế Registry cho phép dễ dàng đăng ký thêm các loại phần thưởng mới.
    /// </summary>
    public static class RewardGrantService
    {
        private static readonly Dictionary<RewardKind, IRewardHandler> Handlers = new Dictionary<RewardKind, IRewardHandler>();

        static RewardGrantService()
        {
            // Tự động đăng ký các bộ xử lý mặc định
            RegisterHandler(new CoinsRewardHandler());
            RegisterHandler(new BoosterRewardHandler());
            RegisterHandler(new HeartRewardHandler());
            RegisterHandler(new InfiniteHeartRewardHandler());
        }

        /// <summary>Đăng ký thêm một trình xử lý phần thưởng mới (dùng để mở rộng).</summary>
        public static void RegisterHandler(IRewardHandler handler)
        {
            if (handler == null) return;
            Handlers[handler.Kind] = handler;
        }

        /// <summary>Hủy đăng ký một trình xử lý phần thưởng.</summary>
        public static void UnregisterHandler(RewardKind kind)
        {
            Handlers.Remove(kind);
        }

        /// <summary>Trao một phần thưởng đơn lẻ cho người chơi.</summary>
        /// <param name="reward">Asset cấu hình phần thưởng.</param>
        /// <param name="reason">Lý do trao thưởng (dùng cho analytics/log).</param>
        /// <returns>true nếu trao thành công.</returns>
        public static bool TryGrant(RewardEntrySO reward, string reason = "grant")
        {
            if (reward == null || reward.Amount <= 0)
            {
                return false;
            }

            if (Handlers.TryGetValue(reward.RewardKind, out var handler))
            {
                return handler.TryGrant(reward, reason);
            }

            Debug.LogWarning($"[RewardGrantService] Không tìm thấy Handler nào cho loại phần thưởng: {reward.RewardKind}");
            return false;
        }

        /// <summary>
        /// Trao nhiều phần thưởng cùng lúc (dùng cho gói đặc biệt / bundles).
        /// </summary>
        /// <param name="rewards">Danh sách asset phần thưởng.</param>
        /// <param name="reason">Lý do trao thưởng.</param>
        /// <returns>true nếu TẤT CẢ phần thưởng đều được trao thành công.</returns>
        public static bool TryGrantMultiple(IEnumerable<RewardEntrySO> rewards, string reason = "grant")
        {
            if (rewards == null)
            {
                return false;
            }

            bool allSucceeded = true;
            foreach (RewardEntrySO reward in rewards)
            {
                if (!TryGrant(reward, reason))
                {
                    allSucceeded = false;
                }
            }

            return allSucceeded;
        }
    }
}
