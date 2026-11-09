using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Data
{
    [CreateAssetMenu(fileName = "ConveyorCell", menuName = "_PawSlidePopGame/Match3/Underlay/Conveyor Cell")]
    public class ConveyorCellDefinitionSO : UnderlayDefinitionSO
    {
        [SerializeField] private LineSlideDirection direction = LineSlideDirection.Right;

        public LineSlideDirection Direction => direction;
        public override UnderlayLogicType LogicType => UnderlayLogicType.ConveyorCell;
    }
}

