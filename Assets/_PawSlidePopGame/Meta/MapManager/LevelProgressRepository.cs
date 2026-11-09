using System;
using _PawSlidePopGame._Scripts.Data.LevelProvider;
using _PawSlidePopGame._Scripts.Data.SaveSystem;

namespace _PawSlidePopGame._Scripts.Gameplay.Meta.MapManager
{
    public sealed class LevelProgressRepository
    {
        public const string DefaultSaveKey = "level_progress";

        private static readonly Lazy<LevelProgressRepository> LazyInstance =
            new Lazy<LevelProgressRepository>(() => new LevelProgressRepository(DefaultSaveKey));

        private readonly string _saveKey;
        private LevelProgressSaveData _data;

        public static event Action OnDataChanged;

        public static LevelProgressRepository Instance => LazyInstance.Value;
        public LevelProgressSaveData Data => _data ??= Load();

        public LevelProgressRepository(string saveKey)
        {
            _saveKey = string.IsNullOrWhiteSpace(saveKey) ? DefaultSaveKey : saveKey;
            SaveSystem.OnSaveSynced += HandleSaveSynced;
        }

        private void HandleSaveSynced(string key)
        {
            if (key == _saveKey)
            {
                Reload();
            }
        }

        public void Reload()
        {
            _data = Load();
            OnDataChanged?.Invoke();
        }

        public string GetCurrentLevelId(string fallbackLevelId = "Level_001")
        {
            LevelProgressSaveData data = Data;
            string sanitizedFallback = string.IsNullOrWhiteSpace(fallbackLevelId)
                ? "Level_001"
                : LevelPathUtility.SanitizeLevelId(fallbackLevelId);

            if (string.IsNullOrWhiteSpace(data.currentLevelId))
            {
                data.currentLevelId = sanitizedFallback;
                Save();
            }

            return data.currentLevelId;
        }

        public int GetHighestUnlockedLevelNumber()
        {
            return Math.Max(1, Data.highestUnlockedLevelNumber);
        }

        public bool IsLevelUnlocked(string levelId)
        {
            string sanitizedLevelId = LevelPathUtility.SanitizeLevelId(levelId);
            if (string.IsNullOrWhiteSpace(sanitizedLevelId))
            {
                return false;
            }

            return MapManager.TryParseLevelNumber(sanitizedLevelId, out int levelNumber) &&
                   levelNumber <= GetHighestUnlockedLevelNumber();
        }

        public void EnsureInitializedProgress(string fallbackLevelId = "Level_001")
        {
            LevelProgressSaveData data = Data;
            string sanitizedFallback = string.IsNullOrWhiteSpace(fallbackLevelId)
                ? "Level_001"
                : LevelPathUtility.SanitizeLevelId(fallbackLevelId);

            bool changed = false;
            if (string.IsNullOrWhiteSpace(data.currentLevelId))
            {
                data.currentLevelId = sanitizedFallback;
                changed = true;
            }

            if (!MapManager.TryParseLevelNumber(data.currentLevelId, out int currentLevelNumber))
            {
                data.currentLevelId = sanitizedFallback;
                currentLevelNumber = 1;
                changed = true;
            }

            if (data.highestUnlockedLevelNumber < currentLevelNumber)
            {
                data.highestUnlockedLevelNumber = currentLevelNumber;
                changed = true;
            }

            if (changed)
            {
                Save();
            }
        }

        public LevelProgressEntryData GetProgress(string levelId)
        {
            string sanitizedLevelId = LevelPathUtility.SanitizeLevelId(levelId);
            if (string.IsNullOrWhiteSpace(sanitizedLevelId))
            {
                return null;
            }

            LevelProgressSaveData data = Data;
            for (int i = 0; i < data.levels.Count; i++)
            {
                LevelProgressEntryData entry = data.levels[i];
                if (entry != null && string.Equals(entry.levelId, sanitizedLevelId, StringComparison.OrdinalIgnoreCase))
                {
                    return entry;
                }
            }

            return null;
        }

