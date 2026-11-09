using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Entities;
using _PawSlidePopGame._Scripts.Feature.Match3.Presentation;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Logic.Booster
{
    internal static class BoosterExplosionUtility
    {
        public static void ExplodeArea(BoardModel board, CellModel centerCell, TileModel sourceTile, int radius, BoardFxContext fxContext, int score)
        {
            if (board == null || centerCell == null || sourceTile == null)
            {
                return;
            }

            fxContext?.RecordActivate(sourceTile, centerCell);
            fxContext?.RecordClear(sourceTile, centerCell, false);
            centerCell.ClearTile(centerCell.GetTileLayer(sourceTile) ?? TileStackLayer.Base);
            board.AddScore(score);
            fxContext?.RecordScore(sourceTile, centerCell, score, board.CurrentScore);

            for (int x = centerCell.X - radius; x <= centerCell.X + radius; x++)
            {
                for (int y = centerCell.Y - radius; y <= centerCell.Y + radius; y++)
                {
                    if (x == centerCell.X && y == centerCell.Y)
                    {
                        continue;
                    }

                    CellModel neighbor = board.GetCell(x, y);
                    ExplodeCell(board, neighbor, fxContext);
                }
            }
        }

        public static void ExplodeDiamond(BoardModel board, CellModel centerCell, TileModel sourceTile, int radius, BoardFxContext fxContext, int score)
        {
            if (board == null || centerCell == null || sourceTile == null)
            {
                return;
            }

            fxContext?.RecordActivate(sourceTile, centerCell);
            fxContext?.RecordClear(sourceTile, centerCell, false);
            centerCell.ClearTile(centerCell.GetTileLayer(sourceTile) ?? TileStackLayer.Base);
            board.AddScore(score);
            fxContext?.RecordScore(sourceTile, centerCell, score, board.CurrentScore);

            for (int x = centerCell.X - radius; x <= centerCell.X + radius; x++)
            {
                for (int y = centerCell.Y - radius; y <= centerCell.Y + radius; y++)
                {
                    if (x == centerCell.X && y == centerCell.Y)
                    {
                        continue;
                    }

                    if (System.Math.Abs(x - centerCell.X) + System.Math.Abs(y - centerCell.Y) > radius)
                    {
                        continue;
                    }

                    CellModel neighbor = board.GetCell(x, y);
                    ExplodeCell(board, neighbor, fxContext);
                }
            }
        }

        public static void ExplodeCross(BoardModel board, CellModel centerCell, TileModel sourceTile, BoardFxContext fxContext, int score)
        {
            if (board == null || centerCell == null || sourceTile == null)
            {
                return;
            }

            fxContext?.RecordActivate(sourceTile, centerCell);
            fxContext?.RecordClear(sourceTile, centerCell, false);
            centerCell.ClearTile(centerCell.GetTileLayer(sourceTile) ?? TileStackLayer.Base);
            board.AddScore(score);
            fxContext?.RecordScore(sourceTile, centerCell, score, board.CurrentScore);

            List<CellModel> rowCells = board.GetPlayableCellsInRow(centerCell.Y);
            for (int i = 0; i < rowCells.Count; i++)
            {
                CellModel cell = rowCells[i];
                if (cell.X == centerCell.X)
                {
                    continue;
                }

                ExplodeCell(board, cell, fxContext);
            }

            List<CellModel> columnCells = board.GetPlayableCellsInColumn(centerCell.X);
            for (int i = 0; i < columnCells.Count; i++)
            {
                CellModel cell = columnCells[i];
                if (cell.Y == centerCell.Y)
                {
                    continue;
                }

                ExplodeCell(board, cell, fxContext);
            }
        }

        public static void ExplodeCell(BoardModel board, CellModel cell, BoardFxContext fxContext)
        {
            if (board == null || cell == null || !cell.IsPlayable)
            {
                return;
            }

            if (cell.Overlay != null)
            {
                TileModel overlayTile = cell.Overlay;
                bool stopsExplosionAtOverlay =
                    overlayTile.LogicType == TileLogicType.IceOverlay ||
                    overlayTile.LogicType == TileLogicType.ChocolateOverlay;
                overlayTile.Explode(board, cell, fxContext);
                if (stopsExplosionAtOverlay || cell.Overlay != null)
                {
                    return;
                }
            }

            if (cell.Tile != null)
            {
                cell.Tile.Explode(board, cell, fxContext);
            }
        }
    }
}

