using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Base;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Entities;
using _PawSlidePopGame._Scripts.Feature.Match3.Presentation;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Logic.Booster
{
    public class AreaBombLargeLogic : BaseBoosterLogic
    {
        public override void OnActivated(BoardModel board, CellModel cell, TileModel tile, BoardFxContext fxContext)
        {
            BoosterExplosionUtility.ExplodeArea(board, cell, tile, 2, fxContext, 45);
        }
    }
}

