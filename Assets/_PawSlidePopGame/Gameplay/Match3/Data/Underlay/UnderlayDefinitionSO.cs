using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.View;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Data
{
    public abstract class UnderlayDefinitionSO : BoardContentDefinitionSO
    {
        [Header("View")]
        [SerializeField] private Match3TileView tileViewPrefab;

        public override Match3TileView TileViewPrefab => tileViewPrefab;
        public override BoardLayer ContentLayer => BoardLayer.Underlay;
        public override bool CanSpawnOnRefill => false;
        public override int SpawnWeight => 0;
        public override bool SupportsTargetObjective => false;
        public override string TargetObjectiveRestrictionReason => "Underlay objective targets must come from a destructible underlay definition.";
        public abstract UnderlayLogicType LogicType { get; }

        protected override void OnValidate()
        {
            base.OnValidate();
            MigrateLegacyTileViewPrefab(ref tileViewPrefab);
        }
    }
}

