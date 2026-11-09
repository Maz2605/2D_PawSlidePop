using System;
using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Data.LevelProvider;

namespace _PawSlidePopGame._Scripts.Gameplay.Meta.MapManager
{
    [Serializable]
    public sealed class LevelProgressEntryData
    {
        public string levelId;
        public int bestStars;
        public int bestScore;
        public bool isCompleted;

        public void Sanitize()
        {
            levelId = LevelPathUtility.SanitizeLevelId(levelId);
            bestStars = Math.Max(0, bestStars);
            bestScore = Math.Max(0, bestScore);
            isCompleted = isCompleted || bestStars > 0;
        }
    }

    [Serializable]
    public sealed class LevelProgressSaveData
    {
        public string currentLevelId = "Level_001";
        public int highestUnlockedLevelNumber = 1;
        public List<LevelProgressEntryData> levels = new List<LevelProgressEntryData>();

        public void Sanitize()
        {
            currentLevelId = string.IsNullOrWhiteSpace(currentLevelId)
                ? "Level_001"
                : LevelPathUtility.SanitizeLevelId(currentLevelId);

            if (!MapManager.TryParseLevelNumber(currentLevelId, out int currentLevelNumber))
            {
                currentLevelId = "Level_001";
                currentLevelNumber = 1;
            }

            highestUnlockedLevelNumber = Math.Max(1, Math.Max(highestUnlockedLevelNumber, currentLevelNumber));

            Dictionary<string, LevelProgressEntryData> merged = new Dictionary<string, LevelProgressEntryData>(StringComparer.OrdinalIgnoreCase);
            if (levels != null)
            {
                for (int i = 0; i < levels.Count; i++)
                {
                    LevelProgressEntryData entry = levels[i];
                    if (entry == null)
                    {
                        continue;
                    }

                    entry.Sanitize();
                    if (string.IsNullOrWhiteSpace(entry.levelId))
                    {
                        continue;
                    }

                    if (!merged.TryGetValue(entry.levelId, out LevelProgressEntryData existing))
                    {
                        merged.Add(entry.levelId, entry);
                        continue;
                    }

                    existing.bestStars = Math.Max(existing.bestStars, entry.bestStars);
                    existing.bestScore = Math.Max(existing.bestScore, entry.bestScore);
                    existing.isCompleted = existing.isCompleted || entry.isCompleted;
                }
            }

            levels = new List<LevelProgressEntryData>(merged.Values);
            levels.Sort((a, b) => string.Compare(a.levelId, b.levelId, StringComparison.OrdinalIgnoreCase));
        }
    }
}
