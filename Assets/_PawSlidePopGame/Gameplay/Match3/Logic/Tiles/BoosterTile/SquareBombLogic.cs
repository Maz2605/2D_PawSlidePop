using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Base;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Entities;
using _PawSlidePopGame._Scripts.Feature.Match3.Presentation;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Logic.Booster
{
    public class SquareBombLogic : BaseBoosterLogic
    {
        public override void OnActivated(BoardModel board, CellModel cell, TileModel tile, BoardFxContext fxContext)
        {
            if (board == null || cell == null || tile == null)
            {
                return;
            }

            List<CellModel> candidates = new List<CellModel>();
            foreach (CellModel candidate in board.GetAllCells())
            {
                if (candidate == null || !candidate.IsPlayable)
                {
                    continue;
                }

                if (candidate.TopTile != null)
                {
                    candidates.Add(candidate);
                }
            }

            if (candidates.Count == 0)
            {
                return;
            }

            int index = fxContext?.Random != null ? fxContext.Random.Next(0, candidates.Count) : UnityEngine.Random.Range(0, candidates.Count);
            CellModel targetCell = candidates[index];
            fxContext?.RecordActivate(tile, cell);
            fxContext?.RecordTargetSelection(tile, targetCell);
            fxContext?.RecordClear(tile, cell, false);
            cell.ClearTile(cell.GetTileLayer(tile) ?? TileStackLayer.Base);
            board.AddScore(30);
            fxContext?.RecordScore(tile, cell, 30, board.CurrentScore);

            for (int x = targetCell.X - 1; x <= targetCell.X + 1; x++)
            {
                for (int y = targetCell.Y - 1; y <= targetCell.Y + 1; y++)
                {
                    CellModel affectedCell = board.GetCell(x, y);
                    BoosterExplosionUtility.ExplodeCell(board, affectedCell, fxContext);
                }
            }
        }
    }
}

