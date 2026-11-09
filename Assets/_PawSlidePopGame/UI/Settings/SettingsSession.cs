using System;
using MazFramework.Audio;
using MazFramework.Haptics;
using UnityEngine;
using BaseSettings = MazFramework.Settings.SettingsManager;

namespace _PawSlidePopGame._Scripts.Core.System
{
    /// <summary>Preview/save/cancel for one popup opening, using the existing Base services directly.</summary>
    public sealed class SettingsSession
    {
        public bool IsAvailable => BaseSettings.Instance != null && BaseSettings.Instance.IsInitialized &&
            AudioManager.ExistingInstance != null && AudioManager.ExistingInstance.IsInitialized &&
            HapticManager.ExistingInstance != null && HapticManager.ExistingInstance.IsInitialized;
        public float MusicVolume => BaseSettings.Instance == null ? 1f :
            (BaseSettings.Instance.IsMusicEnabled ? BaseSettings.Instance.MusicVolume : 0f);
        public float SfxVolume => BaseSettings.Instance == null ? 1f :
            (BaseSettings.Instance.IsSfxEnabled ? BaseSettings.Instance.SfxVolume : 0f);
        public bool IsVibrationEnabled => BaseSettings.Instance == null || BaseSettings.Instance.IsHapticsEnabled;
        public void PreviewAudio(float music, float sfx)
        {
            var audio = AudioManager.ExistingInstance;
            if (audio == null) return;
            music = Clamp(music);
            sfx = Clamp(sfx);
            audio.SetMusicVolume(music);
            audio.SetSfxVolume(sfx);
            audio.SetMusicEnabled(music > .001f);
            audio.SetSfxEnabled(sfx > .001f);
        }
        public void PreviewVibration(bool enabled, bool playFeedback = false)
        {
            var haptics = HapticManager.ExistingInstance;
            if (haptics == null) return;
            haptics.SetEnabled(enabled);
            if (enabled && playFeedback) haptics.PlaySelection();
        }
        public void RestoreSavedSettings()
        {
            BaseSettings.Instance?.ApplySettings();
        }
        public bool Save(float music, float sfx, bool vibration)
        {
            if (float.IsNaN(music) || float.IsInfinity(music) || float.IsNaN(sfx) || float.IsInfinity(sfx)) return false;
            if (!IsAvailable) return false;
            music = Clamp(music);
            sfx = Clamp(sfx);
            var settings = BaseSettings.Instance;
            float savedMusic = settings.MusicVolume;
            float savedSfx = settings.SfxVolume;
            bool savedMusicEnabled = settings.IsMusicEnabled;
            bool savedSfxEnabled = settings.IsSfxEnabled;
            bool savedHaptics = settings.IsHapticsEnabled;
            try
            {
                settings.SetMusicVolume(music);
                settings.SetSfxVolume(sfx);
                settings.SetMusicEnabled(music > .001f);
                settings.SetSfxEnabled(sfx > .001f);
                settings.SetHapticsEnabled(vibration);
                settings.Save();
                return true;
            }
            catch (Exception exception)
            {
                settings.SetMusicVolume(savedMusic);
                settings.SetSfxVolume(savedSfx);
                settings.SetMusicEnabled(savedMusicEnabled);
                settings.SetSfxEnabled(savedSfxEnabled);
                settings.SetHapticsEnabled(savedHaptics);
                settings.ApplySettings();
                Debug.LogException(exception);
                return false;
            }
        }
        private static float Clamp(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 0f : Math.Max(0f, Math.Min(1f, value));
    }
}
