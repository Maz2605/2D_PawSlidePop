using MazFramework.Audio;
using MazFramework.Haptics;
using MazFramework.UI;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.UI.Components
{
    [DisallowMultipleComponent, RequireComponent(typeof(BasePopup))]
    public sealed class GamePopupFeedback : MonoBehaviour
    {
        [SerializeField] private bool playOpenSound = true;
        [SerializeField] private bool playCloseSound = true;
        [SerializeField] private bool playOpenHaptics = true;
        private BasePopup _popup;

        private void OnEnable()
        {
            _popup = GetComponent<BasePopup>();
            _popup.Showing -= HandleShowing;
            _popup.Showing += HandleShowing;
            _popup.Hiding -= HandleHiding;
            _popup.Hiding += HandleHiding;
        }

        private void HandleShowing(BasePopup popup)
        {
            if (playOpenSound)
            {
                var audio = AudioManager.ExistingInstance;
                audio?.PlaySfx(audio.Config?.popupOpen);
            }
            if (playOpenHaptics) HapticManager.ExistingInstance?.PlayLightImpact();
        }

        private void HandleHiding(BasePopup popup)
        {
            if (!playCloseSound) return;
            var audio = AudioManager.ExistingInstance;
            audio?.PlaySfx(audio.Config?.popupClose);
        }

        private void OnDisable()
        {
            if (_popup == null) return;
            _popup.Showing -= HandleShowing;
            _popup.Hiding -= HandleHiding;
        }
    }
}
