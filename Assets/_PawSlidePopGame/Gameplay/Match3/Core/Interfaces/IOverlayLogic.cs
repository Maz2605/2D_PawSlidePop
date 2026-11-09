using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Entities;
using _PawSlidePopGame._Scripts.Feature.Match3.Presentation;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Core.Interfaces
{
    public interface IOverlayLogic : IMoveConstraintProvider, IMatchConstraintProvider, IExplosionConstraintProvider
    {
        bool CanBeMoved();
        bool CanFall();
        bool BlocksTileBelow();

        void OnMatched(BoardModel board, CellModel cell, TileModel tile, BoardFxContext fxContext);
        void OnExploded(BoardModel board, CellModel cell, TileModel tile, BoardFxContext fxContext);
        void OnActivated(BoardModel board, CellModel cell, TileModel tile, BoardFxContext fxContext);
    }
}

