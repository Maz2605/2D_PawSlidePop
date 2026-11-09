using System;
using _PawSlidePopGame._Scripts.Data.SaveSystem;

namespace _PawSlidePopGame._Scripts.Feature.Meta.Wheel
{
    public sealed class WheelStateRepository
    {
        public const string DefaultSaveKey = "wheel_state";

        private readonly string _saveKey;
        private WheelStateSaveData _data;

        public static event Action OnDataChanged;

        public WheelStateRepository(string saveKey = DefaultSaveKey)
        {
            _saveKey = string.IsNullOrWhiteSpace(saveKey) ? DefaultSaveKey : saveKey;
            SaveSystem.OnSaveSynced += HandleSaveSynced;
        }

        ~WheelStateRepository()
        {
            SaveSystem.OnSaveSynced -= HandleSaveSynced;
        }

        private void HandleSaveSynced(string key)
        {
            if (key == _saveKey)
            {
                Reload();
            }
        }

        public DateTime? UtcNowOverride { get; set; }
        public WheelStateSaveData Data => _data ??= Load();

        public bool CanFreeSpin(int cooldownSeconds)
        {
            return GetRemainingCooldownSeconds(cooldownSeconds) <= 0;
        }

        public double GetRemainingCooldownSeconds(int cooldownSeconds)
        {
            cooldownSeconds = Math.Max(0, cooldownSeconds);
            if (cooldownSeconds == 0)
            {
                return 0;
            }

            DateTime lastSpin = GetLastSpinTime();
            if (lastSpin == DateTime.MinValue)
            {
                return 0;
            }

            double elapsed = (CurrentUtcNow - lastSpin).TotalSeconds;
            return Math.Max(0, cooldownSeconds - elapsed);
        }

        public void MarkFreeSpinUsed()
        {
            Data.lastFreeSpinUtc = CurrentUtcNow.ToString("o");
            Save();
        }

        public void ResetFreeSpinCooldown()
        {
            Data.lastFreeSpinUtc = string.Empty;
            Save();
        }

        public void Reload()
        {
            _data = Load();
            OnDataChanged?.Invoke();
        }

        public void DeleteSave()
        {
            _data = new WheelStateSaveData();
            SaveSystem.DeleteFile(_saveKey);
        }

        private DateTime GetLastSpinTime()
        {
            string raw = Data.lastFreeSpinUtc;
            if (string.IsNullOrWhiteSpace(raw) || !DateTime.TryParse(raw, out DateTime parsed))
            {
                return DateTime.MinValue;
            }

            return parsed.ToUniversalTime();
        }

        private DateTime CurrentUtcNow => UtcNowOverride ?? DateTime.UtcNow;

        private WheelStateSaveData Load()
        {
            string filePath = SaveSystem.GetPath(_saveKey);
            bool fileExists = System.IO.File.Exists(filePath);
            WheelStateSaveData loaded = SaveSystem.Load<WheelStateSaveData>(_saveKey) ?? new WheelStateSaveData();
            loaded.Sanitize();
            if (!fileExists)
            {
                SaveSystem.Save(_saveKey, loaded);
            }
            return loaded;
        }

        private void Save()
        {
            Data.Sanitize();
            SaveSystem.Save(_saveKey, Data);
            OnDataChanged?.Invoke();
        }
    }

    [Serializable]
    public sealed class WheelStateSaveData
    {
        public int schemaVersion = 1;
        public string lastFreeSpinUtc = string.Empty;

        public void Sanitize()
        {
            schemaVersion = Math.Max(1, schemaVersion);
            lastFreeSpinUtc ??= string.Empty;
        }
    }
}
