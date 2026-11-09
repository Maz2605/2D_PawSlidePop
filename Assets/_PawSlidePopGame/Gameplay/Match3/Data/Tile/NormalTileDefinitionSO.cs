using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Data
{
    public abstract class NormalTileDefinitionSO : TileDefinitionSO
    {
        public override TileKind TileKind => TileKind.Normal;
        public override TileLogicType LogicType => TileLogicType.NormalAnimal;
    }
}
