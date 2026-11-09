using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Logic.Move
{
    public sealed class BoardMoveContext
    {
        public BoardMoveRequest Request { get; }
        public IReadOnlyList<CellModel> AffectedCells { get; }
        public BoardMoveSnapshot Snapshot { get; }

        public BoardMoveContext(BoardMoveRequest request, IReadOnlyList<CellModel> affectedCells, BoardMoveSnapshot snapshot)
        {
            Request = request;
            AffectedCells = affectedCells;
            Snapshot = snapshot;
        }
    }
}

