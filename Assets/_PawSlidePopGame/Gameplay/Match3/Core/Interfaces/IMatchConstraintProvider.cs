using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Entities;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Core.Interfaces
{
    public interface IMatchConstraintProvider
    {
        bool BlocksMatch(BoardModel board, CellModel cell, TileModel tile);
    }
}

