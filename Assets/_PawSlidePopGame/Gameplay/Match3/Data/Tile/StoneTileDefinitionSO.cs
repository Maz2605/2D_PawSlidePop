using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Data
{
    [CreateAssetMenu(fileName = "StoneTile", menuName = "_PawSlidePopGame/Match3/Tiles/Stone Tile")]
    public sealed class StoneTileDefinitionSO : BlockerTileDefinitionSO
    {
        [Min(1)]
        [SerializeField] private int defaultHP = 1;

        public override TileLogicType LogicType => TileLogicType.StoneBlocker;
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

