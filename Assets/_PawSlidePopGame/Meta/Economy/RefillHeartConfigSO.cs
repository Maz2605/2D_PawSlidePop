using UnityEngine;

namespace _PawSlidePopGame._Scripts.Gameplay.Meta.EconomyManager
{
    [CreateAssetMenu(fileName = "RefillHeartConfig", menuName = "_PawSlidePopGame/Meta/Economy/Refill Heart Config")]
    public sealed class RefillHeartConfigSO : ScriptableObject
    {
        [Header("Refill Settings")]
        [SerializeField] private int refillPriceCoins = 150;

        public int RefillPriceCoins => refillPriceCoins;
    }
}
