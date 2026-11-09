using _PawSlidePopGame._Scripts.Core.System.GameFlow;
using _PawSlidePopGame._Scripts.Data.Events;
using _PawSlidePopGame._Scripts.Data.Events.Payloads;
using _PawSlidePopGame._Scripts.Feature.Match3.Flow;
using _PawSlidePopGame._Scripts.UI.Manager;
using _PawSlidePopGame._Scripts.UI.Popups;
using _PawSlidePopGame.Scripts.DesignPattern.ObserverPattern;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.UI.Screens.Gameplay
{
    public sealed class GameplayPausePopupPresenter : MonoBehaviour
    {
        private void OnEnable()
        {
            EventManager<LogicGameEvent>.AddListener<InGameSubStateChangedPayload>(
                LogicGameEvent.InGameSubStateChanged,
                HandleSubStateChanged);

            SyncPausePopup(GameFlowManager.Instance != null
                ? GameFlowManager.Instance.CurrentInGameSubState
                : InGameSubState.None);
        }

        private void OnDisable()
        {
            EventManager<LogicGameEvent>.RemoveListener<InGameSubStateChangedPayload>(
                LogicGameEvent.InGameSubStateChanged,
                HandleSubStateChanged);
        }

        private void HandleSubStateChanged(InGameSubStateChangedPayload payload)
        {
            SyncPausePopup(payload.Current);
        }

        private static void SyncPausePopup(InGameSubState subState)
        {
            UIManager uiManager = UIManager.Instance;
            if (uiManager == null)
            {
                return;
            }

            bool shouldShowPausePopup = subState == InGameSubState.Paused;
            if (shouldShowPausePopup)
            {
                if (!uiManager.IsPopupVisible<PausePopup>())
                {
                    uiManager.ShowPopup<PausePopup>();
                }

                return;
            }

            if (uiManager.IsPopupVisible<PausePopup>())
            {
                uiManager.ClosePopup<PausePopup>();
            }
        }
    }
}
