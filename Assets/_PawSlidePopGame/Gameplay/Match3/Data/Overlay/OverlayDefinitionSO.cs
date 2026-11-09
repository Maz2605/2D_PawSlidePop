using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.View;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Data
{
    public abstract class OverlayDefinitionSO : BoardContentDefinitionSO
    {
        [Header("View")]
        [SerializeField] private Match3TileView tileViewPrefab;

        public override Match3TileView TileViewPrefab => tileViewPrefab;
        public override BoardLayer ContentLayer => BoardLayer.Overlay;
        public override bool CanSpawnOnRefill => false;
        public override int SpawnWeight => 0;
        public override bool AllowsOverlayPlacement => true;
        public abstract OverlayLogicType LogicType { get; }

        protected override void OnValidate()
        {
            base.OnValidate();
            MigrateLegacyTileViewPrefab(ref tileViewPrefab);
        }
    }
}

