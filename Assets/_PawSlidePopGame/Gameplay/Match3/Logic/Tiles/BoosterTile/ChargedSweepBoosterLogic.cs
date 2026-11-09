using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Base;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Entities;
using _PawSlidePopGame._Scripts.Feature.Match3.Presentation;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Logic.Booster
{
    public class ChargedSweepBoosterLogic : BaseBoosterLogic
    {
        private static readonly BoardDirection[] Directions =
        {
            new BoardDirection(0, -1),
            new BoardDirection(1, 0),
            new BoardDirection(0, 1),
            new BoardDirection(-1, 0)
        };

        public override void OnActivated(BoardModel board, CellModel cell, TileModel tile, BoardFxContext fxContext)
        {
            if (board == null || cell == null || tile == null)
            {
                return;
            }

            TileModel selectedTargetTile = FindDirectionalTarget(board, cell, fxContext);
            if (selectedTargetTile == null)
            {
                selectedTargetTile = FindFallbackTarget(board, fxContext);
            }

            fxContext?.RecordActivate(tile, cell);
            if (selectedTargetTile != null)
            {
                CellModel targetCell = FindCellForTile(board, selectedTargetTile);
                if (targetCell != null)
                {
                    fxContext?.RecordTargetSelection(tile, targetCell, selectedTargetTile);
                }
            }

            fxContext?.RecordClear(tile, cell, false);
            cell.ClearTile(cell.GetTileLayer(tile) ?? TileStackLayer.Base);
            board.AddScore(45);
            fxContext?.RecordScore(tile, cell, 45, board.CurrentScore);

            if (!TryGetAnimalId(selectedTargetTile, out AnimalTileId targetAnimalId))
            {
                return;
            }

            List<CellModel> affectedCells = new List<CellModel>();
            foreach (CellModel candidate in board.GetAllCells())
            {
                if (candidate?.Tile == null || candidate.Tile.TileKind != TileKind.Normal)
                {
                    continue;
                }

                if (!TryGetAnimalId(candidate.Tile, out AnimalTileId candidateAnimalId) || candidateAnimalId != targetAnimalId)
                {
                    continue;
                }

                affectedCells.Add(candidate);
            }

            for (int i = 0; i < affectedCells.Count; i++)
            {
                BoosterExplosionUtility.ExplodeCell(board, affectedCells[i], fxContext);
            }
        }

        private static TileModel FindDirectionalTarget(BoardModel board, CellModel originCell, BoardFxContext fxContext)
        {
            List<BoardDirection> shuffledDirections = new List<BoardDirection>(Directions);
            ShuffleDirections(shuffledDirections, fxContext);

            for (int directionIndex = 0; directionIndex < shuffledDirections.Count; directionIndex++)
            {
                BoardDirection direction = shuffledDirections[directionIndex];
                TileModel targetTile = ScanDirection(board, originCell, direction);
                if (targetTile != null)
                {
                    return targetTile;
                }
            }

            return null;
        }

        private static TileModel ScanDirection(BoardModel board, CellModel originCell, BoardDirection direction)
        {
            int x = originCell.X + direction.X;
            int y = originCell.Y + direction.Y;
            while (true)
            {
                CellModel candidate = board.GetCell(x, y);
                if (candidate == null)
                {
                    return null;
                }

                if (candidate.IsPlayable &&
                    candidate.Tile != null &&
                    candidate.Tile.TileKind == TileKind.Normal &&
                    TryGetAnimalId(candidate.Tile, out _))
                {
                    return candidate.Tile;
                }

                x += direction.X;
                y += direction.Y;
            }
        }

        private static TileModel FindFallbackTarget(BoardModel board, BoardFxContext fxContext)
        {
            List<TileModel> candidates = new List<TileModel>();
            foreach (CellModel cell in board.GetAllCells())
            {
                if (cell?.Tile == null || cell.Tile.TileKind != TileKind.Normal)
                {
                    continue;
                }

                if (TryGetAnimalId(cell.Tile, out _))
                {
                    candidates.Add(cell.Tile);
                }
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            int index = fxContext?.Random != null
                ? fxContext.Random.Next(0, candidates.Count)
                : UnityEngine.Random.Range(0, candidates.Count);
            return candidates[index];
        }

        private static CellModel FindCellForTile(BoardModel board, TileModel tile)
        {
            if (board == null || tile == null)
            {
                return null;
            }

            foreach (CellModel cell in board.GetAllCells())
            {
                if (cell?.Tile != null && cell.Tile.InstanceId == tile.InstanceId)
                {
                    return cell;
                }
            }

            return null;
        }

        private static bool TryGetAnimalId(TileModel tile, out AnimalTileId animalId)
        {
            animalId = AnimalTileId.None;
            if (!(tile?.Definition is NormalAnimalTileDefinitionSO definition))
            {
                return false;
            }

            animalId = definition.AnimalId;
            return animalId != AnimalTileId.None;
        }

        private static void ShuffleDirections(List<BoardDirection> directions, BoardFxContext fxContext)
        {
            if (directions == null || directions.Count <= 1)
            {
                return;
            }

            System.Random random = fxContext?.Random;
            if (random == null)
            {
                for (int i = directions.Count - 1; i > 0; i--)
                {
                    int swapIndex = UnityEngine.Random.Range(0, i + 1);
                    (directions[i], directions[swapIndex]) = (directions[swapIndex], directions[i]);
                }

                return;
            }

            for (int i = directions.Count - 1; i > 0; i--)
            {
                int swapIndex = random.Next(0, i + 1);
                (directions[i], directions[swapIndex]) = (directions[swapIndex], directions[i]);
            }
        }

        private readonly struct BoardDirection
        {
            public int X { get; }
            public int Y { get; }

            public BoardDirection(int x, int y)
            {
                X = x;
                Y = y;
            }
        }
    }
}

