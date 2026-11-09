using _PawSlidePopGame._Scripts.Feature.Match3.Core;
using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Entities;
using _PawSlidePopGame._Scripts.Feature.Match3.Presentation;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Logic.Mechanics
{
    public sealed class StoneLogic : ITileLogic
    {
        public bool CanBeMoved() => true;
        public bool CanFall() => true;
        public bool CanMatch() => false;
        public bool BlocksTileBelow() => false;

        public void OnMatched(BoardModel board, CellModel cell, TileModel tile, BoardFxContext fxContext)
        {
        }

        public void OnExploded(BoardModel board, CellModel cell, TileModel tile, BoardFxContext fxContext)
        {
            if (tile == null || cell == null || tile.IsDead)
            {
                return;
            }

            int previousHp = tile.CurrentHP;
            tile.TakeDamage(previousHp);
            fxContext?.RecordDamage(tile, cell, previousHp, tile.CurrentHP, true);
            fxContext?.RecordClear(tile, cell, false);
            cell.ClearTile(cell.GetTileLayer(tile) ?? TileStackLayer.Base);
            board?.AddScore(50);
            fxContext?.RecordScore(tile, cell, 50, board != null ? board.CurrentScore : 0);
        }

        public void OnActivated(BoardModel board, CellModel cell, TileModel tile, BoardFxContext fxContext)
        {
        }
    }
}

