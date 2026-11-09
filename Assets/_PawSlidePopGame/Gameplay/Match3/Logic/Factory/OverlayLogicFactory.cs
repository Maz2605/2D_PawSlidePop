using _PawSlidePopGame._Scripts.Feature.Match3.Core;
using _PawSlidePopGame._Scripts.Feature.Match3.Core.Interfaces;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Overlays;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Logic.Factory
{
    public static class OverlayLogicFactory
    {
        public static IOverlayLogic CreateLogic(OverlayDefinitionSO definition)
        {
            return definition != null
                ? CreateLogicFromOverlayType(definition.LogicType)
                : null;
        }

        public static IOverlayLogic CreateLogic(TileDefinitionSO definition)
        {
            return definition != null
                ? CreateLogic(definition.LogicType)
                : null;
        }

        private static IOverlayLogic CreateLogic(Core.Enum.TileLogicType logicType)
        {
            switch (logicType)
            {
                case Core.Enum.TileLogicType.IceOverlay:
                    return new OverlayLogicAdapter(new IceOverlayLogic(), true, true);
                case Core.Enum.TileLogicType.BubbleOverlay:
                    return new OverlayLogicAdapter(new BubbleOverlayLogic(), false, false);
                case Core.Enum.TileLogicType.ChocolateOverlay:
                    return new OverlayLogicAdapter(new ChocolateOverlayLogic(), true, true);
                default:
                    return null;
            }
        }

        private static IOverlayLogic CreateLogicFromOverlayType(Core.Enum.OverlayLogicType logicType)
        {
            switch (logicType)
            {
                case Core.Enum.OverlayLogicType.Ice:
                    return new OverlayLogicAdapter(new IceOverlayLogic(), true, true);
                case Core.Enum.OverlayLogicType.Bubble:
                    return new OverlayLogicAdapter(new BubbleOverlayLogic(), false, false);
                case Core.Enum.OverlayLogicType.Chocolate:
                    return new OverlayLogicAdapter(new ChocolateOverlayLogic(), true, true);
                default:
                    return null;
            }
        }

        private sealed class OverlayLogicAdapter : IOverlayLogic
        {
            private readonly ITileLogic _inner;
            private readonly bool _blocksMatch;
            private readonly bool _blocksExplosionToTile;
            public OverlayLogicAdapter(ITileLogic inner, bool blocksMatch, bool blocksExplosionToTile)
            {
                _inner = inner;
                _blocksMatch = blocksMatch;
                _blocksExplosionToTile = blocksExplosionToTile;
            }
            public bool CanBeMoved() => _inner == null || _inner.CanBeMoved();
            public bool CanFall() => _inner == null || _inner.CanFall();
            public bool BlocksTileBelow() => _inner != null && _inner.BlocksTileBelow();
            public bool CanTileEnter(Model.Board.BoardModel board, Model.Board.CellModel cell, Model.Entities.TileModel tile) => true;
            public bool CanTileExit(Model.Board.BoardModel board, Model.Board.CellModel cell, Model.Entities.TileModel tile) => true;
            public bool LocksLine(Model.Board.BoardModel board, Model.Board.CellModel cell, Core.Enum.MoveAxis axis) => false;
            public bool BlocksMatch(Model.Board.BoardModel board, Model.Board.CellModel cell, Model.Entities.TileModel tile) => _blocksMatch;
            public bool BlocksExplosionToTile(Model.Board.BoardModel board, Model.Board.CellModel cell, Model.Entities.TileModel overlayTile) => _blocksExplosionToTile;
            public void OnMatched(Model.Board.BoardModel board, Model.Board.CellModel cell, Model.Entities.TileModel tile, Presentation.BoardFxContext fxContext) => _inner?.OnMatched(board, cell, tile, fxContext);
            public void OnExploded(Model.Board.BoardModel board, Model.Board.CellModel cell, Model.Entities.TileModel tile, Presentation.BoardFxContext fxContext) => _inner?.OnExploded(board, cell, tile, fxContext);
            public void OnActivated(Model.Board.BoardModel board, Model.Board.CellModel cell, Model.Entities.TileModel tile, Presentation.BoardFxContext fxContext) => _inner?.OnActivated(board, cell, tile, fxContext);
        }
    }
}

