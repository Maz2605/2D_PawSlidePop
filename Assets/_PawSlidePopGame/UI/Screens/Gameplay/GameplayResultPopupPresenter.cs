using _PawSlidePopGame._Scripts.Core.System.GameFlow;
using _PawSlidePopGame._Scripts.Core.Vibration;
using _PawSlidePopGame._Scripts.Data.Events;
using _PawSlidePopGame._Scripts.Data.Events.Payloads;
using _PawSlidePopGame._Scripts.Feature.Match3.Flow;
using _PawSlidePopGame._Scripts.Feature.Match3.Presenter;
using _PawSlidePopGame._Scripts.Feature.Match3.View;
using _PawSlidePopGame._Scripts.Gameplay.Meta.MapManager;
using _PawSlidePopGame._Scripts.UI.Manager;
using _PawSlidePopGame._Scripts.UI.Popups;
using _PawSlidePopGame.Scripts.DesignPattern.ObserverPattern;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.UI.Screens.Gameplay
{
    public sealed class GameplayResultPopupPresenter : MonoBehaviour
    {
        private void OnEnable()
        {
            EventManager<LogicGameEvent>.AddListener<GameplayHudSnapshot>(LogicGameEvent.GameplayWon, HandleGameplayWon);
            EventManager<LogicGameEvent>.AddListener<GameplayHudSnapshot>(LogicGameEvent.GameplayLost, HandleGameplayLost);
        }

        private void OnDisable()
        {
            EventManager<LogicGameEvent>.RemoveListener<GameplayHudSnapshot>(LogicGameEvent.GameplayWon, HandleGameplayWon);
            EventManager<LogicGameEvent>.RemoveListener<GameplayHudSnapshot>(LogicGameEvent.GameplayLost, HandleGameplayLost);
        }

        private void HandleGameplayWon(GameplayHudSnapshot snapshot)
        {
            StartCoroutine(ShowWinPopupDelayed(snapshot));
        }

        private System.Collections.IEnumerator ShowWinPopupDelayed(GameplayHudSnapshot snapshot)
        {
            var boardPresenter = Match3BoardPresenter.Instance;
            if (boardPresenter != null)
            {
                yield return new WaitUntil(() => !boardPresenter.IsVictoryOutroPlaying);
            }

            GameplayHudSnapshot resolvedSnapshot = GameFlowManager.Instance?.LastSnapshot?.Clone() ?? snapshot?.Clone();
            if (resolvedSnapshot == null)
            {
                Debug.LogWarning("[GameplayResultPopupPresenter] Missing GameplayHudSnapshot for WinPopup.");
                yield break;
            }

            RecordLevelProgress(resolvedSnapshot);
            UIManager.Instance?.ShowPopup<WinPopup>(
                popup => popup.SetSnapshot(resolvedSnapshot));
        }

        private void HandleGameplayLost(GameplayHudSnapshot snapshot)
        {
            GameplayHudSnapshot resolvedSnapshot = snapshot?.Clone() ?? GameFlowManager.Instance?.LastSnapshot?.Clone();
            if (resolvedSnapshot == null)
            {
                Debug.LogWarning("[GameplayResultPopupPresenter] Missing GameplayHudSnapshot for LosePopup.");
                return;
            }

            float delay = 1.8f;
            var boardView = Match3BoardView.Instance;
            if (boardView != null)
            {
                delay = boardView.LoseOutroDelayBeforePopup;
            }

            StartCoroutine(ShowLosePopupDelayed(resolvedSnapshot, delay));
        }

        private System.Collections.IEnumerator ShowLosePopupDelayed(GameplayHudSnapshot snapshot, float delay)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            UIManager.Instance?.ShowPopup<LosePopup>(
                popup => popup.SetSnapshot(snapshot));
        }

        private static void RecordLevelProgress(GameplayHudSnapshot snapshot)
        {
            string levelId = GameAppFlowManager.Instance != null ? GameAppFlowManager.Instance.CurrentLevelId : null;
            if (string.IsNullOrWhiteSpace(levelId))
            {
                Debug.LogWarning("[GameplayResultPopupPresenter] Cannot save level progress because CurrentLevelId is empty.");
                return;
            }

            LevelProgressRepository.Instance.RecordWinAndAdvance(levelId, snapshot.reachedStars, snapshot.currentScore);
        }
    }
}
