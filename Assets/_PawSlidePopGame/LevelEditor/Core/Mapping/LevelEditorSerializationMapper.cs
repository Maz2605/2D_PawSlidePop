using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Contracts;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Model;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Mapping
{
    public static class LevelEditorSerializationMapper
    {
        public static Match3LevelData ToLevelData(LevelEditorBoardState board, LevelEditorMetadata metadata)
        {
            Match3LevelData levelData = new Match3LevelData
            {
                levelID = metadata.levelId,
                displayLevelNumber = metadata.displayLevelNumber,
                width = board.width,
                height = board.height,
                movesLimit = metadata.movesLimit,
                tileLayout = (int[])board.tileLayout.Clone(),
                overlayLayout = (int[])board.overlayLayout.Clone(),
                underlayLayout = (int[])board.underlayLayout.Clone(),
                playableMask = (bool[])board.playableMask.Clone(),
                spawnableTileIds = new List<int>(metadata.spawnableTileIds),
                targets = ToTargetData(metadata.targets),
                starScoreThresholds = new[] { 100, 250, 500, 1500 },
                chargedAbility = new Match3LevelData.ChargedAbilityConfig()
            };

            return levelData;
        }

        private static List<LevelTargetData> ToTargetData(IReadOnlyList<LevelTargetRequirement> requirements)
        {
            List<LevelTargetData> targets = new List<LevelTargetData>();
            if (requirements == null)
            {
                return targets;
            }

            for (int i = 0; i < requirements.Count; i++)
            {
                targets.Add(new LevelTargetData(requirements[i].tileId, requirements[i].requiredCount));
            }

            return targets;
        }
    }
}
