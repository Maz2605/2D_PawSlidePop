using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Contracts;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Mapping;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Model;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Services
{
    public sealed class LevelAssemblyService
    {
        public Match3LevelData Build(LevelEditorBoardState board, LevelGenerationRequest request)
        {
            LevelEditorMetadata metadata = new LevelEditorMetadata
            {
                levelId = request.levelId,
                displayLevelNumber = request.displayLevelNumber,
                movesLimit = request.movesLimit,
                spawnableTileIds = new List<int>(request.allowedSpawnableTileIds),
                targets = new List<LevelTargetRequirement>(request.targets)
            };

            return LevelEditorSerializationMapper.ToLevelData(board, metadata);
        }
    }
}
