using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Entities;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Logic.Move
{
    public sealed class BoardMoveSnapshot
    {
        private readonly CellModel[] _cells;
        private readonly TileModel[] _tiles;
        private readonly TileModel[] _overlays;

        private BoardMoveSnapshot(CellModel[] cells, TileModel[] tiles, TileModel[] overlays)
        {
            _cells = cells;
            _tiles = tiles;
            _overlays = overlays;
        }

        public static BoardMoveSnapshot Capture(System.Collections.Generic.IReadOnlyList<CellModel> cells)
        {
            CellModel[] capturedCells = new CellModel[cells.Count];
            TileModel[] capturedTiles = new TileModel[cells.Count];
            TileModel[] capturedOverlays = new TileModel[cells.Count];

            for (int i = 0; i < cells.Count; i++)
            {
                capturedCells[i] = cells[i];
                capturedTiles[i] = cells[i].Tile;
                capturedOverlays[i] = cells[i].Overlay;
            }

            return new BoardMoveSnapshot(capturedCells, capturedTiles, capturedOverlays);
        }

        public int Count => _cells.Length;
        public CellModel GetCell(int index) => _cells[index];
        public TileModel GetTile(int index) => _tiles[index];
        public TileModel GetOverlay(int index) => _overlays[index];

        public void Restore()
        {
            for (int i = 0; i < _cells.Length; i++)
            {
                _cells[i].SetTile(_tiles[i]);
                _cells[i].SetOverlay(_overlays[i]);
            }
        }
    }
}

