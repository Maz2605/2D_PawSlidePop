  using _PawSlidePopGame._Scripts.Feature.Match3.Core;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Entities;
using _PawSlidePopGame._Scripts.Feature.Match3.Presentation;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Logic.Mechanics
{
    public sealed class CakeDeliveryLogic : ITileLogic
    {
        public bool CanBeMoved() => true;
        public bool CanFall() => true;
        public bool CanMatch() => false;
        public bool BlocksTileBelow() => false;

        public void OnMatched(BoardModel board, CellModel cell, TileModel tile, BoardFxContext fxContext)
        {
        }

        public void OnExploded(BoardModel board, CellModel cell, TileModel tile, BoardFxContext fxContext)
        {
        }

        public void OnActivated(BoardModel board, CellModel cell, TileModel tile, BoardFxContext fxContext)
        {
        }
    }
}

