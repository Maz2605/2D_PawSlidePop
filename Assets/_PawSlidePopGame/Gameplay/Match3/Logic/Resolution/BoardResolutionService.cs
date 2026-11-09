using System;
using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Booster;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Match;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Move;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Rules;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Entities;
using _PawSlidePopGame._Scripts.Feature.Match3.Presentation;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Logic.Resolution
{
    public static class BoardResolutionService
    {
        public static BoardMoveExecutionResult ExecuteMove(
            BoardModel board,
            BoardMoveRequest moveRequest,
            Match3LevelData levelData,
            Match3TileDatabaseSO tileDatabase,
            Random random,
            BoardRuleSet ruleSet = null)
        {
            BoardMoveExecutionResult executionResult = new BoardMoveExecutionResult();
            executionResult.Kind = BoardExecutionKind.Move;
            BoardRuleSet activeRuleSet = ruleSet ?? BoardRuleSet.Default;
            if (activeRuleSet.MoveRule == null || activeRuleSet.MatchRule == null)
            {
                return executionResult;
            }

            if (!activeRuleSet.MoveRule.TryApply(board, moveRequest, out BoardMoveContext moveContext))
            {
                return executionResult;
            }

            executionResult.IsApplied = true;

            BoardPresentationTraceBuilder traceBuilder = new BoardPresentationTraceBuilder(moveRequest);
            traceBuilder.RecordMoveAttempt(BuildMoveTrace(moveContext));

            BoardResolutionResult resolutionResult = new BoardResolutionResult();
            int scoreBefore = board.CurrentScore;
            bool hasTriggeredRule = ApplyPostMoveRules(board, moveContext, resolutionResult, activeRuleSet.PostMoveRules);
            BoardMatchAnalysis initialAnalysis = activeRuleSet.MatchRule.Analyze(board);

            if (!initialAnalysis.HasMatches && !hasTriggeredRule)
            {
                traceBuilder.RecordRollback(moveRequest, ReverseOperations(traceBuilder.Build().MoveAttempt.TravelOps));
                moveContext.Snapshot.Restore();
                executionResult.IsAccepted = false;
                executionResult.ResolutionResult = resolutionResult;
                executionResult.PresentationTrace = traceBuilder.Build();
                return executionResult;
            }

            if (board.CanConsumeMove())
            {
                board.ConsumeMove();
            }

            executionResult.IsAccepted = true;
            resolutionResult.IsMoveAccepted = true;
            resolutionResult.HasTriggeredPostMoveRule = hasTriggeredRule;
            ResolveBoard(
                board,
                levelData,
                tileDatabase,
                random,
                resolutionResult,
                activeRuleSet.MatchRule,
                initialAnalysis.HasMatches ? initialAnalysis : null,
                traceBuilder,
                moveRequest);
            ApplyDeliveryMechanics(board, levelData, tileDatabase, random, resolutionResult, activeRuleSet.MatchRule, traceBuilder);
            ApplyChocolateGrowth(board, tileDatabase, random, resolutionResult, traceBuilder);
            resolutionResult.ScoreDelta = board.CurrentScore - scoreBefore;

            executionResult.ResolutionResult = resolutionResult;
            executionResult.PresentationTrace = traceBuilder.Build();
            return executionResult;
        }

        public static bool TryResolveMove(
            BoardModel board,
            BoardMoveRequest moveRequest,
            Match3LevelData levelData,
            Match3TileDatabaseSO tileDatabase,
            Random random,
            out BoardResolutionResult result,
            BoardRuleSet ruleSet = null)
        {
            BoardMoveExecutionResult executionResult = ExecuteMove(board, moveRequest, levelData, tileDatabase, random, ruleSet);
            result = executionResult.ResolutionResult;
            return executionResult.IsAccepted;
        }

        public static BoardMoveExecutionResult ExecuteTileActivation(
            BoardModel board,
            int x,
            int y,
            Match3LevelData levelData,
            Match3TileDatabaseSO tileDatabase,
            Random random,
            BoardRuleSet ruleSet = null)
        {
            BoardMoveExecutionResult executionResult = new BoardMoveExecutionResult();
            executionResult.Kind = BoardExecutionKind.TileActivation;
            BoardRuleSet activeRuleSet = ruleSet ?? BoardRuleSet.Default;
            if (board == null || levelData == null || tileDatabase == null || activeRuleSet.MatchRule == null)
            {
                return executionResult;
            }

            CellModel cell = board.GetCell(x, y);
            TileModel tile = cell?.Tile;
            if (cell == null || !cell.IsPlayable || !cell.CanTileActivate() || tile == null || tile.TileKind != TileKind.Booster)
            {
                return executionResult;
            }

            if (!board.CanConsumeMove())
            {
                return executionResult;
            }

            executionResult.IsApplied = true;
            executionResult.IsAccepted = true;

            board.ConsumeMove();

            BoardPresentationTraceBuilder traceBuilder = new BoardPresentationTraceBuilder();
            BoardResolutionResult resolutionResult = new BoardResolutionResult
            {
                IsMoveAccepted = true
            };

            int scoreBefore = board.CurrentScore;
            CascadeTrace activationCascade = traceBuilder.BeginCascade();
            BoardFxContext fxContext = new BoardFxContext(activationCascade, random);

            tile.Activate(board, cell, fxContext);
            resolutionResult.CascadesResolved += CountCascadeAsResolved(activationCascade);
            resolutionResult.ClearedTiles += activationCascade.ClearPhase.ClearOps.Count;

            BoardGravityService.Apply(board, activationCascade.GravityPhase.TravelOps);
            resolutionResult.SpawnedTiles += BoardRefillService.Apply(board, levelData, tileDatabase, random, activationCascade.RefillPhase.SpawnOps);

            ResolveBoard(board, levelData, tileDatabase, random, resolutionResult, activeRuleSet.MatchRule, null, traceBuilder, null);
            ApplyDeliveryMechanics(board, levelData, tileDatabase, random, resolutionResult, activeRuleSet.MatchRule, traceBuilder);
            ApplyChocolateGrowth(board, tileDatabase, random, resolutionResult, traceBuilder);
            resolutionResult.ScoreDelta = board.CurrentScore - scoreBefore;

            executionResult.ResolutionResult = resolutionResult;
            executionResult.PresentationTrace = traceBuilder.Build();
            return executionResult;
        }

        public static BoardMoveExecutionResult ExecuteSugarCrushActivation(
            BoardModel board,
            int x,
            int y,
            Match3LevelData levelData,
            Match3TileDatabaseSO tileDatabase,
            Random random,
            BoardRuleSet ruleSet = null)
        {
            BoardMoveExecutionResult executionResult = new BoardMoveExecutionResult();
            executionResult.Kind = BoardExecutionKind.TileActivation;
            BoardRuleSet activeRuleSet = ruleSet ?? BoardRuleSet.Default;
            if (board == null || levelData == null || tileDatabase == null || activeRuleSet.MatchRule == null)
            {
                return executionResult;
            }

            CellModel cell = board.GetCell(x, y);
            TileModel tile = cell?.Tile;
            if (cell == null || !cell.IsPlayable || !cell.CanTileActivate() || tile == null || tile.TileKind != TileKind.Booster)
            {
                return executionResult;
            }

            executionResult.IsApplied = true;
            executionResult.IsAccepted = true;

            BoardPresentationTraceBuilder traceBuilder = new BoardPresentationTraceBuilder();
            BoardResolutionResult resolutionResult = new BoardResolutionResult
            {
                IsMoveAccepted = true
            };

            int scoreBefore = board.CurrentScore;
            CascadeTrace activationCascade = traceBuilder.BeginCascade();
            BoardFxContext fxContext = new BoardFxContext(activationCascade, random);

            tile.Activate(board, cell, fxContext);
            resolutionResult.CascadesResolved += CountCascadeAsResolved(activationCascade);
            resolutionResult.ClearedTiles += activationCascade.ClearPhase.ClearOps.Count;

            BoardGravityService.Apply(board, activationCascade.GravityPhase.TravelOps);
            resolutionResult.SpawnedTiles += BoardRefillService.Apply(board, levelData, tileDatabase, random, activationCascade.RefillPhase.SpawnOps);

            ResolveBoard(board, levelData, tileDatabase, random, resolutionResult, activeRuleSet.MatchRule, null, traceBuilder, null);
            ApplyDeliveryMechanics(board, levelData, tileDatabase, random, resolutionResult, activeRuleSet.MatchRule, traceBuilder);
            ApplyChocolateGrowth(board, tileDatabase, random, resolutionResult, traceBuilder);
            resolutionResult.ScoreDelta = board.CurrentScore - scoreBefore;

            executionResult.ResolutionResult = resolutionResult;
            executionResult.PresentationTrace = traceBuilder.Build();
            return executionResult;
        }

        public static bool CanPlaceChargedBoosterAt(BoardModel board, int x, int y, int chargedTileId, Match3TileDatabaseSO tileDatabase)
        {
            if (board == null || tileDatabase == null || chargedTileId <= 0)
            {
                return false;
            }

            CellModel cell = board.GetCell(x, y);
            TileDefinitionSO chargedDefinition = tileDatabase.GetTileDefinition(chargedTileId);
            return IsValidChargedPlacementCell(cell, chargedDefinition);
        }

        public static BoardMoveExecutionResult ExecuteChargedPlacement(
            BoardModel board,
            int x,
            int y,
            int chargedTileId,
            Match3TileDatabaseSO tileDatabase,
            Random random)
        {
            BoardMoveExecutionResult executionResult = new BoardMoveExecutionResult
            {
                Kind = BoardExecutionKind.ChargedPlacement
            };

            if (board == null || tileDatabase == null || chargedTileId <= 0)
            {
                return executionResult;
            }

            CellModel cell = board.GetCell(x, y);
            TileDefinitionSO chargedDefinition = tileDatabase.GetTileDefinition(chargedTileId);
            if (!IsValidChargedPlacementCell(cell, chargedDefinition))
            {
                return executionResult;
            }

            TileModel sourceTile = cell.Tile;
            TileModel createdTile = board.CreateTileFromDefinitionId(chargedTileId, tileDatabase);
            if (sourceTile == null || createdTile == null)
            {
                return executionResult;
            }

            executionResult.IsApplied = true;
            executionResult.IsAccepted = true;

            BoardPresentationTraceBuilder traceBuilder = new BoardPresentationTraceBuilder();
            BoardResolutionResult resolutionResult = new BoardResolutionResult
            {
                IsMoveAccepted = true
            };

            CascadeTrace placementCascade = traceBuilder.BeginCascade();
            BoardFxContext fxContext = new BoardFxContext(placementCascade, random);
            board.SetTile(cell, TileStackLayer.Base, createdTile);
            fxContext.RecordSpecialCreate(sourceTile, createdTile, cell, TileStackLayer.Base, true);
            resolutionResult.CascadesResolved += CountCascadeAsResolved(placementCascade);
            resolutionResult.SpawnedTiles += 1;

            executionResult.ResolutionResult = resolutionResult;
            executionResult.PresentationTrace = traceBuilder.Build();
            return executionResult;
        }

        public static BoardMoveExecutionResult ExecuteChargedCombo(
            BoardModel board,
            BoardCellPosition sourcePosition,
            BoardCellPosition partnerPosition,
            Match3LevelData levelData,
            Match3TileDatabaseSO tileDatabase,
            Random random,
            BoardRuleSet ruleSet = null)
        {
            BoardMoveExecutionResult executionResult = new BoardMoveExecutionResult
            {
                Kind = BoardExecutionKind.ChargedCombo
            };
            BoardRuleSet activeRuleSet = ruleSet ?? BoardRuleSet.Default;
            if (board == null || levelData == null || tileDatabase == null || activeRuleSet.MatchRule == null)
            {
                return executionResult;
            }

            CellModel sourceCell = board.GetCell(sourcePosition.X, sourcePosition.Y);
            CellModel partnerCell = board.GetCell(partnerPosition.X, partnerPosition.Y);
            if (!IsValidChargedComboPair(sourceCell, partnerCell) || !board.CanConsumeMove())
            {
                return executionResult;
            }

            executionResult.IsApplied = true;
            executionResult.IsAccepted = true;
            board.ConsumeMove();

            BoardPresentationTraceBuilder traceBuilder = new BoardPresentationTraceBuilder();
            BoardResolutionResult resolutionResult = new BoardResolutionResult
            {
                IsMoveAccepted = true
            };

            int scoreBefore = board.CurrentScore;
            CascadeTrace comboCascade = traceBuilder.BeginCascade();
            BoardFxContext fxContext = new BoardFxContext(comboCascade, random);
            ApplyChargedComboPurge(board, sourceCell, partnerCell, fxContext);
            resolutionResult.CascadesResolved += CountCascadeAsResolved(comboCascade);
            resolutionResult.ClearedTiles += comboCascade.ClearPhase.ClearOps.Count;

            BoardGravityService.Apply(board, comboCascade.GravityPhase.TravelOps);
            resolutionResult.SpawnedTiles += BoardRefillService.Apply(board, levelData, tileDatabase, random, comboCascade.RefillPhase.SpawnOps);

            ResolveBoard(board, levelData, tileDatabase, random, resolutionResult, activeRuleSet.MatchRule, null, traceBuilder, null);
            ApplyDeliveryMechanics(board, levelData, tileDatabase, random, resolutionResult, activeRuleSet.MatchRule, traceBuilder);
            ApplyChocolateGrowth(board, tileDatabase, random, resolutionResult, traceBuilder);
            resolutionResult.ScoreDelta = board.CurrentScore - scoreBefore;

            executionResult.ResolutionResult = resolutionResult;
            executionResult.PresentationTrace = traceBuilder.Build();
            return executionResult;
        }

        public static BoardResolutionResult ResolveBoard(
            BoardModel board,
            Match3LevelData levelData,
            Match3TileDatabaseSO tileDatabase,
            Random random,
            BoardRuleSet ruleSet = null)
        {
            BoardRuleSet activeRuleSet = ruleSet ?? BoardRuleSet.Default;
            int scoreBefore = board.CurrentScore;
            BoardResolutionResult result = new BoardResolutionResult();
            ResolveBoard(board, levelData, tileDatabase, random, result, activeRuleSet.MatchRule, null, null, null);
            result.ScoreDelta = board.CurrentScore - scoreBefore;
            return result;
        }

        internal static void ResolveAfterBoosterMutation(
            BoardModel board,
            Match3LevelData levelData,
            Match3TileDatabaseSO tileDatabase,
            Random random,
            BoardResolutionResult result,
            BoardPresentationTraceBuilder traceBuilder,
            BoardRuleSet ruleSet = null)
        {
            BoardRuleSet activeRuleSet = ruleSet ?? BoardRuleSet.Default;
            if (board == null || levelData == null || tileDatabase == null || random == null || result == null || traceBuilder == null || activeRuleSet.MatchRule == null)
            {
                return;
            }

            ResolveBoard(board, levelData, tileDatabase, random, result, activeRuleSet.MatchRule, null, traceBuilder, null);
            ApplyDeliveryMechanics(board, levelData, tileDatabase, random, result, activeRuleSet.MatchRule, traceBuilder);
            ApplyChocolateGrowth(board, tileDatabase, random, result, traceBuilder);
        }

        private static void ResolveBoard(
            BoardModel board,
            Match3LevelData levelData,
            Match3TileDatabaseSO tileDatabase,
            Random random,
            BoardResolutionResult result,
            IBoardMatchRule matchRule,
            BoardMatchAnalysis startingAnalysis,
            BoardPresentationTraceBuilder traceBuilder,
            BoardMoveRequest? sourceMoveRequest)
        {
            BoardMatchAnalysis matchAnalysis = startingAnalysis ?? matchRule.Analyze(board);
            while (matchAnalysis.HasMatches)
            {
                CascadeTrace cascadeTrace = traceBuilder?.BeginCascade();
                BoardFxContext fxContext = cascadeTrace != null ? new BoardFxContext(cascadeTrace, random) : null;
                BoardMoveRequest decisionMoveRequest = sourceMoveRequest ?? new BoardMoveRequest(Core.Enum.MoveAxis.Row, -1, Core.Enum.LineSlideDirection.Left, -1, -1);
                List<SpecialSpawnDecision> decisions = SpecialCreationService.CreateDecisions(matchAnalysis, decisionMoveRequest, tileDatabase);
                result.CascadesResolved++;
                result.ClearedTiles += ClearMatches(board, matchAnalysis, decisions, tileDatabase, fxContext);
                BoardGravityService.Apply(board, cascadeTrace?.GravityPhase.TravelOps);
                result.SpawnedTiles += BoardRefillService.Apply(board, levelData, tileDatabase, random, cascadeTrace?.RefillPhase.SpawnOps);
                matchAnalysis = matchRule.Analyze(board);
            }
        }

        private static int ClearMatches(
            BoardModel board,
            BoardMatchAnalysis matchAnalysis,
            IReadOnlyList<SpecialSpawnDecision> decisions,
            Match3TileDatabaseSO tileDatabase,
            BoardFxContext fxContext)
        {
            Dictionary<int, SpecialSpawnDecision> decisionsByGroupId = new Dictionary<int, SpecialSpawnDecision>();
            if (decisions != null)
            {
                for (int i = 0; i < decisions.Count; i++)
                {
                    decisionsByGroupId[decisions[i].GroupId] = decisions[i];
                }
            }

            int clearedTiles = 0;
            List<PendingSpecialCreate> pendingCreates = new List<PendingSpecialCreate>();
            ApplyAdjacentSolidBlockerBreaks(board, matchAnalysis, fxContext);

            for (int groupIndex = 0; groupIndex < matchAnalysis.Groups.Count; groupIndex++)
            {
                MatchGroup group = matchAnalysis.Groups[groupIndex];
                decisionsByGroupId.TryGetValue(group.Id, out SpecialSpawnDecision decision);

                for (int cellIndex = 0; cellIndex < group.Cells.Count; cellIndex++)
                {
                    CellModel cell = group.Cells[cellIndex];
                    if (cell?.Tile == null)
                    {
                        continue;
                    }

                    ClearMatchedBubbleOverlay(cell, board, fxContext);

                    if (decision != null && cell == decision.SpawnCell)
                    {
                        pendingCreates.Add(new PendingSpecialCreate(decision, cell.Tile, cell));
                        if (cell.CanTileMatch())
                        {
                            board.AddScore(10);
                            fxContext?.RecordScore(cell.Tile, cell, 10, board.CurrentScore);
                        }

                        continue;
                    }

                    clearedTiles++;
                    cell.Tile.Match(board, cell, fxContext);
                }
            }

            for (int i = 0; i < pendingCreates.Count; i++)
            {
                PendingSpecialCreate pendingCreate = pendingCreates[i];
                BoosterTileDefinitionSO boosterDefinition = tileDatabase != null
                    ? tileDatabase.GetBoosterDefinition(pendingCreate.Decision.LogicType)
                    : null;
                if (boosterDefinition == null || pendingCreate.Cell == null)
                {
                    continue;
                }

                TileModel createdTile = board.CreateTileFromDefinitionId(boosterDefinition.TileId, tileDatabase);
                if (createdTile == null)
                {
                    continue;
                }

                board.SetTile(pendingCreate.Cell, TileStackLayer.Base, createdTile);
                fxContext?.RecordSpecialCreate(pendingCreate.SourceTile, createdTile, pendingCreate.Cell);
            }

            return clearedTiles;
        }

        private static void ClearMatchedBubbleOverlay(CellModel cell, BoardModel board, BoardFxContext fxContext)
        {
            if (cell == null || !cell.HasOverlayLogic(TileLogicType.BubbleOverlay))
            {
                return;
            }
            UnityEngine.Debug.Log($"[Bubble] ClearMatchedBubbleOverlay called for Cell ({cell.X}, {cell.Y})");
            cell.Overlay?.Match(board, cell, fxContext);
        }

        private static void ApplyAdjacentSolidBlockerBreaks(BoardModel board, BoardMatchAnalysis matchAnalysis, BoardFxContext fxContext)
        {
            if (board == null || matchAnalysis?.Groups == null || matchAnalysis.Groups.Count == 0)
            {
                return;
            }

            HashSet<CellModel> blockerCellsToBreak = new HashSet<CellModel>();
            for (int groupIndex = 0; groupIndex < matchAnalysis.Groups.Count; groupIndex++)
            {
                MatchGroup group = matchAnalysis.Groups[groupIndex];
                if (group?.Cells == null)
                {
                    continue;
                }

                for (int cellIndex = 0; cellIndex < group.Cells.Count; cellIndex++)
                {
                    CollectAdjacentSolidBlockerCells(board, group.Cells[cellIndex], blockerCellsToBreak);
                }
            }

            List<CellModel> orderedBlockerCells = new List<CellModel>(blockerCellsToBreak);
            orderedBlockerCells.Sort(CompareCells);
            for (int i = 0; i < orderedBlockerCells.Count; i++)
            {
                CellModel blockerCell = orderedBlockerCells[i];
                blockerCell?.Overlay?.Match(board, blockerCell, fxContext);
            }
        }

        private static void CollectAdjacentSolidBlockerCells(BoardModel board, CellModel sourceCell, HashSet<CellModel> blockerCellsToBreak)
        {
            if (board == null || sourceCell == null || blockerCellsToBreak == null)
            {
                return;
            }

            TryAddAdjacentSolidBlockerCell(board, sourceCell.X + 1, sourceCell.Y, blockerCellsToBreak);
            TryAddAdjacentSolidBlockerCell(board, sourceCell.X - 1, sourceCell.Y, blockerCellsToBreak);
            TryAddAdjacentSolidBlockerCell(board, sourceCell.X, sourceCell.Y + 1, blockerCellsToBreak);
            TryAddAdjacentSolidBlockerCell(board, sourceCell.X, sourceCell.Y - 1, blockerCellsToBreak);
        }

        private static void TryAddAdjacentSolidBlockerCell(BoardModel board, int x, int y, HashSet<CellModel> blockerCellsToBreak)
        {
            CellModel adjacentCell = board.GetCell(x, y);
            if (adjacentCell != null &&
                (adjacentCell.HasOverlayLogic(TileLogicType.IceOverlay) ||
                 adjacentCell.HasOverlayLogic(TileLogicType.ChocolateOverlay)))
            {
                blockerCellsToBreak.Add(adjacentCell);
            }
        }

        private static void ApplyChocolateGrowth(
            BoardModel board,
            Match3TileDatabaseSO tileDatabase,
            Random random,
            BoardResolutionResult resolutionResult,
            BoardPresentationTraceBuilder traceBuilder)
        {
            if (board == null || tileDatabase == null || random == null || traceBuilder == null)
            {
                return;
            }

            BoardPresentationTrace trace = traceBuilder.Build();
            if (HasChocolateClears(trace, tileDatabase))
            {
                board.ResetChocolateGrowthCounter();
                return;
            }

            List<CellModel> chocolateCells = GetLivingChocolateCells(board);
            if (chocolateCells.Count == 0)
            {
                board.ResetChocolateGrowthCounter();
                return;
            }

            ChocolateOverlayDefinitionSO chocolateDefinition = GetChocolateDefinition(chocolateCells, tileDatabase);
            int configuredGrowthCount = chocolateDefinition != null ? chocolateDefinition.GrowthPerTurn : 0;
            if (configuredGrowthCount <= 0)
            {
                return;
            }

            int growthTurnInterval = chocolateDefinition != null ? chocolateDefinition.GrowthTurnInterval : 1;
            if (!board.AdvanceChocolateGrowthCounter(growthTurnInterval))
            {
                return;
            }

            List<ChocolateGrowthCandidate> candidates = CollectChocolateGrowthCandidates(board, chocolateCells);
            if (candidates.Count == 0)
            {
                return;
            }

            int targetSpawnCount = Math.Min(configuredGrowthCount, candidates.Count);
            if (targetSpawnCount <= 0)
            {
                return;
            }

            CascadeTrace growthCascade = traceBuilder.BeginCascade();
            BoardFxContext growthFxContext = new BoardFxContext(growthCascade, random);
            int spawnedCount = 0;
            for (int i = 0; i < targetSpawnCount; i++)
            {
                int selectedIndex = random.Next(0, candidates.Count);
                ChocolateGrowthCandidate selectedCandidate = candidates[selectedIndex];
                candidates.RemoveAt(selectedIndex);

                TileModel createdTile = board.CreateTileFromDefinitionId(chocolateDefinition.TileId, tileDatabase);
                if (createdTile == null)
                {
                    continue;
                }

                board.SetTile(selectedCandidate.TargetCell, TileStackLayer.Overlay, createdTile);
                growthFxContext.RecordSpecialCreate(
                    selectedCandidate.SourceTile,
                    createdTile,
                    selectedCandidate.TargetCell,
                    TileStackLayer.Overlay,
                    false);
                spawnedCount++;
            }

            if (spawnedCount <= 0)
            {
                return;
            }

            if (resolutionResult != null)
            {
                resolutionResult.CascadesResolved += CountCascadeAsResolved(growthCascade);
                resolutionResult.SpawnedTiles += spawnedCount;
            }
        }

        private static ChocolateOverlayDefinitionSO GetChocolateDefinition(IReadOnlyList<CellModel> chocolateCells, Match3TileDatabaseSO tileDatabase)
        {
            if (chocolateCells != null)
            {
                for (int i = 0; i < chocolateCells.Count; i++)
                {
                    if (chocolateCells[i]?.Overlay?.Definition is ChocolateOverlayDefinitionSO chocolateDefinition)
                    {
                        return chocolateDefinition;
                    }
                }
            }

            return tileDatabase?.GetOverlayDefinition(303) as ChocolateOverlayDefinitionSO;
        }

        private static bool HasChocolateClears(BoardPresentationTrace trace, Match3TileDatabaseSO tileDatabase)
        {
            if (trace?.Cascades == null || tileDatabase == null)
            {
                return false;
            }

            for (int cascadeIndex = 0; cascadeIndex < trace.Cascades.Count; cascadeIndex++)
            {
                List<TileClearOp> clearOps = trace.Cascades[cascadeIndex]?.ClearPhase?.ClearOps;
                if (clearOps == null)
                {
                    continue;
                }

                for (int clearIndex = 0; clearIndex < clearOps.Count; clearIndex++)
                {
                    BoardContentDefinitionSO definition = tileDatabase.GetContentDefinition(clearOps[clearIndex].TileId);
                    if (definition != null)
                    {
                        if (definition is ChocolateOverlayDefinitionSO || (definition is TileDefinitionSO tileDef && tileDef.LogicType == TileLogicType.ChocolateOverlay))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static void ApplyDeliveryMechanics(
            BoardModel board,
            Match3LevelData levelData,
            Match3TileDatabaseSO tileDatabase,
            Random random,
            BoardResolutionResult resolutionResult,
            IBoardMatchRule matchRule,
            BoardPresentationTraceBuilder traceBuilder)
        {
            if (board == null || levelData == null || tileDatabase == null || matchRule == null || traceBuilder == null)
            {
                return;
            }

            bool deliveredAny;
            do
            {
                List<CellModel> deliveredCells = CollectDeliveredTargetCells(board);
                int deliveredCount = deliveredCells.Count;
                deliveredAny = deliveredCount > 0;
                if (!deliveredAny)
                {
                    continue;
                }

                CascadeTrace deliveryCascade = traceBuilder.BeginCascade();
                BoardFxContext fxContext = new BoardFxContext(deliveryCascade, random);
                ApplyDeliveredTargetClears(deliveredCells, fxContext);

                resolutionResult.CascadesResolved += CountCascadeAsResolved(deliveryCascade);
                resolutionResult.ClearedTiles += deliveredCount;

                BoardGravityService.Apply(board, deliveryCascade.GravityPhase.TravelOps);
                resolutionResult.SpawnedTiles += BoardRefillService.Apply(board, levelData, tileDatabase, random, deliveryCascade.RefillPhase.SpawnOps);
                ResolveBoard(board, levelData, tileDatabase, random, resolutionResult, matchRule, null, traceBuilder, null);
            } while (deliveredAny);
        }

        private static List<CellModel> CollectDeliveredTargetCells(BoardModel board)
        {
            List<CellModel> deliveredCells = new List<CellModel>();
            if (board == null)
            {
                return deliveredCells;
            }

            for (int x = 0; x < board.Width; x++)
            {
                CellModel exitCell = GetBottomPlayableCell(board, x);
                if (exitCell?.Tile == null || exitCell.Overlay != null || exitCell.Tile.LogicType != TileLogicType.CakeTarget)
                {
                    continue;
                }

                deliveredCells.Add(exitCell);
            }

            return deliveredCells;
        }

        private static void ApplyDeliveredTargetClears(IReadOnlyList<CellModel> deliveredCells, BoardFxContext fxContext)
        {
            if (deliveredCells == null)
            {
                return;
            }

            for (int i = 0; i < deliveredCells.Count; i++)
            {
                CellModel deliveredCell = deliveredCells[i];
                TileModel deliveredTile = deliveredCell?.Tile;
                if (deliveredCell == null || deliveredTile == null)
                {
                    continue;
                }

                fxContext?.RecordClear(deliveredTile, deliveredCell, false);
                deliveredCell.ClearTile();
            }
        }

        private static CellModel GetBottomPlayableCell(BoardModel board, int column)
        {
            if (board == null || column < 0 || column >= board.Width)
            {
                return null;
            }

            for (int y = board.Height - 1; y >= 0; y--)
            {
                CellModel cell = board.GetCell(column, y);
                if (cell != null && cell.IsPlayable)
                {
                    return cell;
                }
            }

            return null;
        }

        private static List<CellModel> GetLivingChocolateCells(BoardModel board)
        {
            List<CellModel> chocolateCells = new List<CellModel>();
            foreach (CellModel cell in board.GetAllCells())
            {
                if (cell != null && cell.HasOverlayLogic(TileLogicType.ChocolateOverlay))
                {
                    chocolateCells.Add(cell);
                }
            }

            return chocolateCells;
        }

        private static List<ChocolateGrowthCandidate> CollectChocolateGrowthCandidates(BoardModel board, IReadOnlyList<CellModel> chocolateCells)
        {
            Dictionary<int, ChocolateGrowthCandidate> candidatesByCell = new Dictionary<int, ChocolateGrowthCandidate>();
            for (int i = 0; i < chocolateCells.Count; i++)
            {
                CellModel sourceCell = chocolateCells[i];
                if (sourceCell?.Overlay == null)
                {
                    continue;
                }

                TryAddChocolateGrowthCandidate(board, sourceCell, sourceCell.X + 1, sourceCell.Y, candidatesByCell);
                TryAddChocolateGrowthCandidate(board, sourceCell, sourceCell.X - 1, sourceCell.Y, candidatesByCell);
                TryAddChocolateGrowthCandidate(board, sourceCell, sourceCell.X, sourceCell.Y + 1, candidatesByCell);
                TryAddChocolateGrowthCandidate(board, sourceCell, sourceCell.X, sourceCell.Y - 1, candidatesByCell);
            }

            List<ChocolateGrowthCandidate> candidates = new List<ChocolateGrowthCandidate>(candidatesByCell.Values);
            candidates.Sort((left, right) => CompareCells(left.TargetCell, right.TargetCell));
            return candidates;
        }

        private static void TryAddChocolateGrowthCandidate(
            BoardModel board,
            CellModel sourceCell,
            int targetX,
            int targetY,
            Dictionary<int, ChocolateGrowthCandidate> candidatesByCell)
        {
            CellModel targetCell = board.GetCell(targetX, targetY);
            if (targetCell == null ||
                !targetCell.IsPlayable ||
                targetCell.Tile == null ||
                !targetCell.CanAcceptOverlay() ||
                targetCell.Overlay != null)
            {
                return;
            }

            int key = (targetCell.Y * board.Width) + targetCell.X;
            if (candidatesByCell.ContainsKey(key))
            {
                return;
            }

            candidatesByCell[key] = new ChocolateGrowthCandidate(sourceCell, targetCell, sourceCell.Overlay);
        }

        private static bool ApplyPostMoveRules(
            BoardModel board,
            BoardMoveContext moveContext,
            BoardResolutionResult result,
            IReadOnlyList<IBoardPostMoveRule> postMoveRules)
        {
            bool hasTriggeredRule = false;
            if (postMoveRules == null)
            {
                return false;
            }

            for (int i = 0; i < postMoveRules.Count; i++)
            {
                IBoardPostMoveRule rule = postMoveRules[i];
                if (rule != null && rule.TryApply(board, moveContext, result))
                {
                    hasTriggeredRule = true;
                }
            }

            return hasTriggeredRule;
        }

        private static List<TileTravelOp> BuildMoveTrace(BoardMoveContext moveContext)
        {
            List<TileTravelOp> operations = new List<TileTravelOp>();
            if (moveContext?.AffectedCells == null || moveContext.Snapshot == null)
            {
                return operations;
            }

            int count = moveContext.AffectedCells.Count;
            if (count <= 0)
            {
                return operations;
            }

            int normalizedStep = moveContext.Request.GetRotationStep() % count;
            if (normalizedStep < 0)
            {
                normalizedStep += count;
            }

            for (int i = 0; i < count; i++)
            {
                CellModel sourceCell = moveContext.Snapshot.GetCell(i);
                CellModel destinationCell = moveContext.AffectedCells[(i + normalizedStep) % count];
                bool isWrapAround = IsWrapAround(moveContext.Request, sourceCell, destinationCell);
                AddTravelOp(operations, moveContext.Snapshot.GetTile(i), TileStackLayer.Base, sourceCell, destinationCell, isWrapAround);
                // Overlays are stationary in the model (never rotated), so they do not slide visually.
                // AddTravelOp(operations, moveContext.Snapshot.GetOverlay(i), TileStackLayer.Overlay, sourceCell, destinationCell, isWrapAround);
            }

            return operations;
        }

        private static void AddTravelOp(
            ICollection<TileTravelOp> operations,
            TileModel tile,
            TileStackLayer layer,
            CellModel sourceCell,
            CellModel destinationCell,
            bool isWrapAround)
        {
            if (operations == null || tile == null || sourceCell == null || destinationCell == null)
            {
                return;
            }

            operations.Add(new TileTravelOp
            {
                TileInstanceId = tile.InstanceId,
                TileId = tile.TileId,
                Layer = layer,
                FromCell = new BoardCellPosition(sourceCell.X, sourceCell.Y),
                ToCell = new BoardCellPosition(destinationCell.X, destinationCell.Y),
                Distance = 1,
                IsWrapAround = isWrapAround
            });
        }

        private static List<TileTravelOp> ReverseOperations(IReadOnlyList<TileTravelOp> operations)
        {
            List<TileTravelOp> reversed = new List<TileTravelOp>();
            if (operations == null)
            {
                return reversed;
            }

            for (int i = 0; i < operations.Count; i++)
            {
                TileTravelOp operation = operations[i];
                reversed.Add(new TileTravelOp
                {
                    TileInstanceId = operation.TileInstanceId,
                    TileId = operation.TileId,
                    Layer = operation.Layer,
                    FromCell = operation.ToCell,
                    ToCell = operation.FromCell,
                    Distance = operation.Distance,
                    IsWrapAround = operation.IsWrapAround
                });
            }

            return reversed;
        }

        private static bool IsWrapAround(BoardMoveRequest request, CellModel sourceCell, CellModel destinationCell)
        {
            if (sourceCell == null || destinationCell == null)
            {
                return false;
            }

            if (request.Axis == Core.Enum.MoveAxis.Row)
            {
                return Math.Abs(sourceCell.X - destinationCell.X) > 1;
            }

            return Math.Abs(sourceCell.Y - destinationCell.Y) > 1;
        }

        internal static int CountCascadeAsResolved(CascadeTrace cascadeTrace)
        {
            if (cascadeTrace == null)
            {
                return 0;
            }

            return cascadeTrace.ClearPhase.ActivateOps.Count > 0 ||
                   cascadeTrace.ClearPhase.TargetSelectionOps.Count > 0 ||
                   cascadeTrace.ClearPhase.DamageOps.Count > 0 ||
                   cascadeTrace.ClearPhase.ClearOps.Count > 0 ||
                   cascadeTrace.ClearPhase.SpecialCreateOps.Count > 0
                ? 1
                : 0;
        }

        private static int CompareCells(CellModel left, CellModel right)
        {
            int byY = left.Y.CompareTo(right.Y);
            return byY != 0 ? byY : left.X.CompareTo(right.X);
        }

        private static bool IsValidChargedPlacementCell(CellModel cell, TileDefinitionSO chargedDefinition)
        {
            return cell != null &&
                   cell.IsPlayable &&
                   chargedDefinition != null &&
                   chargedDefinition.TileKind == TileKind.Booster &&
                   chargedDefinition.LogicType == TileLogicType.ChargedSweepBooster &&
                   cell.Overlay == null &&
                   cell.Tile != null &&
                   cell.Tile.TileKind == TileKind.Normal;
        }

        private static bool IsValidChargedComboPair(CellModel sourceCell, CellModel partnerCell)
        {
            return sourceCell != null &&
                   partnerCell != null &&
                   sourceCell.IsPlayable &&
                   partnerCell.IsPlayable &&
                   sourceCell.Tile != null &&
                   partnerCell.Tile != null &&
                   sourceCell.Tile.LogicType == TileLogicType.ChargedSweepBooster &&
                   partnerCell.Tile.LogicType == TileLogicType.ChargedSweepBooster &&
                   Math.Abs(sourceCell.X - partnerCell.X) + Math.Abs(sourceCell.Y - partnerCell.Y) == 1;
        }

        private static void ApplyChargedComboPurge(BoardModel board, CellModel sourceCell, CellModel partnerCell, BoardFxContext fxContext)
        {
            if (board == null)
            {
                return;
            }

            if (sourceCell?.Tile != null)
            {
                fxContext?.RecordActivate(sourceCell.Tile, sourceCell);
            }

            if (partnerCell?.Tile != null)
            {
                fxContext?.RecordActivate(partnerCell.Tile, partnerCell);
            }

            foreach (CellModel cell in board.GetAllCells())
            {
                if (cell == null || !cell.IsPlayable)
                {
                    continue;
                }

                if (cell.Overlay != null)
                {
                    ForceClearTile(board, cell, cell.Overlay, fxContext);
                }

                if (cell.Tile != null)
                {
                    ForceClearTile(board, cell, cell.Tile, fxContext);
                }
            }
        }

        private static void ForceClearTile(BoardModel board, CellModel cell, TileModel tile, BoardFxContext fxContext)
        {
            if (board == null || cell == null || tile == null)
            {
                return;
            }

            if (tile.CurrentHP > 0 && (tile.TileKind == TileKind.Blocker || tile.TileKind == TileKind.Target))
            {
                fxContext?.RecordDamage(tile, cell, tile.CurrentHP, 0, true);
            }

            fxContext?.RecordClear(tile, cell, false);
            cell.ClearTile(cell.GetTileLayer(tile) ?? TileStackLayer.Base);
            board.AddScore(10);
            fxContext?.RecordScore(tile, cell, 10, board.CurrentScore);
        }

        private readonly struct PendingSpecialCreate
        {
            public SpecialSpawnDecision Decision { get; }
            public TileModel SourceTile { get; }
            public CellModel Cell { get; }

            public PendingSpecialCreate(SpecialSpawnDecision decision, TileModel sourceTile, CellModel cell)
            {
                Decision = decision;
                SourceTile = sourceTile;
                Cell = cell;
            }
        }

        private readonly struct ChocolateGrowthCandidate
        {
            public CellModel SourceCell { get; }
            public CellModel TargetCell { get; }
            public TileModel SourceTile { get; }

            public ChocolateGrowthCandidate(CellModel sourceCell, CellModel targetCell, TileModel sourceTile)
            {
                SourceCell = sourceCell;
                TargetCell = targetCell;
                SourceTile = sourceTile;
            }
        }

        private static bool IsLineSlideValid(BoardModel board, List<CellModel> cells, MoveAxis axis)
        {
            if (cells == null || cells.Count <= 1)
            {
                return false;
            }

            bool hasAnyMovableState = false;
            for (int i = 0; i < cells.Count; i++)
            {
                CellModel cell = cells[i];
                if (cell == null) continue;

                if (cell.LocksLine(axis))
                {
                    return false;
                }

                if (cell.Tile != null)
                {
                    hasAnyMovableState = true;
                    if (!cell.Tile.CanBeMoved())
                    {
                        return false;
                    }
                }

                if (cell.Overlay != null)
                {
                    hasAnyMovableState = true;
                    if (!cell.Overlay.CanBeMoved())
                    {
                        return false;
                    }
                }
            }

            return hasAnyMovableState;
        }

        public static bool HasPossibleMoves(BoardModel board, Match.IBoardMatchRule matchRule)
        {
            if (board == null || matchRule == null)
            {
                return false;
            }

            // 0. Check for any activatable boosters on the board
            foreach (CellModel cell in board.GetAllCells())
            {
                if (cell != null && cell.IsPlayable && cell.CanTileActivate() && cell.Tile != null && cell.Tile.TileKind == TileKind.Booster)
                {
                    return true;
                }
            }

            // 1. Check rows
            for (int y = 0; y < board.Height; y++)
            {
                List<CellModel> cells = board.GetPlayableCellsInRow(y);
                if (!IsLineSlideValid(board, cells, MoveAxis.Row))
                {
                    continue;
                }

                int[] steps = { 1, -1 };
                foreach (int step in steps)
                {
                    board.RotateTiles(cells, step);
                    Match.BoardMatchAnalysis analysis = matchRule.Analyze(board);
                    bool hasMatches = analysis.HasMatches;
                    board.RotateTiles(cells, -step);

                    if (hasMatches)
                    {
                        return true;
                    }
                }
            }

            // 2. Check columns
            for (int x = 0; x < board.Width; x++)
            {
                List<CellModel> cells = board.GetPlayableCellsInColumn(x);
                if (!IsLineSlideValid(board, cells, MoveAxis.Column))
                {
                    continue;
                }

                int[] steps = { 1, -1 };
                foreach (int step in steps)
                {
                    board.RotateTiles(cells, step);
                    Match.BoardMatchAnalysis analysis = matchRule.Analyze(board);
                    bool hasMatches = analysis.HasMatches;
                    board.RotateTiles(cells, -step);

                    if (hasMatches)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static BoardMoveRequest? FindPossibleMove(BoardModel board, Match.IBoardMatchRule matchRule)
        {
            if (board == null || matchRule == null)
            {
                return null;
            }

            List<BoardMoveRequest> candidates = new List<BoardMoveRequest>();

            // 0. Check for any activatable boosters on the board
            foreach (CellModel cell in board.GetAllCells())
            {
                if (cell != null && cell.IsPlayable && cell.CanTileActivate() && cell.Tile != null && cell.Tile.TileKind == TileKind.Booster)
                {
                    candidates.Add(new BoardMoveRequest(MoveAxis.Row, -1, LineSlideDirection.Left, cell.X, cell.Y));
                }
            }

            // 1. Check rows
            for (int y = 0; y < board.Height; y++)
            {
                List<CellModel> cells = board.GetPlayableCellsInRow(y);
                if (!IsLineSlideValid(board, cells, MoveAxis.Row))
                {
                    continue;
                }

                int[] steps = { 1, -1 };
                foreach (int step in steps)
                {
                    board.RotateTiles(cells, step);
                    Match.BoardMatchAnalysis analysis = matchRule.Analyze(board);
                    bool hasMatches = analysis.HasMatches;
                    board.RotateTiles(cells, -step);

                    if (hasMatches)
                    {
                        LineSlideDirection direction = (step == 1) ? LineSlideDirection.Right : LineSlideDirection.Left;
                        BoardMoveRequest req = new BoardMoveRequest(MoveAxis.Row, y, direction);
                        if (!candidates.Contains(req))
                        {
                            candidates.Add(req);
                        }
                    }
                }
            }

            // 2. Check columns
            for (int x = 0; x < board.Width; x++)
            {
                List<CellModel> cells = board.GetPlayableCellsInColumn(x);
                if (!IsLineSlideValid(board, cells, MoveAxis.Column))
                {
                    continue;
                }

                int[] steps = { 1, -1 };
                foreach (int step in steps)
                {
                    board.RotateTiles(cells, step);
                    Match.BoardMatchAnalysis analysis = matchRule.Analyze(board);
                    bool hasMatches = analysis.HasMatches;
                    board.RotateTiles(cells, -step);

                    if (hasMatches)
                    {
                        LineSlideDirection direction = (step == 1) ? LineSlideDirection.Down : LineSlideDirection.Up;
                        BoardMoveRequest req = new BoardMoveRequest(MoveAxis.Column, x, direction);
                        if (!candidates.Contains(req))
                        {
                            candidates.Add(req);
                        }
                    }
                }
            }

            if (candidates.Count > 0)
            {
                int index = UnityEngine.Random.Range(0, candidates.Count);
                return candidates[index];
            }

            return null;
        }
    }
}

