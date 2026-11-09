using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Model.Entities
{
    public sealed class TileContentModel : TileModel
    {
        public TileContentModel(int instanceId, TileDefinitionSO definition)
            : base(instanceId, definition)
        {
        }

        public override BoardLayer Layer => BoardLayer.Tile;
    }
}

