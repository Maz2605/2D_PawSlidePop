using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Data
{
    [CreateAssetMenu(fileName = "LockCell", menuName = "_PawSlidePopGame/Match3/Underlay/Lock Cell")]
    public class LockCellDefinitionSO : UnderlayDefinitionSO
    {
        [Min(1)]
        [SerializeField] private int defaultHP = 1;

        public override int DefaultHP => defaultHP;
        public override UnderlayLogicType LogicType => UnderlayLogicType.LockCell;
        public override bool SupportsTargetObjective => true;
    }
}