        public bool RecordLevelResult(string levelId, int reachedStars, int score)
        {
            string sanitizedLevelId = LevelPathUtility.SanitizeLevelId(levelId);
            if (string.IsNullOrWhiteSpace(sanitizedLevelId))
            {
                return false;
            }

            LevelProgressEntryData entry = GetOrCreateProgress(sanitizedLevelId);
            int sanitizedStars = Math.Max(0, reachedStars);
            int sanitizedScore = Math.Max(0, score);
            bool changed = false;

            if (sanitizedStars > entry.bestStars)
            {
                entry.bestStars = sanitizedStars;
                changed = true;
            }

            if (sanitizedScore > entry.bestScore)
            {
                entry.bestScore = sanitizedScore;
                changed = true;
            }

            if (!entry.isCompleted && sanitizedStars > 0)
            {
                entry.isCompleted = true;
                changed = true;
            }

            if (changed)
            {
                Save();
            }

            return changed;
        }

        public bool RecordWinAndAdvance(string levelId, int reachedStars, int score)
        {
            string sanitizedLevelId = LevelPathUtility.SanitizeLevelId(levelId);
            if (string.IsNullOrWhiteSpace(sanitizedLevelId))
            {
                return false;
            }

            EnsureInitializedProgress();

            bool changed = RecordLevelResult(sanitizedLevelId, reachedStars, score);
            LevelProgressSaveData data = Data;

            if (!string.Equals(data.currentLevelId, sanitizedLevelId, StringComparison.OrdinalIgnoreCase))
            {
                return changed;
            }

            if (!MapManager.TryParseLevelNumber(sanitizedLevelId, out int currentLevelNumber))
            {
                return changed;
            }

            int nextLevelNumber = currentLevelNumber + 1;
            string nextLevelId = BuildLevelIdFromTemplate(sanitizedLevelId, nextLevelNumber);
            if (data.highestUnlockedLevelNumber < nextLevelNumber)
            {
                data.highestUnlockedLevelNumber = nextLevelNumber;
                changed = true;
            }

            if (!string.Equals(data.currentLevelId, nextLevelId, StringComparison.OrdinalIgnoreCase))
            {
                data.currentLevelId = nextLevelId;
                changed = true;
            }

            if (changed)
            {
                Save();
            }

            return changed;
        }

        public void Save()
        {
            Data.Sanitize();
            SaveSystem.Save(_saveKey, Data);
            OnDataChanged?.Invoke();
        }

        public void DeleteSave()
        {
            _data = new LevelProgressSaveData();
            SaveSystem.DeleteFile(_saveKey);
        }

        private LevelProgressEntryData GetOrCreateProgress(string sanitizedLevelId)
        {
            LevelProgressEntryData existing = GetProgress(sanitizedLevelId);
            if (existing != null)
            {
                return existing;
            }

            LevelProgressEntryData entry = new LevelProgressEntryData
            {
                levelId = sanitizedLevelId
            };
            Data.levels.Add(entry);
            return entry;
        }

        private LevelProgressSaveData Load()
        {
            string filePath = SaveSystem.GetPath(_saveKey);
            bool fileExists = System.IO.File.Exists(filePath);
            LevelProgressSaveData loaded = SaveSystem.Load<LevelProgressSaveData>(_saveKey) ?? new LevelProgressSaveData();
            loaded.Sanitize();
            if (!fileExists)
            {
                SaveSystem.Save(_saveKey, loaded);
            }
            return loaded;
        }

        private static string BuildLevelIdFromTemplate(string sourceLevelId, int targetLevelNumber)
        {
            if (string.IsNullOrWhiteSpace(sourceLevelId) || targetLevelNumber <= 0)
            {
                return "Level_001";
            }

            int separatorIndex = sourceLevelId.LastIndexOf('_');
            if (separatorIndex >= 0 && separatorIndex < sourceLevelId.Length - 1)
            {
                string prefix = sourceLevelId.Substring(0, separatorIndex + 1);
                int digitCount = sourceLevelId.Length - separatorIndex - 1;
                return $"{prefix}{targetLevelNumber.ToString($"D{Math.Max(1, digitCount)}")}";
            }

            return $"Level_{targetLevelNumber:D3}";
        }
    }
}
