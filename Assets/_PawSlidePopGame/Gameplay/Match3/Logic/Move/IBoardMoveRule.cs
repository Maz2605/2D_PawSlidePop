using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Logic.Move
{
    public interface IBoardMoveRule
    {
        bool TryApply(BoardModel board, BoardMoveRequest request, out BoardMoveContext moveContext);
    }
}

