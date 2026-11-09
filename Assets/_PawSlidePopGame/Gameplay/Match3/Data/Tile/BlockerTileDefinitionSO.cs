using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Data
{
    public abstract class BlockerTileDefinitionSO : SpecialTileDefinitionSO
    {
        public override TileKind TileKind => TileKind.Blocker;
        public override bool CanSpawnOnRefill => false;
        public override int SpawnWeight => 0;
    }
}
