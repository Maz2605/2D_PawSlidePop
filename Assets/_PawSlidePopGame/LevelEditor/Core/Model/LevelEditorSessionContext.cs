using System;
using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Validation;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Model
{
    [Serializable]
    public sealed class LevelEditorSessionContext
    {
        public string levelId = "Level_001";
        public int displayLevelNumber = 1;
        public int movesLimit = 26;
        public LevelEditorBoardState board = new LevelEditorBoardState();
        public int[] cellArtLayout = Array.Empty<int>();
        public List<int> spawnableTileIds = new List<int>();
        public List<LevelSpawnableTileConfig> spawnableTileConfigs = new List<LevelSpawnableTileConfig>();
        public float targetSpawnBias = 1.25f;
        public bool enableDynamicBalancing = true;
        public bool isDirty;
        public string statusMessage = string.Empty;
        public string errorMessage = string.Empty;

        [NonSerialized] public Match3LevelDataValidationResult lastValidationResult;

        public LevelEditorSessionContext Clone()
        {
            return new LevelEditorSessionContext
            {
                levelId = levelId,
                displayLevelNumber = displayLevelNumber,
                movesLimit = movesLimit,
                board = board != null ? board.Clone() : new LevelEditorBoardState(),
                cellArtLayout = cellArtLayout != null ? (int[])cellArtLayout.Clone() : Array.Empty<int>(),
                spawnableTileIds = new List<int>(spawnableTileIds),
                spawnableTileConfigs = spawnableTileConfigs != null ? new List<LevelSpawnableTileConfig>(spawnableTileConfigs) : new List<LevelSpawnableTileConfig>(),
                targetSpawnBias = targetSpawnBias,
                enableDynamicBalancing = enableDynamicBalancing,
                isDirty = isDirty,
                statusMessage = statusMessage,
                errorMessage = errorMessage,
                lastValidationResult = lastValidationResult
            };
        }
    }
}
