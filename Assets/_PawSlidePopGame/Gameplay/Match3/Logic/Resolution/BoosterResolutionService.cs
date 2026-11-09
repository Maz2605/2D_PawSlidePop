using System;
using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Feature.Match3.Boosters;
using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Booster;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Rules;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Entities;
using _PawSlidePopGame._Scripts.Feature.Match3.Presentation;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Logic.Resolution
{
    public static class BoosterResolutionService
    {
        private const int DefaultRainbowTileId = 155;
        private const int DirectClearScore = 10;

        public static bool CanUseAt(BoosterDefinitionSO definition, BoardModel board, int x, int y, Match3TileDatabaseSO tileDatabase)
        {
            if (definition == null || board == null)
            {
                return false;
            }

            CellModel cell = board.GetCell(x, y);
            switch (definition.BoosterType)
            {
                case BoosterType.Hammer:
                    return CanHammerCell(cell);
                case BoosterType.RainbowPlacement:
                    return BoardResolutionService.CanPlaceChargedBoosterAt(
                        board,
                        x,
                        y,
                        ResolveCreatedTileId(definition),
                        tileDatabase);
                default:
                    return false;
            }
        }

        public static bool CanUseLine(BoosterDefinitionSO definition, BoardModel board, MoveAxis axis, int lineIndex)
        {
            if (definition == null || definition.BoosterType != BoosterType.LineClear || board == null)
            {
                return false;
            }

            List<CellModel> cells = board.GetPlayableCellsForMove(axis, lineIndex);
            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i]?.TopTile != null)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool CanUseImmediate(BoosterDefinitionSO definition, BoardModel board)
        {
            return definition != null &&
                   definition.BoosterType == BoosterType.Shuffle &&
                   CountShuffleEligibleCells(board) >= 2;
        }

        public static BoardMoveExecutionResult ExecuteAt(
            BoosterDefinitionSO definition,
            BoardModel board,
            int x,
            int y,
            Match3LevelData levelData,
            Match3TileDatabaseSO tileDatabase,
            Random random,
            BoardRuleSet ruleSet = null)
        {
            if (definition == null)
            {
                return CreateEmptyResult();
            }

            switch (definition.BoosterType)
            {
                case BoosterType.Hammer:
                    return ExecuteHammer(board, x, y, levelData, tileDatabase, random, ruleSet);
                case BoosterType.RainbowPlacement:
                    return ExecuteRainbowPlacement(board, x, y, ResolveCreatedTileId(definition), tileDatabase, random);
                default:
                    return CreateEmptyResult();
            }
        }

        public static BoardMoveExecutionResult ExecuteLine(
            BoosterDefinitionSO definition,
            BoardModel board,
            MoveAxis axis,
            int lineIndex,
            Match3LevelData levelData,
            Match3TileDatabaseSO tileDatabase,
            Random random,
            BoardRuleSet ruleSet = null)
        {
            if (definition == null || definition.BoosterType != BoosterType.LineClear)
            {
                return CreateEmptyResult();
            }

            return ExecuteLineClear(board, axis, lineIndex, levelData, tileDatabase, random, ruleSet);
        }

        public static BoardMoveExecutionResult ExecuteImmediate(
            BoosterDefinitionSO definition,
            BoardModel board,
            Match3LevelData levelData,
            Match3TileDatabaseSO tileDatabase,
            Random random,
            BoardRuleSet ruleSet = null)
        {
            if (definition == null || definition.BoosterType != BoosterType.Shuffle)
            {
                return CreateEmptyResult();
            }

            return ExecuteShuffle(board, levelData, tileDatabase, random, ruleSet);
        }

        private static BoardMoveExecutionResult ExecuteHammer(
            BoardModel board,
            int x,
            int y,
            Match3LevelData levelData,
            Match3TileDatabaseSO tileDatabase,
            Random random,
            BoardRuleSet ruleSet)
        {
            BoardMoveExecutionResult executionResult = CreateEmptyResult();
            if (board == null || levelData == null || tileDatabase == null || random == null)
            {
                return executionResult;
            }

            CellModel cell = board.GetCell(x, y);
            if (!CanHammerCell(cell))
            {
                return executionResult;
            }

            executionResult.IsApplied = true;
            executionResult.IsAccepted = true;

            BoardPresentationTraceBuilder traceBuilder = new BoardPresentationTraceBuilder();
            BoardResolutionResult resolutionResult = new BoardResolutionResult { IsMoveAccepted = true };
            int scoreBefore = board.CurrentScore;

            CascadeTrace clearCascade = traceBuilder.BeginCascade();
            BoardFxContext fxContext = new BoardFxContext(clearCascade, random);
            ClearTopLayerDirect(board, cell, fxContext);
            resolutionResult.CascadesResolved += BoardResolutionService.CountCascadeAsResolved(clearCascade);
            resolutionResult.ClearedTiles += clearCascade.ClearPhase.ClearOps.Count;

            BoardGravityService.Apply(board, clearCascade.GravityPhase.TravelOps);
            resolutionResult.SpawnedTiles += BoardRefillService.Apply(board, levelData, tileDatabase, random, clearCascade.RefillPhase.SpawnOps);
            BoardResolutionService.ResolveAfterBoosterMutation(board, levelData, tileDatabase, random, resolutionResult, traceBuilder, ruleSet);
            resolutionResult.ScoreDelta = board.CurrentScore - scoreBefore;

            executionResult.ResolutionResult = resolutionResult;
            executionResult.PresentationTrace = traceBuilder.Build();
            return executionResult;
        }

        private static BoardMoveExecutionResult ExecuteLineClear(
            BoardModel board,
            MoveAxis axis,
            int lineIndex,
            Match3LevelData levelData,
            Match3TileDatabaseSO tileDatabase,
            Random random,
            BoardRuleSet ruleSet)
        {
            BoardMoveExecutionResult executionResult = CreateEmptyResult();
            if (board == null || levelData == null || tileDatabase == null || random == null)
            {
                return executionResult;
            }

            List<CellModel> cells = board.GetPlayableCellsForMove(axis, lineIndex);
            if (cells.Count == 0)
            {
                return executionResult;
            }

            executionResult.IsApplied = true;
            executionResult.IsAccepted = true;

            BoardPresentationTraceBuilder traceBuilder = new BoardPresentationTraceBuilder();
            BoardPresentationTrace trace = traceBuilder.Build();
            trace.MoveAttempt = new MoveAttemptTrace
            {
                Axis = axis,
                LineIndex = lineIndex,
                Direction = LineSlideDirection.Right
            };
            BoardResolutionResult resolutionResult = new BoardResolutionResult { IsMoveAccepted = true };
            int scoreBefore = board.CurrentScore;

            CascadeTrace clearCascade = traceBuilder.BeginCascade();
            BoardFxContext fxContext = new BoardFxContext(clearCascade, random);
            for (int i = 0; i < cells.Count; i++)
            {
                BoosterExplosionUtility.ExplodeCell(board, cells[i], fxContext);
            }

            resolutionResult.CascadesResolved += BoardResolutionService.CountCascadeAsResolved(clearCascade);
            resolutionResult.ClearedTiles += clearCascade.ClearPhase.ClearOps.Count;

            BoardGravityService.Apply(board, clearCascade.GravityPhase.TravelOps);
            resolutionResult.SpawnedTiles += BoardRefillService.Apply(board, levelData, tileDatabase, random, clearCascade.RefillPhase.SpawnOps);
            BoardResolutionService.ResolveAfterBoosterMutation(board, levelData, tileDatabase, random, resolutionResult, traceBuilder, ruleSet);
            resolutionResult.ScoreDelta = board.CurrentScore - scoreBefore;

            executionResult.ResolutionResult = resolutionResult;
            executionResult.PresentationTrace = traceBuilder.Build();
            return executionResult;
        }

        private static BoardMoveExecutionResult ExecuteRainbowPlacement(
            BoardModel board,
            int x,
            int y,
            int createdTileId,
            Match3TileDatabaseSO tileDatabase,
            Random random)
        {
            BoardMoveExecutionResult executionResult = BoardResolutionService.ExecuteChargedPlacement(
                board,
                x,
                y,
                createdTileId,
                tileDatabase,
                random);
            executionResult.Kind = BoardExecutionKind.Booster;
            return executionResult;
        }

        public static BoardMoveExecutionResult ExecuteShuffle(
            BoardModel board,
            Match3LevelData levelData,
            Match3TileDatabaseSO tileDatabase,
            Random random,
            BoardRuleSet ruleSet)
        {
            BoardMoveExecutionResult executionResult = CreateEmptyResult();
            if (board == null || levelData == null || tileDatabase == null || random == null)
            {
                return executionResult;
            }

            List<CellModel> cells = CollectShuffleEligibleCells(board);
            if (cells.Count < 2)
            {
                return executionResult;
            }

            List<TileContentModel> originalTiles = new List<TileContentModel>(cells.Count);
            for (int i = 0; i < cells.Count; i++)
            {
                originalTiles.Add(cells[i].Tile);
            }

            List<int> shuffledIndices = new List<int>(cells.Count);
            for (int i = 0; i < cells.Count; i++)
            {
                shuffledIndices.Add(i);
            }

            int maxAttempts = 20;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                // Shuffle indices
                for (int i = shuffledIndices.Count - 1; i > 0; i--)
                {
                    int swapIndex = random.Next(0, i + 1);
                    (shuffledIndices[i], shuffledIndices[swapIndex]) = (shuffledIndices[swapIndex], shuffledIndices[i]);
                }

                // Ensure it has a different order if possible
                bool isDifferent = false;
                for (int i = 0; i < shuffledIndices.Count; i++)
                {
                    if (shuffledIndices[i] != i)
                    {
                        isDifferent = true;
                        break;
                    }
                }
                if (!isDifferent && shuffledIndices.Count >= 2)
                {
                    int first = shuffledIndices[0];
                    shuffledIndices.RemoveAt(0);
                    shuffledIndices.Add(first);
                }

                // Clone board to simulate
                BoardModel tempBoard = board.Clone(tileDatabase);
                List<CellModel> tempCells = CollectShuffleEligibleCells(tempBoard);
                if (tempCells.Count != cells.Count)
                {
                    continue;
                }

                List<TileContentModel> tempOriginalTiles = new List<TileContentModel>(tempCells.Count);
                for (int i = 0; i < tempCells.Count; i++)
                {
                    tempOriginalTiles.Add(tempCells[i].Tile);
                }

                // Apply shuffled cloned tiles
                for (int i = 0; i < tempCells.Count; i++)
                {
                    tempCells[i].SetTile(tempOriginalTiles[shuffledIndices[i]]);
                }

                // Resolve temp board matches/refills
                BoardResolutionResult tempResult = new BoardResolutionResult();
                BoardPresentationTraceBuilder tempTraceBuilder = new BoardPresentationTraceBuilder();
                BoardResolutionService.ResolveAfterBoosterMutation(tempBoard, levelData, tileDatabase, random, tempResult, tempTraceBuilder, ruleSet);

                // Check if the resolved board has possible moves
                var matchRule = ruleSet != null ? ruleSet.MatchRule : BoardRuleSet.Default.MatchRule;
                if (BoardResolutionService.HasPossibleMoves(tempBoard, matchRule))
                {
                    break;
                }
            }

            List<TileContentModel> shuffledTiles = new List<TileContentModel>(cells.Count);
            for (int i = 0; i < cells.Count; i++)
            {
                shuffledTiles.Add(originalTiles[shuffledIndices[i]]);
            }

            executionResult.IsApplied = true;
            executionResult.IsAccepted = true;

            BoardPresentationTraceBuilder traceBuilder = new BoardPresentationTraceBuilder();
            BoardResolutionResult resolutionResult = new BoardResolutionResult { IsMoveAccepted = true };
            int scoreBefore = board.CurrentScore;

            CascadeTrace shuffleCascade = traceBuilder.BeginCascade();
            for (int destinationIndex = 0; destinationIndex < cells.Count; destinationIndex++)
            {
                CellModel destinationCell = cells[destinationIndex];
                TileContentModel tile = shuffledTiles[destinationIndex];
                int sourceIndex = IndexOfTile(originalTiles, tile);
                if (sourceIndex >= 0 && sourceIndex != destinationIndex)
                {
                    CellModel sourceCell = cells[sourceIndex];
                    shuffleCascade.GravityPhase.TravelOps.Add(new TileTravelOp
                    {
                        TileInstanceId = tile.InstanceId,
                        TileId = tile.TileId,
                        Layer = TileStackLayer.Base,
                        FromCell = new BoardCellPosition(sourceCell.X, sourceCell.Y),
                        ToCell = new BoardCellPosition(destinationCell.X, destinationCell.Y),
                        Distance = Math.Abs(destinationCell.X - sourceCell.X) + Math.Abs(destinationCell.Y - sourceCell.Y),
                        IsWrapAround = false
                    });
                }
            }

            for (int i = 0; i < cells.Count; i++)
            {
                cells[i].SetTile(shuffledTiles[i]);
            }

            resolutionResult.CascadesResolved++;
            BoardResolutionService.ResolveAfterBoosterMutation(board, levelData, tileDatabase, random, resolutionResult, traceBuilder, ruleSet);
            resolutionResult.ScoreDelta = board.CurrentScore - scoreBefore;

            executionResult.ResolutionResult = resolutionResult;
            executionResult.PresentationTrace = traceBuilder.Build();
            return executionResult;
        }

        private static void ClearTopLayerDirect(BoardModel board, CellModel cell, BoardFxContext fxContext)
        {
            if (board == null || cell == null)
            {
                return;
            }

            if (cell.Overlay != null)
            {
                TileModel overlayTile = cell.Overlay;
                if (overlayTile.CurrentHP > 0 && overlayTile.TileKind != TileKind.Normal)
                {
                    int previousHp = overlayTile.CurrentHP;
                    overlayTile.TakeDamage(previousHp);
                    fxContext?.RecordDamage(overlayTile, cell, previousHp, overlayTile.CurrentHP, true);
                }

                fxContext?.RecordClear(overlayTile, cell, false);
                cell.ClearOverlay();
                board.AddScore(DirectClearScore);
                fxContext?.RecordScore(overlayTile, cell, DirectClearScore, board.CurrentScore);
            }

            if (cell.Tile != null)
            {
                TileModel baseTile = cell.Tile;
                if (baseTile.CurrentHP > 0 && baseTile.TileKind != TileKind.Normal)
                {
                    int previousHp = baseTile.CurrentHP;
                    baseTile.TakeDamage(previousHp);
                    fxContext?.RecordDamage(baseTile, cell, previousHp, baseTile.CurrentHP, true);
                }

                fxContext?.RecordClear(baseTile, cell, false);
                cell.ClearTile();
                board.AddScore(DirectClearScore);
                fxContext?.RecordScore(baseTile, cell, DirectClearScore, board.CurrentScore);
            }
        }

        private static bool CanHammerCell(CellModel cell)
        {
            return cell != null &&
                   cell.IsPlayable &&
                   (cell.Overlay != null || cell.Tile != null);
        }

        private static int ResolveCreatedTileId(BoosterDefinitionSO definition)
        {
            return definition != null && definition.CreatedTileId > 0
                ? definition.CreatedTileId
                : DefaultRainbowTileId;
        }

        private static int CountShuffleEligibleCells(BoardModel board)
        {
            return CollectShuffleEligibleCells(board).Count;
        }

        private static List<CellModel> CollectShuffleEligibleCells(BoardModel board)
        {
            List<CellModel> cells = new List<CellModel>();
            if (board == null)
            {
                return cells;
            }

            foreach (CellModel cell in board.GetAllCells())
            {
                if (cell == null ||
                    !cell.IsPlayable ||
                    cell.Tile == null ||
                    cell.Tile.TileKind != TileKind.Normal)
                {
                    continue;
                }

                cells.Add(cell);
            }

            return cells;
        }

        private static void Shuffle<T>(IList<T> items, Random random)
        {
            for (int i = items.Count - 1; i > 0; i--)
            {
                int swapIndex = random.Next(0, i + 1);
                (items[i], items[swapIndex]) = (items[swapIndex], items[i]);
            }
        }

        private static bool HasDifferentOrder(IReadOnlyList<TileContentModel> original, IReadOnlyList<TileContentModel> shuffled)
        {
            for (int i = 0; i < original.Count; i++)
            {
                if (!ReferenceEquals(original[i], shuffled[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static int IndexOfTile(IReadOnlyList<TileContentModel> tiles, TileContentModel tile)
        {
            for (int i = 0; i < tiles.Count; i++)
            {
                if (ReferenceEquals(tiles[i], tile))
                {
                    return i;
                }
            }

            return -1;
        }

        private static BoardMoveExecutionResult CreateEmptyResult()
        {
            return new BoardMoveExecutionResult
            {
                Kind = BoardExecutionKind.Booster
            };
        }
    }
}
