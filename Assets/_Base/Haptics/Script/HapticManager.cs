using System;
using Lofelt.NiceVibrations;
using MazFramework.Bootstrap;
using MazFramework.Foundation.Singleton;
using UnityEngine;

namespace MazFramework.Haptics
{
    /// <summary>
    /// Mobile vibration only; audio is handled separately by the game.
    /// Place on a root object in the startup scene, then call Instance.PlaySelection(), etc.
    /// </summary>
    [DisallowMultipleComponent, SingletonPrefab]
    public sealed class HapticManager : Singleton<HapticManager>, IAppSystem
    {
        [SerializeField] private bool hapticsEnabled = true;

        public bool IsInitialized { get; private set; }
        public bool IsEnabled => hapticsEnabled;

        private bool _initialized = false;
        private bool _hasFocus;
        private bool _paused;


        public bool Initialize()
        {
            if (IsInitialized) return true;
            if (ExistingInstance != this || !isActiveAndEnabled) return false;
            _hasFocus = Application.isFocused;

#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            // A false result means advanced playback is unavailable; presets can still work.
            HapticController.Init();
            HapticController.Reset();
            HapticController.outputLevel = 1f;
            _initialized = true;
#endif
            ApplyEnabledState();
            // Service readiness is independent of native vibration support (e.g. Editor).
            IsInitialized = true;
            return true;
        }

        public void PlaySelection() => Play(HapticPatterns.PresetType.Selection);
        public void PlaySuccess() => Play(HapticPatterns.PresetType.Success);
        public void PlayWarning() => Play(HapticPatterns.PresetType.Warning);
        public void PlayFailure() => Play(HapticPatterns.PresetType.Failure);
        public void PlayLightImpact() => Play(HapticPatterns.PresetType.LightImpact);
        public void PlayMediumImpact() => Play(HapticPatterns.PresetType.MediumImpact);
        public void PlayHeavyImpact() => Play(HapticPatterns.PresetType.HeavyImpact);
        public void PlayRigidImpact() => Play(HapticPatterns.PresetType.RigidImpact);
        public void PlaySoftImpact() => Play(HapticPatterns.PresetType.SoftImpact);

        public void Play(HapticPatterns.PresetType preset)
        {
            if (ExistingInstance != this || !_initialized || !isActiveAndEnabled ||
                !hapticsEnabled || !_hasFocus || _paused ||
                !DeviceCapabilities.isVersionSupported ||
                preset == HapticPatterns.PresetType.None ||
                !Enum.IsDefined(typeof(HapticPatterns.PresetType), preset)) return;

            ApplyEnabledState();
            HapticController.Stop();
            HapticPatterns.PlayPreset(preset);
        }

        /// <summary>Changes runtime state. The game's settings system owns persistence.</summary>
        public void SetEnabled(bool value)
        {
            hapticsEnabled = value;
            ApplyEnabledState();
        }

        public void Stop()
        {
            if (ExistingInstance == this && _initialized) HapticController.Stop();
        }

        private void ApplyEnabledState()
        {
            if (ExistingInstance != this || !_initialized) return;

            bool canPlay = hapticsEnabled && isActiveAndEnabled && _hasFocus && !_paused;
            // Assigning true again also stops playback in Nice Vibrations.
            if (HapticController.hapticsEnabled != canPlay)
                HapticController.hapticsEnabled = canPlay;
            if (!canPlay) Stop();
        }

        private void OnEnable() => ApplyEnabledState();
        private void OnDisable() => ApplyEnabledState();

        private void OnApplicationFocus(bool hasFocus)
        {
            _hasFocus = hasFocus;
            ApplyEnabledState();
        }

        private void OnApplicationPause(bool paused)
        {
            _paused = paused;
            ApplyEnabledState();
        }

        protected override void OnDestroy()
        {
            if (ExistingInstance == this) Stop();
            base.OnDestroy();
        }
    }
}

