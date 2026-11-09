using System;
using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Data.SaveSystem;
using _PawSlidePopGame._Scripts.Feature.Match3.Boosters;

namespace _PawSlidePopGame._Scripts.Gameplay.Meta.EconomyManager
{
    public sealed class PlayerEconomyRepository
    {
        public const string DefaultSaveKey = "player_economy";

        private static readonly Lazy<PlayerEconomyRepository> LazyInstance =
            new Lazy<PlayerEconomyRepository>(() => new PlayerEconomyRepository(DefaultSaveKey));

        private readonly string _saveKey;
        private PlayerEconomySaveData _data;

        public static event Action OnDataChanged;

        public static PlayerEconomyRepository Instance => LazyInstance.Value;
        public PlayerEconomySaveData Data => _data ??= Load();

        public PlayerEconomyRepository(string saveKey)
        {
            _saveKey = saveKey;
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

        public void Save()
        {
            Data.Sanitize();
            SaveSystem.Save(_saveKey, Data);
            OnDataChanged?.Invoke();
        }
        private PlayerEconomySaveData Load()
        {
            string filePath = SaveSystem.GetPath(_saveKey);
            bool fileExists = System.IO.File.Exists(filePath);
            PlayerEconomySaveData loaded = SaveSystem.Load<PlayerEconomySaveData>(_saveKey) ?? new PlayerEconomySaveData();
            loaded.Sanitize();
            if (!fileExists)
            {
                SaveSystem.Save(_saveKey, loaded);
            }
            return loaded;
        }

        public void DeleteSave()
        {
            _data = new PlayerEconomySaveData();
            SaveSystem.DeleteFile(_saveKey);
        }

        public bool RegisterBoosterDefinitions(IEnumerable<BoosterDefinitionSO> definitions)
        {
            bool changed = false;
            PlayerEconomySaveData data = Data;
            if (definitions == null)
            {
                return false;
            }

            foreach (BoosterDefinitionSO definition in definitions)
            {
                if (definition == null || string.IsNullOrWhiteSpace(definition.BoosterId))
                {
                    continue;
                }

                if (data.boosterCounts.ContainsKey(definition.BoosterId))
                {
                    continue;
                }

                data.boosterCounts[definition.BoosterId] = Math.Max(0, definition.InitialCount);
                changed = true;
            }

            if (changed)
            {
                Save();
            }

            return changed;
        }

        
    }
}
