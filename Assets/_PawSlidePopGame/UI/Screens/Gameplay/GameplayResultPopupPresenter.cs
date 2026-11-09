using System.Collections;
using _PawSlidePopGame._Scripts.Core.System.GameFlow;
using _PawSlidePopGame._Scripts.Data.Events.Payloads;
using _PawSlidePopGame._Scripts.Feature.Match3.View;
using _PawSlidePopGame._Scripts.UI.Popups;
using MazFramework.UI;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.UI.Screens.Gameplay
{
    public sealed class GameplayResultPopupPresenter : MonoBehaviour
    {
        private GameAppFlowManager _app;
        private GameplayScreen _screen;
        private Coroutine _pending;
        private int _version;
        private void OnEnable()
        {
            _screen = GetComponent<GameplayScreen>();
            if (_screen != null) _screen.HidingStarted += CancelPending;
            Bind();
        }
        private void LateUpdate()
        {
            if (_app != GameAppFlowManager.ExistingInstance) Bind();
        }
        private void Bind()
        {
            Unbind();
            _app = GameAppFlowManager.ExistingInstance;
            if (_app == null) return;
            _app.ResultReady += HandleResult;
            _app.SessionChanged += HandleSessionChanged;
        }
        private void Unbind()
        {
            if (_app == null) return;
            _app.ResultReady -= HandleResult;
            _app.SessionChanged -= HandleSessionChanged;
            _app = null;
        }
        private void HandleSessionChanged(long sessionId) => CancelPending();
        private void HandleResult(long sessionId, bool won, GameplayHudSnapshot snapshot)
        {
            CancelPending();
            if (_app == null || sessionId <= 0 || sessionId != _app.CurrentSessionId || snapshot == null) return;
            float delay = won ? 0f : (Match3BoardView.Instance != null ? Match3BoardView.Instance.LoseOutroDelayBeforePopup : 1.8f);
            _pending = StartCoroutine(ShowResult(sessionId, won, snapshot.Clone(), delay, _version));
        }
        private IEnumerator ShowResult(long sessionId, bool won, GameplayHudSnapshot snapshot, float delay, int version)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            if (version != _version || !isActiveAndEnabled || _app == null ||
                sessionId != _app.CurrentSessionId || (_screen != null && !_screen.IsVisible)) yield break;
            _pending = null;
            if (won) UIManager.Instance?.ShowPopup<WinPopup>(popup => popup.SetSnapshot(snapshot));
            else UIManager.Instance?.ShowPopup<LosePopup>(popup => popup.SetSnapshot(snapshot));
        }
        private void CancelPending()
        {
            _version++;
            if (_pending != null) StopCoroutine(_pending);
            _pending = null;
        }
        private void OnDisable()
        {
            CancelPending();
            if (_screen != null) _screen.HidingStarted -= CancelPending;
            Unbind();
        }
    }
}
