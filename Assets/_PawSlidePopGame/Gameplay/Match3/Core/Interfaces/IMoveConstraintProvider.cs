using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Entities;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Core.Interfaces
{
    public interface IMoveConstraintProvider
    {
        bool CanTileEnter(BoardModel board, CellModel cell, TileModel tile);
        bool CanTileExit(BoardModel board, CellModel cell, TileModel tile);
        bool LocksLine(BoardModel board, CellModel cell, MoveAxis axis);
    }
}

