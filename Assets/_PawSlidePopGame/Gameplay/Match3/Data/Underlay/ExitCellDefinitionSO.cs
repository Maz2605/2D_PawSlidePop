using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Data
{
    [CreateAssetMenu(fileName = "ExitCell", menuName = "_PawSlidePopGame/Match3/Underlay/Exit Cell")]
    public class ExitCellDefinitionSO : UnderlayDefinitionSO
    {
        public override UnderlayLogicType LogicType => UnderlayLogicType.ExitCell;
    }
}

