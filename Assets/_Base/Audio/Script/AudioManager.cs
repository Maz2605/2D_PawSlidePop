using System.Collections;
using System.Collections.Generic;
using MazFramework.Bootstrap;
using MazFramework.Foundation.Singleton;
using UnityEngine;
using UnityEngine.Audio;

namespace MazFramework.Audio
{
    [DisallowMultipleComponent, SingletonPrefab]
    public sealed class AudioManager : Singleton<AudioManager>, IAppSystem
    {
        private const string MasterVolumeParameter = "MasterVolume";
        private const string MusicVolumeParameter = "MusicVolume";
        private const string SfxVolumeParameter = "SFXVolume";

        [Header("Config")]
        [SerializeField] private AudioClipConfigSO audioClipConfig;
        [SerializeField] private AudioMixer audioMixer;

        [Header("Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private Transform sfxSourceRoot;
        [SerializeField] private AudioSource sfxSourceTemplate;

        [Header("SFX Pool")]
        [SerializeField, Min(0)] private int initialSfxPoolSize = 10;
        [SerializeField, Min(1)] private int maxSfxPoolSize = 20;

        [Header("Volume")]
        [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

        public bool IsInitialized => _initialized && _mixerReady;

        public AudioClipConfigSO Config => audioClipConfig;
        public bool IsMusicEnabled { get; private set; } = true;
        public bool IsSfxEnabled { get; private set; } = true;

        public float MasterVolume => masterVolume;
        public float MusicVolume => musicVolume;
        public float SfxVolume => sfxVolume;

        private readonly List<AudioSource> _sfxSources = new List<AudioSource>();
        private readonly Dictionary<AudioSource, Coroutine> _releaseRoutines =
            new Dictionary<AudioSource, Coroutine>();

        private readonly Dictionary<string, float> _lastSfxByKey = new Dictionary<string, float>();
        private readonly Dictionary<AudioClip, float> _lastSfxByClip = new Dictionary<AudioClip, float>();

        private Coroutine _musicRoutine;
        private bool _initialized;
        private bool _mixerReady;
        private bool _hasFocus;
        private bool _applicationPaused;
        private bool _musicPaused;
        private bool _sourcePaused;

        private bool MusicSuspended => _musicPaused || !_hasFocus || _applicationPaused;
        private bool CanPlay => ExistingInstance == this && _initialized && isActiveAndEnabled;


        public bool Initialize()
        {
            if (IsInitialized) return true;
            if (ExistingInstance != this || !isActiveAndEnabled) return false;
            _hasFocus = Application.isFocused;

            if (!HasRequiredReferences())
            {
                return false;
            }

            musicSource.playOnAwake = false;
            // AppBootstrap calls Initialize in Start, when mixer parameters can be set.
            musicSource.mute = true;
            sfxSourceTemplate.gameObject.SetActive(false);

            int poolSize = Mathf.Min(initialSfxPoolSize, maxSfxPoolSize);
            for (int i = _sfxSources.Count; i < poolSize; i++)
            {
                CreateSfxSource();
            }

            _mixerReady = true;
            ApplyVolumes();
            musicSource.mute = false;
            foreach (AudioSource source in _sfxSources) source.mute = false;
            _lastSfxByKey.Clear();
            _lastSfxByClip.Clear();
            _initialized = true;
            return true;
        }

        public void PlayMusic(
            AudioClip clip,
            bool loop = true,
            float fadeDuration = 0.5f,
            float volumeScale = 1f,
            float pitch = 1f,
            float delay = 0f)
        {
            if (!CanPlay || clip == null)
            {
                return;
            }

            StopMusicRoutine();

            float volume = Mathf.Clamp01(volumeScale);
            float safePitch = Mathf.Clamp(Mathf.Abs(pitch), 0.01f, 3f);

            if (musicSource.clip == clip && musicSource.isPlaying)
            {
                musicSource.loop = loop;
                musicSource.pitch = safePitch;
                musicSource.volume = volume;
                return;
            }

            _musicRoutine = StartCoroutine(ChangeMusic(
                clip,
                loop,
                Mathf.Max(0f, fadeDuration),
                volume,
                safePitch,
                Mathf.Max(0f, delay)));
        }

        public void PlayRandomMusic(
            AudioClip[] clips,
            bool loop = true,
            float fadeDuration = 0.5f,
            float volumeScale = 1f,
            float pitch = 1f,
            float delay = 0f)
        {
            PlayMusic(GetRandomClip(clips), loop, fadeDuration, volumeScale, pitch, delay);
        }

        public void StopMusic(float fadeDuration = 0.25f)
        {
            if (!CanPlay)
            {
                return;
            }

            StopMusicRoutine();
            _musicRoutine = StartCoroutine(StopMusicAfterFade(Mathf.Max(0f, fadeDuration)));
        }

        public void PauseMusic()
        {
            if (!CanPlay) return;
            _musicPaused = true;
            ApplyMusicPause();
        }

        public void ResumeMusic()
        {
            if (!CanPlay) return;
            _musicPaused = false;
            ApplyMusicPause();
        }

        public void PlaySfx(
            AudioClip clip,
            float volumeScale = 1f,
            float pitch = 1f,
            float delay = 0f,
            float cooldown = 0f,
            string cooldownKey = null)
        {
            PlaySfx(clip, null, volumeScale, pitch, delay, cooldown, cooldownKey);
        }

        public void PlayRandomSfx(
            IReadOnlyList<AudioClip> clips,
            float volumeScale = 1f,
            float pitch = 1f,
            float delay = 0f,
            float cooldown = 0f,
            string cooldownKey = null)
        {
            PlaySfx(GetRandomClip(clips), null, volumeScale, pitch, delay, cooldown, cooldownKey);
        }

        public void PlaySfx(
            AudioClip clip,
            string sourceName,
            float volumeScale = 1f,
            float pitch = 1f,
            float delay = 0f,
            float cooldown = 0f,
            string cooldownKey = null)
        {
            if (!CanPlay || !IsSfxEnabled || !_hasFocus || _applicationPaused || clip == null)
            {
                return;
            }

            bool limitPlayback = cooldown > 0f && !float.IsInfinity(cooldown);
            bool namedCooldown = !string.IsNullOrEmpty(cooldownKey);
            float now = Time.unscaledTime;
            if (limitPlayback)
            {
                float lastPlayed;
                bool hasPrevious = namedCooldown
                    ? _lastSfxByKey.TryGetValue(cooldownKey, out lastPlayed)
                    : _lastSfxByClip.TryGetValue(clip, out lastPlayed);
                if (hasPrevious && now - lastPlayed < cooldown) return;
            }

            AudioSource source = GetAvailableSfxSource();
            if (source == null)
            {
                Debug.LogWarning($"[AudioManager] SFX pool is full. Skipped '{clip.name}'.", this);
                return;
            }

            // Rejected requests do not consume the cooldown or queue a later sound.
            if (limitPlayback)
            {
                if (namedCooldown) _lastSfxByKey[cooldownKey] = now;
                else _lastSfxByClip[clip] = now;
            }

            float safePitch = Mathf.Clamp(Mathf.Abs(pitch), 0.01f, 3f);
            float safeDelay = Mathf.Max(0f, delay);

            source.name = string.IsNullOrWhiteSpace(sourceName) ? clip.name : sourceName;
            source.clip = clip;
            source.volume = Mathf.Clamp01(volumeScale);
            source.pitch = safePitch;
            source.loop = false;
            source.gameObject.SetActive(true);
            source.PlayDelayed(safeDelay);

            float playTime = safeDelay + clip.length / safePitch + 0.05f;
            _releaseRoutines[source] = StartCoroutine(ReleaseSfxAfter(source, playTime));
        }

        public void StopAllSfx()
        {
            foreach (AudioSource source in _sfxSources)
            {
                if (_releaseRoutines.TryGetValue(source, out Coroutine routine))
                {
                    StopCoroutine(routine);
                }

                ResetSfxSource(source);
            }

            _releaseRoutines.Clear();
        }

        public void SetMasterVolume(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return;
            masterVolume = Mathf.Clamp01(value);
            ApplyMixerVolume(MasterVolumeParameter, masterVolume);
        }

        public void SetMusicVolume(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return;
            musicVolume = Mathf.Clamp01(value);
            ApplyMixerVolume(MusicVolumeParameter, IsMusicEnabled ? musicVolume : 0f);
        }

        public void SetSfxVolume(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return;
            sfxVolume = Mathf.Clamp01(value);
            ApplyMixerVolume(SfxVolumeParameter, IsSfxEnabled ? sfxVolume : 0f);
        }

        public void SetMusicEnabled(bool enabled)
        {
            IsMusicEnabled = enabled;
            ApplyMixerVolume(MusicVolumeParameter, enabled ? musicVolume : 0f);
        }

        public void SetSfxEnabled(bool enabled)
        {
            IsSfxEnabled = enabled;
            ApplyMixerVolume(SfxVolumeParameter, enabled ? sfxVolume : 0f);

            if (!enabled)
            {
                StopAllSfx();
            }
        }

        private bool HasRequiredReferences()
        {
            bool isValid = audioClipConfig != null &&
                           audioMixer != null &&
                           musicSource != null &&
                           sfxSourceRoot != null &&
                           sfxSourceTemplate != null;

            if (!isValid)
            {
                Debug.LogError("[AudioManager] Prefab references are missing.", this);
            }

            return isValid;
        }

        private AudioSource CreateSfxSource()
        {
            if (_sfxSources.Count >= maxSfxPoolSize)
            {
                return null;
            }

            AudioSource source = Instantiate(sfxSourceTemplate, sfxSourceRoot);
            source.name = "SFXSource";
            source.playOnAwake = false;
            source.mute = !_mixerReady;
            source.loop = false;
            source.gameObject.SetActive(false);
            _sfxSources.Add(source);
            return source;
        }

        private AudioSource GetAvailableSfxSource()
        {
            foreach (AudioSource source in _sfxSources)
            {
                if (!source.gameObject.activeSelf)
                {
                    return source;
                }
            }

            return CreateSfxSource();
        }

        private static AudioClip GetRandomClip(IReadOnlyList<AudioClip> clips)
        {
            if (clips == null || clips.Count == 0)
            {
                return null;
            }

            int startIndex = Random.Range(0, clips.Count);
            for (int i = 0; i < clips.Count; i++)
            {
                AudioClip clip = clips[(startIndex + i) % clips.Count];
                if (clip != null)
                {
                    return clip;
                }
            }

            return null;
        }

        private IEnumerator ReleaseSfxAfter(AudioSource source, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            _releaseRoutines.Remove(source);
            ResetSfxSource(source);
        }

        private static void ResetSfxSource(AudioSource source)
        {
            if (source == null)
            {
                return;
            }

            source.Stop();
            source.clip = null;
            source.pitch = 1f;
            source.volume = 1f;
            source.name = "SFXSource";
            source.gameObject.SetActive(false);
        }

        private IEnumerator ChangeMusic(
            AudioClip clip,
            bool loop,
            float fadeDuration,
            float volume,
            float pitch,
            float delay)
        {
            if (delay > 0f)
            {
                yield return WaitForMusicSeconds(delay);
            }
            while (MusicSuspended) yield return null;

            bool wasPlaying = musicSource.isPlaying;
            float fadeStep = wasPlaying ? fadeDuration * 0.5f : fadeDuration;

            if (wasPlaying)
            {
                yield return FadeMusic(musicSource.volume, 0f, fadeStep);
            }

            musicSource.Stop();
            musicSource.clip = clip;
            musicSource.loop = loop;
            musicSource.pitch = pitch;
            musicSource.volume = fadeDuration > 0f ? 0f : volume;
            musicSource.Play();
            _sourcePaused = false;

            if (fadeDuration > 0f)
            {
                yield return FadeMusic(0f, volume, fadeStep);
            }

            musicSource.volume = volume;
            _musicRoutine = null;
        }

        private IEnumerator StopMusicAfterFade(float fadeDuration)
        {
            if (musicSource.isPlaying && fadeDuration > 0f)
            {
                yield return FadeMusic(musicSource.volume, 0f, fadeDuration);
            }

            musicSource.Stop();
            _sourcePaused = false;
            musicSource.clip = null;
            musicSource.pitch = 1f;
            musicSource.volume = 1f;
            _musicRoutine = null;
        }

        private IEnumerator FadeMusic(float from, float to, float duration)
        {
            if (duration <= 0f)
            {
                musicSource.volume = to;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (!MusicSuspended)
                {
                    elapsed += Time.unscaledDeltaTime;
                    musicSource.volume = Mathf.Lerp(from, to, elapsed / duration);
                }
                yield return null;
            }

            musicSource.volume = to;
        }

        private void StopMusicRoutine()
        {
            if (_musicRoutine == null)
            {
                return;
            }

            StopCoroutine(_musicRoutine);
            _musicRoutine = null;
        }

        private void ApplyMixerVolume(string parameterName, float value)
        {
            if (ExistingInstance != this || !_mixerReady || audioMixer == null)
            {
                return;
            }

            float decibels = value <= 0f ? -80f : Mathf.Log10(value) * 20f;
            if (!audioMixer.SetFloat(parameterName, decibels))
                Debug.LogWarning($"[AudioManager] Mixer parameter '{parameterName}' is missing.", this);
        }

        private void ApplyVolumes()
        {
            SetMasterVolume(masterVolume);
            SetMusicVolume(musicVolume);
            SetSfxVolume(sfxVolume);
        }

        private IEnumerator WaitForMusicSeconds(float duration)
        {
            float remaining = duration;
            while (remaining > 0f)
            {
                if (!MusicSuspended) remaining -= Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private void ApplyMusicPause()
        {
            if (!CanPlay) return;
            if (MusicSuspended)
            {
                if (musicSource.isPlaying)
                {
                    musicSource.Pause();
                    _sourcePaused = true;
                }
            }
            else if (_sourcePaused)
            {
                musicSource.UnPause();
                _sourcePaused = false;
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            _hasFocus = hasFocus;
            ApplyMusicPause();
            if (ExistingInstance == this && !hasFocus) StopAllSfx();
        }

        private void OnApplicationPause(bool paused)
        {
            _applicationPaused = paused;
            ApplyMusicPause();
            if (ExistingInstance == this && paused) StopAllSfx();
        }

        private void OnDisable()
        {
            if (ExistingInstance != this || !_initialized) return;
            StopMusicRoutine();
            musicSource.Stop();
            musicSource.clip = null;
            _musicPaused = false;
            _sourcePaused = false;
            StopAllSfx();
        }

        private void OnValidate()
        {
            maxSfxPoolSize = Mathf.Max(1, maxSfxPoolSize);
            initialSfxPoolSize = Mathf.Clamp(initialSfxPoolSize, 0, maxSfxPoolSize);
        }

        protected override void OnDestroy()
        {
            if (ExistingInstance == this)
            {
                StopMusicRoutine();
                StopAllSfx();
                _lastSfxByKey.Clear();
                _lastSfxByClip.Clear();
            }
            base.OnDestroy();
        }
    }
}

