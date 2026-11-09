using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Move;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Resolution;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Logic.Rules
{
    public interface IBoardPostMoveRule
    {
        bool TryApply(BoardModel board, BoardMoveContext moveContext, BoardResolutionResult result);
    }
}

