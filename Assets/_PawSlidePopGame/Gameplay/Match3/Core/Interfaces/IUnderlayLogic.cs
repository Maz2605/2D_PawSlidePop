using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Entities;
using _PawSlidePopGame._Scripts.Feature.Match3.Presentation;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Core.Interfaces
{
    public interface IUnderlayLogic : IMoveConstraintProvider, IMatchConstraintProvider
    {
        void OnTileEntered(BoardModel board, CellModel cell, TileModel tile, BoardFxContext fxContext);
        void OnTileExited(BoardModel board, CellModel cell, TileModel tile, BoardFxContext fxContext);
        void OnMatched(BoardModel board, CellModel cell, UnderlayContentModel tile, BoardFxContext fxContext);
        void OnExploded(BoardModel board, CellModel cell, UnderlayContentModel tile, BoardFxContext fxContext);
    }
}

