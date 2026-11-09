using _PawSlidePopGame._Scripts.Feature.Match3.Core;
using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Booster;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Mechanics;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Overlays;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Tiles;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Logic.Factory
{
    public static class TileLogicFactory
    {
        public static ITileLogic CreateLogic(TileDefinitionSO definition)
        {
            if (definition == null)
            {
                return null;
            }

            switch (definition.LogicType)
            {
                case TileLogicType.BombBooster:
                    return new BombLogic();
                case TileLogicType.CrossBomb:
                    return new CrossBombLogic();
                case TileLogicType.SquareBomb:
                    return new SquareBombLogic();
                case TileLogicType.AreaBombMedium:
                    return new AreaBombMediumLogic();
                case TileLogicType.AreaBombLarge:
                    return new AreaBombLargeLogic();
                case TileLogicType.ChargedSweepBooster:
                    return new ChargedSweepBoosterLogic();
                case TileLogicType.BoardClearBomb:
                    return new BoardClearBombLogic();
                case TileLogicType.IceOverlay:
                    return new IceOverlayLogic();
                case TileLogicType.BubbleOverlay:
                    return new BubbleOverlayLogic();
                case TileLogicType.ChocolateOverlay:
                    return new ChocolateOverlayLogic();
                case TileLogicType.CakeTarget:
                    return new CakeDeliveryLogic();
                case TileLogicType.StoneBlocker:
                    return new StoneLogic();
                case TileLogicType.NormalAnimal:
                case TileLogicType.None:
                default:
                    return new NormalAnimalTileLogic();
            }
        }
    }
}

