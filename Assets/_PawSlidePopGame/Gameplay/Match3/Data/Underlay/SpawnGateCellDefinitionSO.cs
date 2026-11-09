using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Data
{
    [CreateAssetMenu(fileName = "SpawnGateCell", menuName = "_PawSlidePopGame/Match3/Underlay/Spawn Gate Cell")]
    public class SpawnGateCellDefinitionSO : UnderlayDefinitionSO
    {
        public override UnderlayLogicType LogicType => UnderlayLogicType.SpawnGateCell;
    }
}

