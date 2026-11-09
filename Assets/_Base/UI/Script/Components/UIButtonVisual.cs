using DG.Tweening;
using MazFramework.Audio;
using MazFramework.Haptics;
using UnityEngine;
using UnityEngine.UI;

namespace MazFramework.UI
{
    /// <summary>Button presentation only; the owning view registers business listeners separately.</summary>
    [DefaultExecutionOrder(-1000), RequireComponent(typeof(Button)), DisallowMultipleComponent]
    public sealed class UIButtonVisual : MonoBehaviour
    {
        [SerializeField] private Transform effectTarget;
        [SerializeField] private bool animatePress = true;
        [SerializeField] private float punchScale = -0.1f;
        [SerializeField, Min(0f)] private float punchDuration = 0.15f;
        [SerializeField, Min(1)] private int punchVibrato = 5;
        [SerializeField] private bool playAudio = true;
        [SerializeField] private AudioClip clickClip;
        [SerializeField, Range(0f, 1f)] private float volumeScale = 1f;
        [SerializeField] private bool playHaptics = true;

        private Button _button;
        private Tween _pressTween;
        private Vector3 _originalScale;
        private Transform _capturedTarget;

        private void Awake()
        {
            _button = GetComponent<Button>();
            CaptureScale();
        }

        private void OnEnable()
        {
            if (_button == null) _button = GetComponent<Button>();
            CaptureScale();
            _button.onClick.RemoveListener(PlayFeedback);
            _button.onClick.AddListener(PlayFeedback);
        }

        /// <summary>Configure runtime-created buttons. No game callback is stored here.</summary>
        public void Configure(Transform target, AudioClip clip, bool haptics, bool animate = true,
            float strength = -0.1f, float duration = 0.15f, int vibrato = 5, float volume = 1f)
        {
            StopPress();
            effectTarget = target;
            clickClip = clip;
            playAudio = clip != null;
            playHaptics = haptics;
            animatePress = animate;
            punchScale = strength;
            punchDuration = Mathf.Max(0f, duration);
            punchVibrato = Mathf.Max(1, vibrato);
            volumeScale = Mathf.Clamp01(volume);
            CaptureScale();
        }

        public void PlayFeedback()
        {
            if (!isActiveAndEnabled || _button == null || !_button.IsActive() || !_button.IsInteractable()) return;

            var audio = AudioManager.ExistingInstance;
            if (playAudio && audio != null)
                audio.PlaySfx(clickClip != null ? clickClip : audio.Config?.buttonClick, volumeScale);
            if (playHaptics) HapticManager.ExistingInstance?.PlaySelection();

            // Restore a previous press before capturing the current resting pose.
            // Show animations may have changed the pose since OnEnable.
            StopPress();
            if (!animatePress || punchDuration <= 0f) return;
            CaptureScale();
            Transform target = _capturedTarget;
            _pressTween = target.DOPunchScale(
                    Vector3.Scale(_originalScale, Vector3.one * punchScale), punchDuration, punchVibrato)
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(() =>
                {
                    _pressTween = null;
                    if (target != null) target.localScale = _originalScale;
                });
        }

        private void CaptureScale()
        {
            _capturedTarget = effectTarget != null ? effectTarget : transform;
            _originalScale = _capturedTarget.localScale;
        }

        private void StopPress()
        {
            if (_pressTween == null) return;
            if (DOTween.instance != null) _pressTween.Kill(false);
            _pressTween = null;
            if (_capturedTarget != null) _capturedTarget.localScale = _originalScale;
        }

        private void OnDisable()
        {
            if (_button != null) _button.onClick.RemoveListener(PlayFeedback);
            StopPress();
        }
                                                                                                                                                                                                                                                       
        private void OnDestroy() => OnDisable();
    }
}
