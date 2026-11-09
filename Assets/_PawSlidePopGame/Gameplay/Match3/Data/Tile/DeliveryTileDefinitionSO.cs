using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Data
{
    [CreateAssetMenu(fileName = "DeliveryTile", menuName = "_PawSlidePopGame/Match3/Tiles/Delivery Tile")]
    public sealed class DeliveryTileDefinitionSO : TargetTileDefinitionSO
    {
        public override TileLogicType LogicType => TileLogicType.CakeTarget;
    }
}

