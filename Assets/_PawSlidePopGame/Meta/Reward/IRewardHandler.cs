namespace _PawSlidePopGame._Scripts.Feature.Meta.Reward
{
    /// <summary>
    /// Giao diện cho trình xử lý trao thưởng.
    /// Giúp hệ thống dễ dàng mở rộng thêm các loại phần thưởng mới (NoAds, VIP, Skins, ...) mà không cần sửa code cốt lõi.
    /// </summary>
    public interface IRewardHandler
    {
        /// <summary>Loại phần thưởng mà trình xử lý này phụ trách.</summary>
        RewardKind Kind { get; }

        /// <summary>Thực hiện trao thưởng cho người chơi.</summary>
        /// <param name="reward">Asset chứa thông tin phần thưởng.</param>
        /// <param name="reason">Lý do trao thưởng (để log/analytics).</param>
        /// <returns>true nếu trao thành công.</returns>
        bool TryGrant(RewardEntrySO reward, string reason);
    }
}
