using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Logic.Move
{
    public class LineSlideMoveRule : IBoardMoveRule
    {
        public bool TryApply(BoardModel board, BoardMoveRequest request, out BoardMoveContext moveContext)
        {
            moveContext = null;
            if (board == null || !request.IsValid())
            {
                return false;
            }
            List<CellModel> affectedCells = board.GetPlayableCellsForMove(request.Axis, request.LineIndex);
            if (affectedCells.Count <= 1)
            {
                return false;
            }
            bool hasAnyMovableState = false;
            for (int i = 0; i < affectedCells.Count; i++)
            {
                CellModel cell = affectedCells[i];
                if (cell == null) continue;

                if (cell.LocksLine(request.Axis))
                {
                    return false;
                }
                if (cell.Tile != null)
                {
                    hasAnyMovableState = true;
                    if (!cell.Tile.CanBeMoved())
                    {
                        return false;
                    }
                }
                if (cell.Overlay != null)
                {
                    hasAnyMovableState = true;
                    if (!cell.Overlay.CanBeMoved())
                    {
                        return false;
                    }
                }
            }
            if (!hasAnyMovableState)
            {
                return false;
            }
            BoardMoveSnapshot snapshot = BoardMoveSnapshot.Capture(affectedCells);
            board.RotateTiles(affectedCells, request.GetRotationStep());
            moveContext = new BoardMoveContext(request, affectedCells, snapshot);
            return true;
        }
    }
}

