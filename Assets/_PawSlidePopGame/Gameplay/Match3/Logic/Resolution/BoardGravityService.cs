using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Entities;
using _PawSlidePopGame._Scripts.Feature.Match3.Presentation;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Logic.Resolution
{
    public static class BoardGravityService
    {
        public static int Apply(BoardModel board, IList<TileTravelOp> travelOps = null)
        {
            int movedTiles = 0;

            for (int x = 0; x < board.Width; x++)
            {
                int writeY = board.Height - 1;

                for (int y = board.Height - 1; y >= 0; y--)
                {
                    CellModel cell = board.GetCell(x, y);
                    if (cell == null || !cell.IsPlayable)
                    {
                        writeY = y - 1;
                        continue;
                    }

                    TileModel tile = cell.Tile;
                    if (tile == null)
                    {
                        continue;
                    }

                    if (!tile.CanFall() || cell.HasBlockingOverlay())
                    {
                        writeY = y - 1;
                        continue;
                    }

                    while (writeY > y)
                    {
                        CellModel destination = board.GetCell(x, writeY);
                        if (destination != null && destination.IsPlayable && destination.IsEmpty())
                        {
                            break;
                        }

                        writeY--;
                    }

                    if (writeY > y)
                    {
                        CellModel target = board.GetCell(x, writeY);
                        travelOps?.Add(new TileTravelOp
                        {
                            TileInstanceId = tile.InstanceId,
                            TileId = tile.TileId,
                            Layer = TileStackLayer.Base,
                            FromCell = new BoardCellPosition(cell.X, cell.Y),
                            ToCell = new BoardCellPosition(target.X, target.Y),
                            Distance = target.Y - cell.Y,
                            IsWrapAround = false
                        });
                        target.SetTile(tile);
                        cell.ClearTile();
                        movedTiles++;
                    }

                    writeY--;
                }
            }

            return movedTiles;
        }
    }
}

