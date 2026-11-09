using System;
using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Model;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.Validation
{
    public static class Match3LevelDataValidator
    {
        public static Match3LevelDataValidationResult Validate(Match3LevelData levelData, Match3TileDatabaseSO database = null)
        {
            Match3LevelDataValidationResult result = new Match3LevelDataValidationResult();
            if (levelData == null)
            {
                result.Add(new Match3LevelDataValidationIssue(Match3LevelDataValidationSeverity.Error, "LEVEL_NULL", "Level data is null.", "levelData"));
                return result;
            }
            if (string.IsNullOrWhiteSpace(levelData.levelID))
            {
                result.Add(new Match3LevelDataValidationIssue(Match3LevelDataValidationSeverity.Error, "LEVEL_ID_EMPTY", "Level id is required.", "levelID"));
            }
            if (levelData.width <= 0 || levelData.height <= 0)
            {
                result.Add(new Match3LevelDataValidationIssue(Match3LevelDataValidationSeverity.Error, "BOARD_SIZE_INVALID", "Width and height must be greater than zero.", "width/height"));
            }
            int cellCount = Math.Max(0, levelData.width) * Math.Max(0, levelData.height);
            ValidateLayout(result, levelData.tileLayout, "tileLayout", cellCount, levelData.width);
            ValidateLayout(result, levelData.overlayLayout, "overlayLayout", cellCount, levelData.width);
            ValidateLayout(result, levelData.underlayLayout, "underlayLayout", cellCount, levelData.width);
            if (levelData.playableMask == null || levelData.playableMask.Length != cellCount)
            {
                result.Add(new Match3LevelDataValidationIssue(Match3LevelDataValidationSeverity.Error, "PLAYABLE_MASK_INVALID", "Playable mask must match cell count.", "playableMask"));
            }
            ValidateTargets(result, levelData.targets, database);
            ValidateSpawnables(result, levelData.spawnableTileIds, database);
            ValidateKnownContent(result, levelData.tileLayout, "tileLayout", levelData.width, database);
            ValidateKnownContent(result, levelData.overlayLayout, "overlayLayout", levelData.width, database);
            ValidateKnownContent(result, levelData.underlayLayout, "underlayLayout", levelData.width, database);
            ValidateNoInitialMatches(result, levelData);
            return result;
        }

        private static void ValidateLayout(Match3LevelDataValidationResult result, int[] layout, string fieldName, int cellCount, int width)
        {
            if (layout == null || layout.Length != cellCount)
            {
                result.Add(new Match3LevelDataValidationIssue(Match3LevelDataValidationSeverity.Error, $"{fieldName.ToUpperInvariant()}_INVALID", $"{fieldName} must match cell count {cellCount}.", fieldName));
                return;
            }

            for (int i = 0; i < layout.Length; i++)
            {
                if (layout[i] < 0)
                {
                    result.Add(new Match3LevelDataValidationIssue(Match3LevelDataValidationSeverity.Error, "CONTENT_NEGATIVE", $"{fieldName}[{i}] cannot be negative.", fieldName, i, CreateCoordinate(i, width)));
                }
            }
        }

        private static void ValidateTargets(Match3LevelDataValidationResult result, List<LevelTargetData> targets, Match3TileDatabaseSO database)
        {
            if (targets == null || targets.Count == 0)
            {
                result.Add(new Match3LevelDataValidationIssue(Match3LevelDataValidationSeverity.Warning, "TARGETS_EMPTY", "Targets are empty.", "targets"));
                return;
            }

            for (int i = 0; i < targets.Count; i++)
            {
                LevelTargetData target = targets[i];
                if (target.tileId <= 0 || target.requiredCount <= 0)
                {
                    result.Add(new Match3LevelDataValidationIssue(Match3LevelDataValidationSeverity.Error, "TARGET_INVALID", $"Target at index {i} is invalid.", $"targets[{i}]"));
                    continue;
                }

                if (database == null)
                {
                    continue;
                }

                BoardContentDefinitionSO definition = database.GetTargetContentDefinition(target.tileId);
                if (definition == null)
                {
                    result.Add(new Match3LevelDataValidationIssue(Match3LevelDataValidationSeverity.Error, "TARGET_UNKNOWN", $"Target tile id {target.tileId} is unknown.", $"targets[{i}]"));
                    continue;
                }

                if (definition.Icon == null)
                {
                    result.Add(new Match3LevelDataValidationIssue(Match3LevelDataValidationSeverity.Warning, "TARGET_ICON_MISSING", $"Target tile id {target.tileId} has no icon assigned for HUD display.", $"targets[{i}]"));
                }

                if (!database.IsTargetObjectiveSupported(target.tileId, out _, out string reason))
                {
                    string message = string.IsNullOrWhiteSpace(reason)
                        ? $"Target tile id {target.tileId} is not supported as a collectible objective."
                        : $"Target tile id {target.tileId} is not supported as a collectible objective. {reason}";
                    result.Add(new Match3LevelDataValidationIssue(Match3LevelDataValidationSeverity.Error, "TARGET_UNSUPPORTED", message, $"targets[{i}]"));
                }
            }
        }

        private static void ValidateSpawnables(Match3LevelDataValidationResult result, List<int> spawnables, Match3TileDatabaseSO database)
        {
            if (spawnables == null || spawnables.Count == 0)
            {
                result.Add(new Match3LevelDataValidationIssue(Match3LevelDataValidationSeverity.Error, "SPAWNABLES_EMPTY", "Spawnable tile ids are empty.", "spawnableTileIds"));
                return;
            }

            HashSet<int> seen = new HashSet<int>();
            for (int i = 0; i < spawnables.Count; i++)
            {
                int tileId = spawnables[i];
                if (tileId <= 0)
                {
                    result.Add(new Match3LevelDataValidationIssue(Match3LevelDataValidationSeverity.Error, "SPAWNABLE_INVALID", $"Spawnable tile id at index {i} is invalid.", $"spawnableTileIds[{i}]"));
                    continue;
                }

                if (!seen.Add(tileId))
                {
                    result.Add(new Match3LevelDataValidationIssue(Match3LevelDataValidationSeverity.Warning, "SPAWNABLE_DUPLICATE", $"Spawnable tile id {tileId} is duplicated.", $"spawnableTileIds[{i}]"));
                }

                if (database != null && database.GetTileDefinition(tileId) == null)
                {
                    result.Add(new Match3LevelDataValidationIssue(Match3LevelDataValidationSeverity.Error, "SPAWNABLE_UNKNOWN", $"Spawnable tile id {tileId} is unknown.", $"spawnableTileIds[{i}]"));
                }
            }
        }

        private static void ValidateKnownContent(Match3LevelDataValidationResult result, int[] layout, string fieldName, int width, Match3TileDatabaseSO database)
        {
            if (database == null || layout == null)
            {
                return;
            }

            for (int i = 0; i < layout.Length; i++)
            {
                int tileId = layout[i];
                if (tileId <= 0)
                {
                    continue;
                }

                if (database.GetContentDefinition(tileId) == null)
                {
                    result.Add(new Match3LevelDataValidationIssue(Match3LevelDataValidationSeverity.Error, "CONTENT_UNKNOWN", $"Unknown content id {tileId} in {fieldName}.", fieldName, i, CreateCoordinate(i, width)));
                }
            }
        }

        private static void ValidateNoInitialMatches(Match3LevelDataValidationResult result, Match3LevelData levelData)
        {
            if (levelData.tileLayout == null)
            {
                return;
            }

            for (int y = 0; y < levelData.height; y++)
            {
                for (int x = 0; x < levelData.width; x++)
                {
                    int index = (y * levelData.width) + x;
                    int tileId = levelData.tileLayout[index];
                    if (tileId <= 0)
                    {
                        continue;
                    }

                    if (x >= 2)
                    {
                        int left1 = levelData.tileLayout[index - 1];
                        int left2 = levelData.tileLayout[index - 2];
                        if (left1 == tileId && left2 == tileId)
                        {
                            result.Add(new Match3LevelDataValidationIssue(Match3LevelDataValidationSeverity.Error, "INITIAL_MATCH", "Board contains an initial horizontal match.", "tileLayout", index, new LevelEditorCoordinate(x, y)));
                            return;
                        }
                    }

                    if (y >= 2)
                    {
                        int up1 = levelData.tileLayout[index - levelData.width];
                        int up2 = levelData.tileLayout[index - (levelData.width * 2)];
                        if (up1 == tileId && up2 == tileId)
                        {
                            result.Add(new Match3LevelDataValidationIssue(Match3LevelDataValidationSeverity.Error, "INITIAL_MATCH", "Board contains an initial vertical match.", "tileLayout", index, new LevelEditorCoordinate(x, y)));
                            return;
                        }
                    }
                }
            }
        }

        private static LevelEditorCoordinate CreateCoordinate(int index, int width)
        {
            return width > 0 ? new LevelEditorCoordinate(index % width, index / width) : new LevelEditorCoordinate(0, 0);
        }
    }
}
