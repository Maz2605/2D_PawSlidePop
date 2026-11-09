using _PawSlidePopGame._Scripts.Feature.Match3.View;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Data
{
    public abstract class SpecialTileDefinitionSO : TileDefinitionSO
    {
        [Header("View")]
        [SerializeField] private Match3TileView tileViewPrefab;

        public override Match3TileView TileViewPrefab => tileViewPrefab;

        protected override void OnValidate()
        {
            base.OnValidate();
            MigrateLegacyTileViewPrefab(ref tileViewPrefab);
        }
    }
}

