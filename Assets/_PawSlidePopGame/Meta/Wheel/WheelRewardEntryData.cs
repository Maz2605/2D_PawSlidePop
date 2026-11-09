using System;
using _PawSlidePopGame._Scripts.Feature.Match3.Boosters;
using _PawSlidePopGame._Scripts.Feature.Meta.Reward;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Meta.Wheel
{
    [Serializable]
    public sealed class WheelRewardEntryData
    {
        [SerializeField] private RewardEntrySO reward;
        [SerializeField] private int weight = 1;
        [SerializeField] private bool enabled = true;

        public RewardEntrySO Reward => reward;
        public string RewardId => reward != null ? reward.RewardId : string.Empty;
        public RewardKind RewardKind => reward != null ? reward.RewardKind : RewardKind.Coins;
        public int Amount => reward != null ? reward.Amount : 0;
        public int Weight => Mathf.Max(0, weight);
        public bool Enabled => enabled;
        public string DisplayName => reward != null ? reward.DisplayName : string.Empty;
        public Sprite Icon => reward != null ? reward.Icon : null;

        /// <summary>
        /// Tham chiếu trực tiếp đến BoosterDefinitionSO từ RewardEntrySO.
        /// Không cần tra cứu qua BoosterInventory hay nhập ID bằng tay.
        /// </summary>
        public BoosterDefinitionSO BoosterDefinition => reward != null ? reward.BoosterDefinition : null;
        public string BoosterId => reward != null ? reward.BoosterId : string.Empty;

        public bool CanSpin()
        {
            if (!enabled || Weight <= 0 || Amount <= 0 || reward == null)
            {
                return false;
            }

            return RewardKind != RewardKind.Booster || BoosterDefinition != null;
        }

        public void Sanitize()
        {
            weight = Mathf.Max(0, weight);
        }

#if UNITY_EDITOR
        public void EditorSetup(RewardEntrySO rewardEntry, int itemWeight, bool isEnabled)
        {
            reward  = rewardEntry;
            weight  = itemWeight;
            enabled = isEnabled;
        }
#endif
    }
}
