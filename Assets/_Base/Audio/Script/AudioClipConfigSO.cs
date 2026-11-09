using System.Collections.Generic;
using UnityEngine;

namespace MazFramework.Audio
{
    /// <summary>Named single clips, with multiple variations only for win and lose SFX.</summary>
    [CreateAssetMenu(fileName = "AudioClipConfig", menuName = "Audio/Audio Clip Config")]
    public class AudioClipConfigSO : ScriptableObject
    {
        [Header("Music")]
        public AudioClip musicBackground;

        [Header("UI")]
        public AudioClip buttonClick;
        public AudioClip backSound;
        public AudioClip popupOpen;
        public AudioClip popupClose;
        public AudioClip toggleOn;
        public AudioClip toggleOff;

        [Header("Feedback")]
        public AudioClip successSound;
        public AudioClip warningSound;
        public AudioClip errorSound;
        public AudioClip notificationSound;

        [Header("Haptics")]
        public AudioClip lightImpact;
        public AudioClip mediumImpact;
        public AudioClip heavyImpact;

        [Header("Gameplay")]
        public List<AudioClip> sfxWin = new List<AudioClip>();
        public List<AudioClip> sfxLose = new List<AudioClip>();
        // [Tooltip("Empty example slot for a game-specific sound. Add other named single clips here as needed.")]
        // public AudioClip gameplaySound;

    }
}
