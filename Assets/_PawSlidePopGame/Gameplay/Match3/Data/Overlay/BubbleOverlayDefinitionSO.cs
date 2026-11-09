using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Data
{
    [CreateAssetMenu(fileName = "BubbleOverlay", menuName = "_PawSlidePopGame/Match3/Overlay/Bubble Overlay")]
    public sealed class BubbleOverlayDefinitionSO : OverlayDefinitionSO
    {
        [Min(1)]
        [SerializeField] private int defaultHP = 1;

        public override OverlayLogicType LogicType => OverlayLogicType.Bubble;
        public override int DefaultHP => defaultHP;

        protected override void OnValidate()
        {
            base.OnValidate();

            if (defaultHP < 1)
            {
                defaultHP = 1;
            }
        }
    }
}

