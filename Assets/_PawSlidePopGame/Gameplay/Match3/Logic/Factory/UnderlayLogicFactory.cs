using _PawSlidePopGame._Scripts.Feature.Match3.Core.Interfaces;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Underlays;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Logic.Factory
{
    public static class UnderlayLogicFactory
    {
        public static IUnderlayLogic CreateLogic(UnderlayDefinitionSO definition)
        {
            if (definition == null)
            {
                return null;
            }

            switch (definition.LogicType)
            {
                case Core.Enum.UnderlayLogicType.LockCell:
                    return new LockCellLogic();
                case Core.Enum.UnderlayLogicType.ConveyorCell:
                    return new ConveyorCellLogic();
                case Core.Enum.UnderlayLogicType.ExitCell:
                    return new ExitCellLogic();
                case Core.Enum.UnderlayLogicType.SpawnGateCell:
                    return new SpawnGateCellLogic();
                default:
                    return null;
            }
        }
    }
}

