using UnityEngine;
using _PawSlidePopGame._Scripts.Feature.Match3.Boosters;

namespace _PawSlidePopGame._Scripts.Feature.Meta.Reward
{
    [CreateAssetMenu(fileName = "RewardEntry", menuName = "_PawSlidePopGame/Meta/Reward/Reward Entry")]
    public sealed class RewardEntrySO : ScriptableObject
    {
        [SerializeField] private RewardKind rewardKind = RewardKind.Coins;
        [SerializeField] private int amount = 1;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;

        [Header("Booster (chỉ dùng khi RewardKind = Booster)")]
        [SerializeField] private BoosterDefinitionSO boosterDefinition;

        public string RewardId => name;
        public RewardKind RewardKind => rewardKind;
        public int Amount => Mathf.Max(1, amount);

        /// <summary>
        /// ID của Booster được tự động lấy từ asset BoosterDefinitionSO.
        /// GD không cần nhập tay – chỉ cần kéo thả asset vào trường boosterDefinition.
        /// </summary>
        public string BoosterId => rewardKind == RewardKind.Booster && boosterDefinition != null
            ? boosterDefinition.BoosterId
            : null;

        /// <summary>Tham chiếu trực tiếp đến asset định nghĩa Booster (nếu có).</summary>
        public BoosterDefinitionSO BoosterDefinition => rewardKind == RewardKind.Booster ? boosterDefinition : null;

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(displayName))
                {
                    return displayName;
                }

                return BuildFallbackDisplayName();
            }
        }

        public Sprite Icon
        {
            get
            {
                if (icon != null)
                {
                    return icon;
                }

                if (rewardKind == RewardKind.Booster && boosterDefinition != null)
                {
                    return boosterDefinition.Icon;
                }

                return null;
            }
        }

        private string BuildFallbackDisplayName()
        {
            return rewardKind switch
            {
                RewardKind.Coins   => $"{Amount} Coins",
                RewardKind.Booster => GetBoosterDisplayNameWithAmount(),
                RewardKind.Heart   => $"{Amount} Heart",
                RewardKind.InfiniteHeart => $"{Amount} Min Infinite Hearts",
                _                  => $"{rewardKind} x{Amount}"
            };
        }

        private string GetBoosterDisplayNameWithAmount()
        {
            string boosterName = boosterDefinition != null ? boosterDefinition.DisplayName : "(Unknown Booster)";
            return $"{boosterName} x{Amount}";
        }

        private void OnValidate()
        {
            amount = Mathf.Max(1, amount);
        }

#if UNITY_EDITOR
        public void EditorSetValues(
            RewardKind kind,
            int amt,
            BoosterDefinitionSO boosterDef = null,
            string nameText = null,
            Sprite customIcon = null)
        {
            rewardKind        = kind;
            amount            = amt;
            boosterDefinition = boosterDef;
            displayName       = nameText;
            icon              = customIcon;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
