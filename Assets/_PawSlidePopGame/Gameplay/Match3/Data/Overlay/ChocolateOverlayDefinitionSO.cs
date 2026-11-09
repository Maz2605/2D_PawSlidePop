using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Data
{
    [CreateAssetMenu(fileName = "ChocolateOverlay", menuName = "_PawSlidePopGame/Match3/Overlay/Chocolate Overlay")]
    public sealed class ChocolateOverlayDefinitionSO : OverlayDefinitionSO
    {
        [Min(1)]
        [SerializeField] private int defaultHP = 1;
        [Header("Behavior")]
        [Min(0)]
        [SerializeField] private int growthPerTurn = 1;
        [Min(1)]
        [SerializeField] private int growthTurnInterval = 1;

        public override OverlayLogicType LogicType => OverlayLogicType.Chocolate;
        public override int DefaultHP => defaultHP;
        public int GrowthPerTurn => growthPerTurn;
        public int GrowthTurnInterval => growthTurnInterval;

        protected override void OnValidate()
        {
            base.OnValidate();

            if (defaultHP < 1)
            {
                defaultHP = 1;
            }

            if (growthPerTurn < 0)
            {
                growthPerTurn = 0;
            }

            if (growthTurnInterval < 1)
            {
                growthTurnInterval = 1;
            }
        }
    }
}

