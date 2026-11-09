using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.View;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Data
{
    [CreateAssetMenu(fileName = "BoosterTile", menuName = "_PawSlidePopGame/Match3/Tiles/Booster Tile")]
    public class BoosterTileDefinitionSO : TileDefinitionSO
    {
        [Header("View")]
        [SerializeField] private SpecialTileView tileViewPrefab;
        [Tooltip("Authoring label is shown from TileLogicType. Medium = Diamond, Large = 5x5.")]
        [SerializeField] private TileLogicType boosterLogicType = TileLogicType.BombBooster;

        public override Match3TileView TileViewPrefab => tileViewPrefab;
        public override TileKind TileKind => TileKind.Booster;
        public override TileLogicType LogicType => boosterLogicType;

        protected override void OnValidate()
        {
            base.OnValidate();
            MigrateLegacyTileViewPrefab(ref tileViewPrefab);

            if (boosterLogicType == TileLogicType.None || boosterLogicType == TileLogicType.NormalAnimal || boosterLogicType == TileLogicType.IceOverlay)
            {
                boosterLogicType = TileLogicType.BombBooster;
            }
        }
    }
}

