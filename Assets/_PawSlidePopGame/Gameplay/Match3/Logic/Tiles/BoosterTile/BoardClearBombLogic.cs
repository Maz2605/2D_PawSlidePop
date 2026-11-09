using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Base;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Entities;
using _PawSlidePopGame._Scripts.Feature.Match3.Presentation;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Logic.Booster
{
    public class BoardClearBombLogic : BaseBoosterLogic
    {
        private const int ActivationScore = 60;

        public override void OnActivated(BoardModel board, CellModel cell, TileModel tile, BoardFxContext fxContext)
        {
            if (board == null || cell == null || tile == null)
            {
                return;
            }

            fxContext?.RecordActivate(tile, cell);
            fxContext?.RecordClear(tile, cell, false);
            cell.ClearTile(cell.GetTileLayer(tile) ?? TileStackLayer.Base);
            board.AddScore(ActivationScore);
            fxContext?.RecordScore(tile, cell, ActivationScore, board.CurrentScore);

            foreach (CellModel targetCell in board.GetAllCells())
            {
                if (targetCell == null || ReferenceEquals(targetCell, cell))
                {
                    continue;
                }

                BoosterExplosionUtility.ExplodeCell(board, targetCell, fxContext);
            }
        }
    }
}
