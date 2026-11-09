using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.View;
using UnityEngine;
using UnityEngine.Serialization;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Data
{
    public abstract class TileDefinitionSO : BoardContentDefinitionSO
    {
        public override BoardLayer ContentLayer => BoardLayer.Tile;
        public abstract TileKind TileKind { get; }
        public abstract TileLogicType LogicType { get; }
        public bool IsSpecialTile => TileKind != TileKind.Normal;
    }
}

