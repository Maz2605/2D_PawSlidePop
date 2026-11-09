using _PawSlidePopGame._Scripts.Feature.Match3.Core;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Entities;
using _PawSlidePopGame._Scripts.Feature.Match3.Presentation;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Logic.Base
{
    public abstract class BaseBlockerLogic : ITileLogic
    {
        public virtual bool CanBeMoved() => false;
        public virtual bool CanFall() => false;
        public virtual bool CanMatch() => false;
        public virtual bool BlocksTileBelow() => true;
        public virtual bool IsSolid() => true;

        public virtual void OnMatched(BoardModel board, CellModel cell, TileModel tile, BoardFxContext fxContext) { }
        public virtual void OnActivated(BoardModel board, CellModel cell, TileModel tile, BoardFxContext fxContext) { }
        public abstract void OnExploded(BoardModel board, CellModel cell, TileModel tile, BoardFxContext fxContext);
    }
}

