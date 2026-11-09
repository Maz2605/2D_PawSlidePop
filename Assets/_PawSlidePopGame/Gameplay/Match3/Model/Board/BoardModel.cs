using System;
using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Entities;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Model.Board
{
    public class BoardModel
    {
        public int Width { get; }
        public int Height { get; }
        public int CurrentScore { get; private set; }
        public int RemainingMoves { get; private set; }
        public int ChocolateGrowthTurnsSinceLastBreak { get; private set; }

        private readonly CellModel[,] _grid;
        private int _tileInstanceCounter;

        public BoardModel(Match3LevelData levelData)
        {
            if (levelData == null)
            {
                throw new ArgumentNullException(nameof(levelData));
            }

            Width = levelData.width;
            Height = levelData.height;
            RemainingMoves = levelData.movesLimit;
            _grid = new CellModel[Width, Height];

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    _grid[x, y] = new CellModel(x, y, levelData.IsPlayableCell(x, y));
                }
            }
        }

        public void PopulateBoard(int[] underlayLayout, int[] tileLayout, int[] overlayLayout, Match3TileDatabaseSO tileDatabase)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    int index = (y * Width) + x;
                    CellModel cell = _grid[x, y];
                    if (!cell.IsPlayable)
                    {
                        cell.ClearUnderlay();
                        cell.ClearTile();
                        cell.ClearOverlay();
                        continue;
                    }

                    int underlayTileId = underlayLayout != null && index < underlayLayout.Length
                        ? underlayLayout[index]
                        : 0;
                    int baseTileId = tileLayout != null && index < tileLayout.Length
                        ? tileLayout[index]
                        : 0;
                    SetUnderlayFromDefinitionId(cell, underlayTileId, tileDatabase);
                    SetTileFromDefinitionId(cell, baseTileId, tileDatabase);
                    if (overlayLayout != null && index < overlayLayout.Length)
                    {
                        SetOverlayFromDefinitionId(cell, overlayLayout[index], tileDatabase);
                    }
                    else
                    {
                        cell.ClearOverlay();
                    }
                }
            }
        }

        public CellModel GetCell(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
            {
                return null;
            }

            return _grid[x, y];
        }

        public IEnumerable<CellModel> GetAllCells()
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    yield return _grid[x, y];
                }
            }
        }

        public List<CellModel> GetPlayableCellsInRow(int rowIndex)
        {
            List<CellModel> cells = new List<CellModel>();
            if (rowIndex < 0 || rowIndex >= Height)
            {
                return cells;
            }

            for (int x = 0; x < Width; x++)
            {
                CellModel cell = _grid[x, rowIndex];
                if (cell.IsPlayable)
                {
                    cells.Add(cell);
                }
            }

            return cells;
        }

        public List<CellModel> GetPlayableCellsInColumn(int columnIndex)
        {
            List<CellModel> cells = new List<CellModel>();
            if (columnIndex < 0 || columnIndex >= Width)
            {
                return cells;
            }

            for (int y = 0; y < Height; y++)
            {
                CellModel cell = _grid[columnIndex, y];
                if (cell.IsPlayable)
                {
                    cells.Add(cell);
                }
            }

            return cells;
        }

        public List<CellModel> GetPlayableCellsForMove(MoveAxis axis, int lineIndex)
        {
            return axis == MoveAxis.Row
                ? GetPlayableCellsInRow(lineIndex)
                : GetPlayableCellsInColumn(lineIndex);
        }

        public void RotateTiles(IReadOnlyList<CellModel> cells, int step)
        {
            if (cells == null || cells.Count <= 1)
            {
                return;
            }
            int normalizedStep = step % cells.Count;
            if (normalizedStep < 0)
            {
                normalizedStep += cells.Count;
            }
            if (normalizedStep == 0)
            {
                return;
            }
            TileModel[] baseSnapshot = new TileModel[cells.Count];
            TileModel[] overlaySnapshot = new TileModel[cells.Count];
            for (int i = 0; i < cells.Count; i++)
            {
                baseSnapshot[i] = cells[i].Tile;
                overlaySnapshot[i] = cells[i].Overlay;
            }
            for (int i = 0; i < cells.Count; i++)
            {
                int sourceIndex = (i - normalizedStep + cells.Count) % cells.Count;
                cells[i].SetTile(baseSnapshot[sourceIndex]);
                cells[i].SetOverlay(overlaySnapshot[sourceIndex]);
            }
        }

        public TileModel CreateTileFromDefinitionId(int tileId, Match3TileDatabaseSO tileDatabase)
        {
            if (tileId == 0 || tileDatabase == null)
            {
                return null;
            }

            BoardContentDefinitionSO definition = tileDatabase.GetContentDefinition(tileId);
            if (definition == null)
            {
                return null;
            }

            _tileInstanceCounter++;
            switch (definition.ContentLayer)
            {
                case BoardLayer.Underlay:
                    return new UnderlayContentModel(_tileInstanceCounter, definition as UnderlayDefinitionSO);
                case BoardLayer.Overlay:
                    return new OverlayContentModel(_tileInstanceCounter, definition);
                default:
                    return new TileContentModel(_tileInstanceCounter, definition as TileDefinitionSO);
            }
        }

        public void SetTileFromDefinitionId(CellModel cell, int tileId, Match3TileDatabaseSO tileDatabase)
        {
            if (cell == null)
            {
                return;
            }

            TileModel tile = CreateTileFromDefinitionId(tileId, tileDatabase);
            cell.SetTile(tile);
        }

        public void SetUnderlayFromDefinitionId(CellModel cell, int tileId, Match3TileDatabaseSO tileDatabase)
        {
            if (cell == null)
            {
                return;
            }

            TileModel tile = CreateTileFromDefinitionId(tileId, tileDatabase);
            cell.SetUnderlay(tile);
        }

        public void SetOverlayFromDefinitionId(CellModel cell, int tileId, Match3TileDatabaseSO tileDatabase)
        {
            if (cell == null)
            {
                return;
            }

            TileModel tile = CreateTileFromDefinitionId(tileId, tileDatabase);
            if (tile != null && (!tile.Definition.AllowsOverlayPlacement || !cell.CanAcceptOverlay()))
            {
                cell.ClearOverlay();
                return;
            }

            cell.SetOverlay(tile);
        }

        public void SetTile(CellModel cell, TileModel tile)
        {
            cell?.SetTile(tile);
        }

        public void SetTile(CellModel cell, TileStackLayer layer, TileModel tile)
        {
            cell?.SetTile(layer, tile);
        }

        public void ClearCell(CellModel cell)
        {
            cell?.ClearTile();
        }

        public void ClearCell(CellModel cell, TileStackLayer layer)
        {
            cell?.ClearTile(layer);
        }

        public List<CellModel> GetNeighbors(int x, int y, int radius = 1)
        {
            List<CellModel> neighbors = new List<CellModel>();
            for (int i = x - radius; i <= x + radius; i++)
            {
                for (int j = y - radius; j <= y + radius; j++)
                {
                    if (i == x && j == y)
                    {
                        continue;
                    }

                    CellModel cell = GetCell(i, j);
                    if (cell != null && cell.IsPlayable)
                    {
                        neighbors.Add(cell);
                    }
                }
            }

            return neighbors;
        }

        public void AddScore(int amount)
        {
            CurrentScore += amount;
        }

        public bool CanConsumeMove()
        {
            return RemainingMoves > 0;
        }

        public void ConsumeMove()
        {
            if (RemainingMoves > 0)
            {
                RemainingMoves--;
            }
        }

        public void AddMoves(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            RemainingMoves += amount;
        }

        public void ResetChocolateGrowthCounter()
        {
            ChocolateGrowthTurnsSinceLastBreak = 0;
        }

        public bool AdvanceChocolateGrowthCounter(int requiredTurns)
        {
            if (requiredTurns <= 1)
            {
                ChocolateGrowthTurnsSinceLastBreak = 0;
                return true;
            }

            ChocolateGrowthTurnsSinceLastBreak++;
            if (ChocolateGrowthTurnsSinceLastBreak < requiredTurns)
            {
                return false;
            }

            ChocolateGrowthTurnsSinceLastBreak = 0;
            return true;
        }

        private BoardModel(int width, int height)
        {
            Width = width;
            Height = height;
            _grid = new CellModel[Width, Height];
        }

        public BoardModel Clone(Match3TileDatabaseSO tileDatabase)
        {
            BoardModel clone = new BoardModel(Width, Height);
            clone.CurrentScore = CurrentScore;
            clone.RemainingMoves = RemainingMoves;
            clone.ChocolateGrowthTurnsSinceLastBreak = ChocolateGrowthTurnsSinceLastBreak;
            clone._tileInstanceCounter = _tileInstanceCounter;

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    CellModel originalCell = _grid[x, y];
                    CellModel clonedCell = new CellModel(x, y, originalCell.IsPlayable);
                    clone._grid[x, y] = clonedCell;

                    if (originalCell.IsPlayable)
                    {
                        if (originalCell.Underlay != null)
                        {
                            TileModel t = clone.CreateTileFromDefinitionId(originalCell.Underlay.TileId, tileDatabase);
                            if (t != null)
                            {
                                t.SetHP(originalCell.Underlay.CurrentHP);
                                clonedCell.SetUnderlay(t);
                            }
                        }
                        if (originalCell.Tile != null)
                        {
                            TileModel t = clone.CreateTileFromDefinitionId(originalCell.Tile.TileId, tileDatabase);
                            if (t != null)
                            {
                                t.SetHP(originalCell.Tile.CurrentHP);
                                clonedCell.SetTile(t);
                            }
                        }
                        if (originalCell.Overlay != null)
                        {
                            TileModel t = clone.CreateTileFromDefinitionId(originalCell.Overlay.TileId, tileDatabase);
                            if (t != null)
                            {
                                t.SetHP(originalCell.Overlay.CurrentHP);
                                clonedCell.SetOverlay(t);
                            }
                        }
                    }
                }
            }

            return clone;
        }
    }
}
