using _PawSlidePopGame._Scripts.Feature.Match3.Core;
using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Entities;
using _PawSlidePopGame._Scripts.Feature.Match3.Presentation;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Logic.Overlays
{
    public sealed class ChocolateOverlayLogic : ITileLogic
    {
        public bool CanBeMoved() => true;
        public bool CanFall() => true;
        public bool CanMatch() => false;
        public bool BlocksTileBelow() => true;

        public void OnMatched(BoardModel board, CellModel cell, TileModel tile, BoardFxContext fxContext)
        {
            Break(board, cell, tile, fxContext);
        }

        public void OnExploded(BoardModel board, CellModel cell, TileModel tile, BoardFxContext fxContext)
        {
            Break(board, cell, tile, fxContext);
        }

        public void OnActivated(BoardModel board, CellModel cell, TileModel tile, BoardFxContext fxContext)
        {
        }

        private static void Break(BoardModel board, CellModel cell, TileModel tile, BoardFxContext fxContext)
        {
            if (tile == null || cell == null || tile.IsDead)
            {
                return;
            }

            int previousHp = tile.CurrentHP;
            tile.TakeDamage(previousHp);
            fxContext?.RecordDamage(tile, cell, previousHp, tile.CurrentHP, true);
            fxContext?.RecordClear(tile, cell, false);
            cell.ClearTile(cell.GetTileLayer(tile) ?? TileStackLayer.Overlay);
            board?.AddScore(50);
            fxContext?.RecordScore(tile, cell, 50, board != null ? board.CurrentScore : 0);
        }
    }
}

