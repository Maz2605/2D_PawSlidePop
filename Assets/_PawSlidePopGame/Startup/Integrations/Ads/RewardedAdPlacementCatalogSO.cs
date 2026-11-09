using System.Collections.Generic;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Services.Ads
{
    [CreateAssetMenu(
        fileName = "RewardedAdPlacementCatalog",
        menuName = "_PawSlidePopGame/Ads/Rewarded Placement Catalog")]
    public sealed class RewardedAdPlacementCatalogSO : ScriptableObject
    {
        [SerializeField] private List<RewardedAdPlacementConfigSO> placements = new();

        public IReadOnlyList<RewardedAdPlacementConfigSO> Placements => placements;

        public bool TryGetConfig(RewardedAdPlacement placement, out RewardedAdPlacementConfigSO config)
        {
            for (int i = 0; i < placements.Count; i++)
            {
                RewardedAdPlacementConfigSO candidate = placements[i];
                if (candidate != null && candidate.Placement == placement)
                {
                    config = candidate;
                    return true;
                }
            }

            config = null;
            return false;
        }
    }
}
