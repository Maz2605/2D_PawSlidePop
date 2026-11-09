using System;
using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Entities;
using _PawSlidePopGame._Scripts.Feature.Match3.Presentation;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Logic.Resolution
{
    public static class BoardRefillService
    {
        public static int Apply(
            BoardModel board,
            Match3LevelData levelData,
            Match3TileDatabaseSO tileDatabase,
            Random random,
            IList<TileSpawnOp> spawnOps = null)
        {
            int spawnedTiles = 0;
            Random rng = random ?? new Random();

            for (int x = 0; x < board.Width; x++)
            {
                int spawnOffset = 0;
                for (int y = board.Height - 1; y >= 0; y--)
                {
                    CellModel cell = board.GetCell(x, y);
                    if (cell == null || !cell.IsPlayable || cell.HasTile())
                    {
                        continue;
                    }

                    int tileId = PickSpawnTileId(board, x, y, levelData, tileDatabase, rng);
                    if (tileId == 0)
                    {
                        continue;
                    }

                    TileModel tile = board.CreateTileFromDefinitionId(tileId, tileDatabase);
                    if (tile == null)
                    {
                        continue;
                    }

                    board.SetTile(cell, tile);
                    spawnOffset++;
                    spawnOps?.Add(new TileSpawnOp
                    {
                        TileInstanceId = tile.InstanceId,
                        TileId = tile.TileId,
                        Layer = TileStackLayer.Base,
                        Definition = tile.Definition,
                        SpawnFromRowAboveBoard = -spawnOffset,
                        ToCell = new BoardCellPosition(cell.X, cell.Y)
                    });
                    spawnedTiles++;
                }
            }

            return spawnedTiles;
        }

        private static int PickSpawnTileId(BoardModel board, int x, int y, Match3LevelData levelData, Match3TileDatabaseSO tileDatabase, Random random)
        {
            List<int> restrictedIds = new List<int>(2);
            AddNeighborRestriction(board.GetCell(x - 1, y), board.GetCell(x - 2, y), restrictedIds);
            AddNeighborRestriction(board.GetCell(x, y + 1), board.GetCell(x, y + 2), restrictedIds);

            if (levelData != null)
            {
                if (levelData.spawnableTileConfigs != null && levelData.spawnableTileConfigs.Count > 0)
                {
                    List<int> filteredIds = new List<int>();
                    for (int i = 0; i < levelData.spawnableTileConfigs.Count; i++)
                    {
                        var config = levelData.spawnableTileConfigs[i];
                        if (config.enabled && IsValidNormalSpawnable(config.tileId, tileDatabase) && !IsRestricted(config.tileId, restrictedIds))
                        {
                            filteredIds.Add(config.tileId);
                        }
                    }

                    if (filteredIds.Count > 0)
                    {
                        return PickSpawnTileIdWithWeights(board, filteredIds, levelData, tileDatabase, random);
                    }
                }
                else if (levelData.spawnableTileIds != null && levelData.spawnableTileIds.Count > 0)
                {
                    List<int> filteredIds = new List<int>();
                    for (int i = 0; i < levelData.spawnableTileIds.Count; i++)
                    {
                        int candidateId = levelData.spawnableTileIds[i];
                        if (IsValidNormalSpawnable(candidateId, tileDatabase) && !IsRestricted(candidateId, restrictedIds))
                        {
                            filteredIds.Add(candidateId);
                        }
                    }

                    if (filteredIds.Count > 0)
                    {
                        return PickSpawnTileIdWithWeights(board, filteredIds, levelData, tileDatabase, random);
                    }
                }
            }

            return tileDatabase != null ? tileDatabase.GetRandomSpawnableTileId(random, restrictedIds) : 0;
        }

        private static int PickSpawnTileIdWithWeights(
            BoardModel board,
            List<int> candidateIds,
            Match3LevelData levelData,
            Match3TileDatabaseSO tileDatabase,
            Random random)
        {
            Dictionary<int, int> boardCounts = new Dictionary<int, int>();
            for (int i = 0; i < candidateIds.Count; i++)
            {
                boardCounts[candidateIds[i]] = 0;
            }

            int totalTilesCount = 0;
            foreach (CellModel cell in board.GetAllCells())
            {
                if (cell != null && cell.Tile != null)
                {
                    int tileId = cell.Tile.TileId;
                    if (boardCounts.ContainsKey(tileId))
                    {
                        boardCounts[tileId]++;
                    }
                    totalTilesCount++;
                }
            }

            HashSet<int> targetTileIds = new HashSet<int>();
            if (levelData != null && levelData.targets != null)
            {
                for (int i = 0; i < levelData.targets.Count; i++)
                {
                    targetTileIds.Add(levelData.targets[i].tileId);
                }
            }

            double[] weights = new double[candidateIds.Count];
            double totalWeight = 0;

            for (int i = 0; i < candidateIds.Count; i++)
            {
                int tileId = candidateIds[i];
                
                // 1. Get baseWeight from LevelSpawnableTileConfig if present, otherwise fall back to database
                double baseWeight = 1.0;
                bool foundConfig = false;
                if (levelData != null && levelData.spawnableTileConfigs != null)
                {
                    for (int j = 0; j < levelData.spawnableTileConfigs.Count; j++)
                    {
                        if (levelData.spawnableTileConfigs[j].tileId == tileId)
                        {
                            baseWeight = levelData.spawnableTileConfigs[j].weight;
                            foundConfig = true;
                            break;
                        }
                    }
                }

                if (!foundConfig)
                {
                    BoardContentDefinitionSO definition = tileDatabase.GetContentDefinition(tileId);
                    baseWeight = definition != null ? definition.SpawnWeight : 1.0;
                }

                if (baseWeight <= 0)
                {
                    baseWeight = 1.0;
                }

                // 2. Calculate balance factor if enableDynamicBalancing is true
                double balanceFactor = 1.0;
                if (levelData == null || levelData.enableDynamicBalancing)
                {
                    double count = boardCounts[tileId];
                    double proportion = totalTilesCount > 0 ? count / (double)totalTilesCount : 0.0;
                    double idealProportion = 1.0 / candidateIds.Count;

                    if (proportion > idealProportion)
                    {
                        balanceFactor = Math.Max(0.2, 1.0 - (proportion - idealProportion) * 2.0);
                    }
                    else if (proportion < idealProportion)
                    {
                        balanceFactor = Math.Min(1.8, 1.0 + (idealProportion - proportion) * 2.0);
                    }
                }

                // 3. Apply custom targetSpawnBias instead of hardcoded 1.25f
                float biasValue = levelData != null ? levelData.targetSpawnBias : 1.25f;
                double objectiveBias = targetTileIds.Contains(tileId) ? biasValue : 1.0;
                double finalWeight = baseWeight * balanceFactor * objectiveBias;
                
                weights[i] = finalWeight;
                totalWeight += finalWeight;
            }

            if (totalWeight <= 0)
            {
                return candidateIds[random.Next(0, candidateIds.Count)];
            }

            double roll = random.NextDouble() * totalWeight;
            double cumulative = 0;
            for (int i = 0; i < candidateIds.Count; i++)
            {
                cumulative += weights[i];
                if (roll <= cumulative)
                {
                    return candidateIds[i];
                }
            }

            return candidateIds[candidateIds.Count - 1];
        }

        private static bool IsValidNormalSpawnable(int tileId, Match3TileDatabaseSO tileDatabase)
        {
            if (tileId <= 0 || tileDatabase == null)
            {
                return false;
            }

            TileDefinitionSO definition = tileDatabase.GetTileDefinition(tileId);
            return definition != null &&
                   definition.TileKind == TileKind.Normal &&
                   definition.CanSpawnOnRefill &&
                   definition.SpawnWeight > 0;
        }

        private static void AddNeighborRestriction(CellModel first, CellModel second, List<int> restrictedIds)
        {
            if (first?.Tile == null || second?.Tile == null)
            {
                return;
            }

            if (!first.Tile.IsMatchableWith(second.Tile))
            {
                return;
            }

            restrictedIds.Add(first.Tile.TileId);
        }

        private static bool IsRestricted(int tileId, List<int> restrictedIds)
        {
            for (int i = 0; i < restrictedIds.Count; i++)
            {
                if (restrictedIds[i] == tileId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

