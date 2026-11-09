using System.Collections.Generic;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Meta.Reward
{
    /// <summary>
    /// Milestone nhận thưởng bằng Sao (Star Reward).
    /// </summary>
    [CreateAssetMenu(fileName = "StarReward", menuName = "_PawSlidePopGame/Meta/Reward/Star Reward")]
    public sealed class StarRewardSO : ScriptableObject
    {
        [Header("Hiển Thị")]
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;

        [Header("Điều Kiện")]
        [SerializeField] private int requiredStars = 10;

        [Header("Phần Thưởng")]
        [Tooltip("Danh sách quà tặng người chơi nhận được khi nhận mốc này.")]
        [SerializeField] private List<RewardEntrySO> rewards = new List<RewardEntrySO>();

        public string RewardId => name;

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(displayName))
                    return displayName;

                return rewards is { Count: 1 } ? rewards[0].DisplayName : name;
            }
        }

        public Sprite Icon
        {
            get
            {
                if (icon != null)
                    return icon;

                return rewards is { Count: > 0 } ? rewards[0].Icon : null;
            }
        }

        public int RequiredStars => Mathf.Max(1, requiredStars);

        public IReadOnlyList<RewardEntrySO> Rewards => rewards;

        private void OnValidate()
        {
            requiredStars = Mathf.Max(1, requiredStars);
        }
    }
}
