using System.Collections.Generic;
using System.Linq;
using _PawSlidePopGame._Scripts.Data.LevelProvider;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Contracts;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Model;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Persistence;
using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Mapping
{
    public static class LevelEditorDocumentMapper
    {
        public static LevelEditorSessionContext CreateNew(
            string levelId, 
            int displayLevelNumber, 
            int width, 
            int height, 
            int movesLimit, 
            IReadOnlyList<int> spawnableTileIds = null,
            IReadOnlyList<LevelSpawnableTileConfig> spawnableTileConfigs = null)
        {
            LevelEditorBoardState board = new LevelEditorBoardState();
            board.Initialize(width, height);

            return new LevelEditorSessionContext
            {
                levelId = string.IsNullOrWhiteSpace(levelId) ? "Level_001" : levelId.Trim(),
                displayLevelNumber = displayLevelNumber > 0 ? displayLevelNumber : 1,
                movesLimit = movesLimit > 0 ? movesLimit : 1,
                board = board,
                cellArtLayout = new int[board.CellCount],
                spawnableTileIds = spawnableTileIds != null ? new List<int>(spawnableTileIds) : new List<int>(),
                spawnableTileConfigs = spawnableTileConfigs != null ? new List<LevelSpawnableTileConfig>(spawnableTileConfigs) : new List<LevelSpawnableTileConfig>(),
                targetSpawnBias = 1.25f,
                enableDynamicBalancing = true,
                isDirty = false
            };
        }

        public static LevelEditorSessionContext LoadFromDocument(LevelSaveData document)
        {
            Match3LevelData levelData = document?.levelData ?? new Match3LevelData();
            LevelEditorBoardState board = new LevelEditorBoardState();
            board.Initialize(levelData.width, levelData.height);

            CopyLayout(levelData.tileLayout, board.tileLayout);
            CopyLayout(levelData.overlayLayout, board.overlayLayout);
            CopyLayout(levelData.underlayLayout, board.underlayLayout);
            CopyMask(levelData.playableMask, board.playableMask);

            int[] cellArtLayout = new int[board.CellCount];
            if (levelData.cellArtLayout != null)
            {
                CopyLayout(levelData.cellArtLayout, cellArtLayout);
            }

            return new LevelEditorSessionContext
            {
                levelId = string.IsNullOrWhiteSpace(document?.levelId) ? levelData.levelID : document.levelId,
                displayLevelNumber = levelData.DisplayLevelNumber,
                movesLimit = levelData.movesLimit > 0 ? levelData.movesLimit : 1,
                board = board,
                cellArtLayout = cellArtLayout,
                spawnableTileIds = levelData.spawnableTileIds != null ? new List<int>(levelData.spawnableTileIds) : new List<int>(),
                spawnableTileConfigs = levelData.spawnableTileConfigs != null ? new List<LevelSpawnableTileConfig>(levelData.spawnableTileConfigs) : new List<LevelSpawnableTileConfig>(),
                targetSpawnBias = levelData.targetSpawnBias,
                enableDynamicBalancing = levelData.enableDynamicBalancing,
                isDirty = false
            };
        }

        public static LevelSaveData BuildDocument(
            LevelEditorSessionContext context,
            Match3TileDatabaseSO database,
            IReadOnlyList<LevelTargetRequirement> targets,
            int schemaVersion = 1)
        {
            // Sync session context spawnables with resolved spawnables from board
            context.spawnableTileIds = ResolveSpawnableTileIds(context, database);
            context.spawnableTileConfigs = SyncSpawnableTileConfigs(context.spawnableTileConfigs, context.spawnableTileIds);

            Match3LevelData levelData = new Match3LevelData
            {
                levelID = LevelPathUtility.SanitizeLevelId(context.levelId),
                displayLevelNumber = context.displayLevelNumber > 0 ? context.displayLevelNumber : 1,
                width = context.board.width,
                height = context.board.height,
                movesLimit = context.movesLimit > 0 ? context.movesLimit : 1,
                tileLayout = (int[])context.board.tileLayout.Clone(),
                overlayLayout = (int[])context.board.overlayLayout.Clone(),
                underlayLayout = (int[])context.board.underlayLayout.Clone(),
                playableMask = (bool[])context.board.playableMask.Clone(),
                cellArtLayout = context.cellArtLayout != null ? (int[])context.cellArtLayout.Clone() : new int[context.board.CellCount],
                spawnableTileIds = new List<int>(context.spawnableTileIds),
                spawnableTileConfigs = new List<LevelSpawnableTileConfig>(context.spawnableTileConfigs),
                targetSpawnBias = context.targetSpawnBias,
                enableDynamicBalancing = context.enableDynamicBalancing,
                targets = BuildTargets(targets)
            };

            return LevelEditorDocumentFactory.Create(levelData, schemaVersion);
        }

        private static List<LevelSpawnableTileConfig> SyncSpawnableTileConfigs(List<LevelSpawnableTileConfig> existingConfigs, List<int> activeIds)
        {
            List<LevelSpawnableTileConfig> result = new List<LevelSpawnableTileConfig>();
            if (activeIds == null)
            {
                return result;
            }

            Dictionary<int, LevelSpawnableTileConfig> existingLookup = new Dictionary<int, LevelSpawnableTileConfig>();
            if (existingConfigs != null)
            {
                for (int i = 0; i < existingConfigs.Count; i++)
                {
                    existingLookup[existingConfigs[i].tileId] = existingConfigs[i];
                }
            }

            for (int i = 0; i < activeIds.Count; i++)
            {
                int id = activeIds[i];
                if (existingLookup.TryGetValue(id, out LevelSpawnableTileConfig config))
                {
                    result.Add(config);
                }
                else
                {
                    result.Add(new LevelSpawnableTileConfig(id, 100, true));
                }
            }

            return result;
        }

        private static List<int> ResolveSpawnableTileIds(LevelEditorSessionContext context, Match3TileDatabaseSO database)
        {
            List<int> spawnables = new List<int>();

            // 1. Gather all normal tiles currently present on the board
            if (context.board?.tileLayout != null && database != null)
            {
                for (int i = 0; i < context.board.tileLayout.Length; i++)
                {
                    int tileId = context.board.tileLayout[i];
                    if (tileId <= 0)
                    {
                        continue;
                    }

                    TileDefinitionSO definition = database.GetTileDefinition(tileId);
                    if (definition != null && definition.TileKind == TileKind.Normal && definition.CanSpawnOnRefill)
                    {
                        if (!spawnables.Contains(tileId))
                        {
                            spawnables.Add(tileId);
                        }
                    }
                }
            }

            // 2. Fallback: If no normal tiles are on the board, use existing context spawnables or all database normal tiles
            if (spawnables.Count == 0)
            {
                if (context.spawnableTileIds != null)
                {
                    for (int i = 0; i < context.spawnableTileIds.Count; i++)
                    {
                        int tileId = context.spawnableTileIds[i];
                        if (tileId > 0 && !spawnables.Contains(tileId))
                        {
                            spawnables.Add(tileId);
                        }
                    }
                }

                if (spawnables.Count == 0 && database != null)
                {
                    IReadOnlyList<BoardContentDefinitionSO> definitions = database.Tiles;
                    for (int i = 0; i < definitions.Count; i++)
                    {
                        if (definitions[i] is TileDefinitionSO tileDefinition &&
                            tileDefinition.TileKind == TileKind.Normal &&
                            tileDefinition.CanSpawnOnRefill &&
                            tileDefinition.SpawnWeight > 0 &&
                            !spawnables.Contains(tileDefinition.TileId))
                        {
                            spawnables.Add(tileDefinition.TileId);
                        }
                    }
                }
            }

            return spawnables;
        }

        public static List<LevelTargetRequirement> LoadTargetsFromDocument(LevelSaveData document)
        {
            Match3LevelData levelData = document?.levelData;
            return levelData?.targets != null
                ? levelData.targets.Select(target => new LevelTargetRequirement(target.tileId, target.requiredCount)).ToList()
                : new List<LevelTargetRequirement>();
        }

        private static List<LevelTargetData> BuildTargets(IReadOnlyList<LevelTargetRequirement> goals)
        {
            List<LevelTargetData> targets = new List<LevelTargetData>();
            if (goals == null)
            {
                return targets;
            }

            for (int i = 0; i < goals.Count; i++)
            {
                LevelTargetRequirement goal = goals[i];
                if (goal.tileId <= 0 || goal.requiredCount <= 0)
                {
                    continue;
                }

                targets.Add(new LevelTargetData(goal.tileId, goal.requiredCount));
            }

            return targets;
        }

        private static void CopyLayout(int[] source, int[] destination)
        {
            if (source == null || destination == null)
            {
                return;
            }

            int count = source.Length < destination.Length ? source.Length : destination.Length;
            for (int i = 0; i < count; i++)
            {
                destination[i] = source[i] < 0 ? 0 : source[i];
            }
        }

        private static void CopyMask(bool[] source, bool[] destination)
        {
            if (destination == null)
            {
                return;
            }

            if (source == null)
            {
                for (int i = 0; i < destination.Length; i++)
                {
                    destination[i] = true;
                }

                return;
            }

            int count = source.Length < destination.Length ? source.Length : destination.Length;
            for (int i = 0; i < count; i++)
            {
                destination[i] = source[i];
            }
        }
    }
}
