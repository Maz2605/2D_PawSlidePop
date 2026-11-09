using MazFramework.Audio;
using MazFramework.Bootstrap;
using MazFramework.Haptics;
using UnityEngine;

namespace MazFramework.Settings
{
    /// <summary>Loads, applies and persists player preferences. UI changes settings through this manager.</summary>
    [DisallowMultipleComponent]
    public sealed class SettingsManager : MonoBehaviour, IAppSystem
    {
        private const string KeyPrefix = "Settings.";
        private const string MusicEnabledKey = KeyPrefix + "MusicEnabled";
        private const string SfxEnabledKey = KeyPrefix + "SfxEnabled";
        private const string HapticsEnabledKey = KeyPrefix + "HapticsEnabled";
        private const string MasterVolumeKey = KeyPrefix + "MasterVolume";
        private const string MusicVolumeKey = KeyPrefix + "MusicVolume";
        private const string SfxVolumeKey = KeyPrefix + "SfxVolume";
        private bool _dirty;

        public static SettingsManager Instance { get; private set; }
        public bool IsInitialized { get; private set; }
        public bool IsMusicEnabled { get; private set; } = true;
        public bool IsSfxEnabled { get; private set; } = true;
        public bool IsHapticsEnabled { get; private set; } = true;
        public float MasterVolume { get; private set; } = 1f;
        public float MusicVolume { get; private set; } = 1f;
        public float SfxVolume { get; private set; } = 1f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public bool Initialize()
        {
            if (IsInitialized) return true;
            if (Instance != this || !isActiveAndEnabled) return false;
            if (AudioManager.ExistingInstance == null || !AudioManager.ExistingInstance.IsInitialized ||
                HapticManager.ExistingInstance == null || !HapticManager.ExistingInstance.IsInitialized)
            {
                Debug.LogError("[SettingsManager] Initialize Audio and Haptics before Settings in AppBootstrap.", this);
                return false;
            }
            IsMusicEnabled = PlayerPrefs.GetInt(MusicEnabledKey, 1) != 0;
            IsSfxEnabled = PlayerPrefs.GetInt(SfxEnabledKey, 1) != 0;
            IsHapticsEnabled = PlayerPrefs.GetInt(HapticsEnabledKey, 1) != 0;
            MasterVolume = ValidateVolume(PlayerPrefs.GetFloat(MasterVolumeKey, 1f));
            MusicVolume = ValidateVolume(PlayerPrefs.GetFloat(MusicVolumeKey, 1f));
            SfxVolume = ValidateVolume(PlayerPrefs.GetFloat(SfxVolumeKey, 1f));
            ApplySettingsToServices();
            IsInitialized = true;
            return true;
        }

        public void SetMusicEnabled(bool value)
        {
            if (Instance != this || !IsInitialized || IsMusicEnabled == value) return;
            IsMusicEnabled = value;
            _dirty = true;
            if (AudioManager.ExistingInstance != null) AudioManager.ExistingInstance.SetMusicEnabled(value);
        }

        public void SetSfxEnabled(bool value)
        {
            if (Instance != this || !IsInitialized || IsSfxEnabled == value) return;
            IsSfxEnabled = value;
            _dirty = true;
            if (AudioManager.ExistingInstance != null) AudioManager.ExistingInstance.SetSfxEnabled(value);
        }

        public void SetHapticsEnabled(bool value)
        {
            if (Instance != this || !IsInitialized || IsHapticsEnabled == value) return;
            IsHapticsEnabled = value;
            _dirty = true;
            if (HapticManager.ExistingInstance != null) HapticManager.ExistingInstance.SetEnabled(value);
        }

        public void SetMasterVolume(float value)
        {
            if (Instance != this || !IsInitialized || !IsFinite(value)) return;
            value = Mathf.Clamp01(value);
            if (MasterVolume == value) return;
            MasterVolume = value;
            _dirty = true;
            if (AudioManager.ExistingInstance != null) AudioManager.ExistingInstance.SetMasterVolume(value);
        }

        public void SetMusicVolume(float value)
        {
            if (Instance != this || !IsInitialized || !IsFinite(value)) return;
            value = Mathf.Clamp01(value);
            if (MusicVolume == value) return;
            MusicVolume = value;
            _dirty = true;
            if (AudioManager.ExistingInstance != null) AudioManager.ExistingInstance.SetMusicVolume(value);
        }

        public void SetSfxVolume(float value)
        {
            if (Instance != this || !IsInitialized || !IsFinite(value)) return;
            value = Mathf.Clamp01(value);
            if (SfxVolume == value) return;
            SfxVolume = value;
            _dirty = true;
            if (AudioManager.ExistingInstance != null) AudioManager.ExistingInstance.SetSfxVolume(value);
        }

        /// <summary>Reapply preferences after manually creating or replacing services.</summary>
        public void ApplySettings()
        {
            if (Instance != this || !IsInitialized) return;
            ApplySettingsToServices();
        }

        private void ApplySettingsToServices()
        {
            AudioManager audio = AudioManager.ExistingInstance;
            if (audio != null)
            {
                audio.SetMusicEnabled(IsMusicEnabled);
                audio.SetSfxEnabled(IsSfxEnabled);
                audio.SetMasterVolume(MasterVolume);
                audio.SetMusicVolume(MusicVolume);
                audio.SetSfxVolume(SfxVolume);
            }
            if (HapticManager.ExistingInstance != null)
                HapticManager.ExistingInstance.SetEnabled(IsHapticsEnabled);
        }

        public void ResetToDefaults()
        {
            if (Instance != this || !IsInitialized) return;
            SetMusicEnabled(true);
            SetSfxEnabled(true);
            SetHapticsEnabled(true);
            SetMasterVolume(1f);
            SetMusicVolume(1f);
            SetSfxVolume(1f);
            _dirty = true;
            ApplySettings();
            Save();
        }

        /// <summary>Flush to disk when closing the settings popup. Also called on background/quit.</summary>
        public void Save()
        {
            if (Instance != this || !IsInitialized || !_dirty) return;
            PlayerPrefs.SetInt(MusicEnabledKey, IsMusicEnabled ? 1 : 0);
            PlayerPrefs.SetInt(SfxEnabledKey, IsSfxEnabled ? 1 : 0);
            PlayerPrefs.SetInt(HapticsEnabledKey, IsHapticsEnabled ? 1 : 0);
            PlayerPrefs.SetFloat(MasterVolumeKey, MasterVolume);
            PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume);
            PlayerPrefs.SetFloat(SfxVolumeKey, SfxVolume);
            PlayerPrefs.Save();
            _dirty = false;
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private static float ValidateVolume(float value)
        {
            return IsFinite(value) ? Mathf.Clamp01(value) : 1f;
        }

        private void OnApplicationPause(bool paused) { if (paused) Save(); }
        private void OnApplicationFocus(bool hasFocus) { if (!hasFocus) Save(); }
        private void OnApplicationQuit() => Save();
        private void OnDisable() => Save();

        private void OnDestroy()
        {
            if (Instance != this) return;
            Save();
            Instance = null;
        }
    }
}
