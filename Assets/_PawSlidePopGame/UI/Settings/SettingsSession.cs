using System.IO;
using _PawSlidePopGame._Scripts.Core.Audio;
using _PawSlidePopGame._Scripts.Core.Boostrap;
using _PawSlidePopGame._Scripts.Core.System.DesignPattern.Singleton;
using _PawSlidePopGame._Scripts.Core.Vibration;
using _PawSlidePopGame._Scripts.Data.Events;
using _PawSlidePopGame._Scripts.Data.SaveSystem;
using _PawSlidePopGame.Scripts.DesignPattern.ObserverPattern;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Core.System
{
    public sealed class SettingsManager : Singleton<SettingsManager>, IAppService
    {
        private const string SettingsFileName = "settings.json";

        [Header("Defaults")]
        [SerializeField] private float defaultMusicVolume = 1f;
        [SerializeField] private float defaultSfxVolume = 1f;
        [SerializeField] private bool defaultMusicEnabled = true;
        [SerializeField] private bool defaultSfxEnabled = true;
        [SerializeField] private bool defaultVibrationEnabled = true;

        [Header("Runtime Debug")]
        [SerializeField] private float musicVolume = 1f;
        [SerializeField] private float sfxVolume = 1f;
        [SerializeField] private bool musicEnabled = true;
        [SerializeField] private bool sfxEnabled = true;
        [SerializeField] private bool vibrationEnabled = true;
        [SerializeField] private float previewMusicVolume = 1f;
        [SerializeField] private float previewSfxVolume = 1f;
        [SerializeField] private bool previewMusicEnabled = true;
        [SerializeField] private bool previewSfxEnabled = true;
        [SerializeField] private bool previewVibrationEnabled = true;

        private bool _initialized;
        private string _settingsFilePath;

        public float MusicVolume
        {
            get
            {
                EnsureInitialized();
                return musicVolume;
            }
        }

        public float SfxVolume
        {
            get
            {
                EnsureInitialized();
                return sfxVolume;
            }
        }

        public bool IsMusicEnabled
        {
            get
            {
                EnsureInitialized();
                return musicEnabled;
            }
        }

        public bool IsSfxEnabled
        {
            get
            {
                EnsureInitialized();
                return sfxEnabled;
            }
        }

        public bool IsVibrationEnabled
        {
            get
            {
                EnsureInitialized();
                return vibrationEnabled;
            }
        }

        public void Init()
        {
            EnsureInitialized();
        }

        public void PreviewAudio(float musicValue, float sfxValue)
        {
            EnsureInitialized();

            previewMusicVolume = Mathf.Clamp01(musicValue);
            previewSfxVolume = Mathf.Clamp01(sfxValue);
            previewMusicEnabled = previewMusicVolume > 0.001f;
            previewSfxEnabled = previewSfxVolume > 0.001f;
            ApplyAudioSettings(previewMusicVolume, previewSfxVolume, previewMusicEnabled, previewSfxEnabled);
        }

        public void PreviewVibration(bool enabled, bool playFeedback = false)
        {
            EnsureInitialized();

            previewVibrationEnabled = enabled;
            ApplyVibrationSettings(previewVibrationEnabled);

            if (playFeedback && previewVibrationEnabled)
            {
                EventManager<FeedbackEvent>.Post(FeedbackEvent.VibrationPreviewRequested);
            }
        }

        public void RestoreSavedSettings()
        {
            EnsureInitialized();
            SyncPreviewWithSaved();
            ApplySavedSettings();
        }

        public void Save(float musicValue, float sfxValue, bool vibrationState)
        {
            EnsureInitialized();

            musicVolume = Mathf.Clamp01(musicValue);
            sfxVolume = Mathf.Clamp01(sfxValue);
            musicEnabled = musicVolume > 0.001f;
            sfxEnabled = sfxVolume > 0.001f;
            vibrationEnabled = vibrationState;

            SyncPreviewWithSaved();
            ApplySavedSettings();
            SaveSettingsFile();
        }

        private void LoadSettings()
        {
            SettingsSaveData loaded = LoadSettingsFile();
            loaded.Sanitize(
                defaultMusicVolume,
                defaultSfxVolume,
                defaultMusicEnabled,
                defaultSfxEnabled,
                defaultVibrationEnabled);

            musicVolume = loaded.musicVolume;
            sfxVolume = loaded.sfxVolume;
            musicEnabled = loaded.musicEnabled;
            sfxEnabled = loaded.sfxEnabled;
            vibrationEnabled = loaded.vibrationEnabled;

            SyncPreviewWithSaved();
        }

        private void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            LoadSettings();
            ApplySavedSettings();
            _initialized = true;
        }

        private void SyncPreviewWithSaved()
        {
            previewMusicVolume = musicVolume;
            previewSfxVolume = sfxVolume;
            previewMusicEnabled = musicEnabled;
            previewSfxEnabled = sfxEnabled;
            previewVibrationEnabled = vibrationEnabled;
        }

        private void ApplySavedSettings()
        {
            ApplyAudioSettings(musicVolume, sfxVolume, musicEnabled, sfxEnabled);
            ApplyVibrationSettings(vibrationEnabled);
        }

        private void ApplyAudioSettings(float targetMusicVolume, float targetSfxVolume, bool targetMusicEnabled, bool targetSfxEnabled)
        {
            AudioManager audioManager = AudioManager.Instance;
            if (audioManager == null)
            {
                return;
            }

            audioManager.SetMusicVolume(targetMusicVolume);
            audioManager.SetMusicState(targetMusicEnabled);
            audioManager.SetSfxVolume(targetSfxVolume);
            audioManager.SetSfxState(targetSfxEnabled);
        }

        private void ApplyVibrationSettings(bool targetVibrationEnabled)
        {
            VibrationManager vibrationManager = VibrationManager.Instance;
            if (vibrationManager == null)
            {
                return;
            }

            vibrationManager.SetVibrationEnabled(targetVibrationEnabled);
        }

        private SettingsSaveData LoadSettingsFile()
        {
            string filePath = GetSettingsFilePath();
            return SaveSystem.LoadFromPath<SettingsSaveData>(filePath, useEncryption: false) ?? CreateDefaultSaveData();
        }

        private void SaveSettingsFile()
        {
            SettingsSaveData data = new SettingsSaveData
            {
                musicVolume = musicVolume,
                sfxVolume = sfxVolume,
                musicEnabled = musicEnabled,
                sfxEnabled = sfxEnabled,
                vibrationEnabled = vibrationEnabled
            };

            data.Sanitize(
                defaultMusicVolume,
                defaultSfxVolume,
                defaultMusicEnabled,
                defaultSfxEnabled,
                defaultVibrationEnabled);

            SaveSystem.SaveToPath(GetSettingsFilePath(), data, useEncryption: false);
        }

        private SettingsSaveData CreateDefaultSaveData()
        {
            SettingsSaveData data = new SettingsSaveData
            {
                musicVolume = defaultMusicEnabled ? Mathf.Clamp01(defaultMusicVolume) : 0f,
                sfxVolume = defaultSfxEnabled ? Mathf.Clamp01(defaultSfxVolume) : 0f,
                musicEnabled = defaultMusicEnabled && defaultMusicVolume > 0.001f,
                sfxEnabled = defaultSfxEnabled && defaultSfxVolume > 0.001f,
                vibrationEnabled = defaultVibrationEnabled
            };

            data.Sanitize(
                defaultMusicVolume,
                defaultSfxVolume,
                defaultMusicEnabled,
                defaultSfxEnabled,
                defaultVibrationEnabled);

            return data;
        }

        private string GetSettingsFilePath()
        {
            if (!string.IsNullOrEmpty(_settingsFilePath))
            {
                return _settingsFilePath;
            }

            _settingsFilePath = Path.Combine(Application.persistentDataPath, SettingsFileName);
            return _settingsFilePath;
        }
    }

    [global::System.Serializable]
    public sealed class SettingsSaveData
    {
        public int schemaVersion = 1;
        public float musicVolume = 1f;
        public float sfxVolume = 1f;
        public bool musicEnabled = true;
        public bool sfxEnabled = true;
        public bool vibrationEnabled = true;

        public void Sanitize(
            float defaultMusicVolume,
            float defaultSfxVolume,
            bool defaultMusicEnabled,
            bool defaultSfxEnabled,
            bool defaultVibrationEnabled)
        {
            schemaVersion = global::System.Math.Max(1, schemaVersion);

            if (!defaultMusicEnabled)
            {
                musicEnabled = false;
                musicVolume = 0f;
            }
            else
            {
                musicVolume = Mathf.Clamp01(musicVolume);
                musicEnabled = musicEnabled && musicVolume > 0.001f;
            }

            if (!defaultSfxEnabled)
            {
                sfxEnabled = false;
                sfxVolume = 0f;
            }
            else
            {
                sfxVolume = Mathf.Clamp01(sfxVolume);
                sfxEnabled = sfxEnabled && sfxVolume > 0.001f;
            }

            if (!defaultVibrationEnabled)
            {
                vibrationEnabled = false;
            }

            if (!musicEnabled)
            {
                musicVolume = 0f;
            }

            if (!sfxEnabled)
            {
                sfxVolume = 0f;
            }
        }
    }
}
