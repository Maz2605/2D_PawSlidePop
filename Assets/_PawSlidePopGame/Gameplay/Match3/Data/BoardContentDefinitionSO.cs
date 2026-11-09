using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.View;
using UnityEngine;
using UnityEngine.Serialization;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Data
{
    public abstract class BoardContentDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        [Min(1)]
        [SerializeField] private int tileId = 1;

        [Header("Gameplay")]
        [SerializeField] private bool canSpawnOnRefill = true;
        [Min(0)]
        [SerializeField] private int spawnWeight = 1;

        [Header("View")]
        [FormerlySerializedAs("tileViewPrefab")]
        [SerializeField, HideInInspector] private Match3TileView legacyTileViewPrefab;
        [SerializeField] private Sprite icon;

        public int TileId => tileId;
        public virtual bool CanSpawnOnRefill => canSpawnOnRefill;
        public virtual int SpawnWeight => spawnWeight;
        public virtual Match3TileView TileViewPrefab => legacyTileViewPrefab;
        public Sprite Icon => icon;
        public virtual int DefaultHP => 1;
        public virtual bool AllowsOverlayPlacement => false;
        public virtual bool SupportsTargetObjective => ContentLayer != BoardLayer.Underlay;
        public virtual string TargetObjectiveRestrictionReason => null;
        public abstract BoardLayer ContentLayer { get; }

        protected virtual void OnValidate()
        {
            if (tileId < 1)
            {
                tileId = 1;
            }

            if (spawnWeight < 0)
            {
                spawnWeight = 0;
            }
        }

        protected void MigrateLegacyTileViewPrefab<TView>(ref TView typedPrefab) where TView : Match3TileView
        {
            if (typedPrefab == null && legacyTileViewPrefab is TView typedLegacyPrefab)
            {
                typedPrefab = typedLegacyPrefab;
            }
        }
    }
}

