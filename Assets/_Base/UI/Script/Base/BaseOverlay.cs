using System;
using DG.Tweening;
using UnityEngine;

namespace MazFramework.UI
{
    [DisallowMultipleComponent, RequireComponent(typeof(CanvasGroup))]
    public abstract class BaseOverlay : MonoBehaviour
    {
        private CanvasGroup _group;
        private Tween _transition;
        private Tween _autoClose;
        private int _version;
        private bool _closed;

        protected CanvasGroup Group => _group != null ? _group : (_group = GetComponent<CanvasGroup>());
        protected virtual bool BlocksRaycasts => true;
        public bool IsVisible { get; private set; }
        public bool IsClosing { get; private set; }
        public bool IsInputBlocking => BlocksRaycasts && IsVisible && isActiveAndEnabled;
        public event Action PresentationStateChanged;
        public event Action<BaseOverlay> Closed;

        public void InitializeHidden()
        {
            CancelOwnedTweens();
            IsVisible = false;
            IsClosing = false;
            Group.alpha = 0f;
            Group.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        // Duration is the visible hold time after the entrance animation; negative means manual close.
        public void Show(float duration = -1f, Action onShown = null)
        {
            if (float.IsNaN(duration) || float.IsInfinity(duration)) throw new ArgumentOutOfRangeException(nameof(duration));
            if (_closed) return;
            CancelOwnedTweens();
            int version = ++_version;
            IsVisible = true;
            IsClosing = false;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Group.blocksRaycasts = BlocksRaycasts;
            PresentationStateChanged?.Invoke();
            OnBeforeShow();
            Tween transition = CreateShowTween();
            Action complete = () =>
            {
                if (version != _version || !IsVisible || IsClosing || _closed) return;
                _transition = null;
                if (duration >= 0f)
                    _autoClose = DOVirtual.DelayedCall(duration, Hide).SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDisable);
                InvokeSafely(OnShown);
                if (this == null || version != _version || !IsVisible || IsClosing || _closed || !isActiveAndEnabled) return;
                InvokeSafely(onShown);
            };
            if (transition == null) complete();
            else _transition = transition.SetUpdate(true).OnComplete(() => complete());
        }

        public void Hide()
        {
            if (!IsVisible || IsClosing || _closed) return;
            CancelOwnedTweens();
            int version = ++_version;
            IsClosing = true;
            OnBeforeHide();
            Tween transition = CreateHideTween();
            Action complete = () =>
            {
                if (version != _version || _closed) return;
                _transition = null;
                IsVisible = false;
                IsClosing = false;
                _closed = true;
                Group.blocksRaycasts = false;
                PresentationStateChanged?.Invoke();
                try { Closed?.Invoke(this); }
                finally { Destroy(gameObject); }
            };
            if (transition == null) complete();
            else _transition = transition.SetUpdate(true).OnComplete(() => complete());
        }

        protected virtual void OnBeforeShow() { }
        protected virtual void OnShown() { }
        protected virtual void OnBeforeHide() { }
        protected virtual Tween CreateShowTween() => Group.DOFade(1f, .2f);
        protected virtual Tween CreateHideTween() => Group.DOFade(0f, .15f);

        private void InvokeSafely(Action callback)
        {
            try { callback?.Invoke(); }
            catch (Exception exception) { Debug.LogException(exception, this); }
        }

        private void CancelOwnedTweens()
        {
            _transition?.Kill();
            _transition = null;
            _autoClose?.Kill();
            _autoClose = null;
        }

        protected virtual void OnDisable()
        {
            ++_version;
            CancelOwnedTweens();
            IsVisible = false;
            IsClosing = false;
            PresentationStateChanged?.Invoke();
            CanvasGroup group = Group;
            if (group != null)
            {
                group.alpha = 0f;
                group.blocksRaycasts = false;
            }
        }

        protected virtual void OnDestroy() => CancelOwnedTweens();
    }
}
