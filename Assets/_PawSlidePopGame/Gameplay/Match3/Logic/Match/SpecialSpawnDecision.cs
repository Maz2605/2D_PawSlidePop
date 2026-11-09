using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Logic.Match
{
    public sealed class SpecialSpawnDecision
    {
        public int GroupId { get; }
        public TileLogicType LogicType { get; }
        public CellModel SpawnCell { get; }

        public SpecialSpawnDecision(int groupId, TileLogicType logicType, CellModel spawnCell)
        {
            GroupId = groupId;
            LogicType = logicType;
            SpawnCell = spawnCell;
        }
    }
}

