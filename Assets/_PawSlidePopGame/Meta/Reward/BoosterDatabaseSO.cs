using System.Collections.Generic;
using UnityEngine;
using _PawSlidePopGame._Scripts.Feature.Match3.Boosters;

namespace _PawSlidePopGame._Scripts.Feature.Meta.Reward
{
    [CreateAssetMenu(fileName = "BoosterDatabase", menuName = "_PawSlidePopGame/Meta/Reward/Booster Database")]
    public sealed class BoosterDatabaseSO : ScriptableObject
    {
        [SerializeField] private List<BoosterDefinitionSO> boosters = new List<BoosterDefinitionSO>();

        public IReadOnlyList<BoosterDefinitionSO> Boosters => boosters;
    }
}
