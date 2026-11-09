using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

namespace MazFramework.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class BasePopup : MonoBehaviour
    {
        [Header("Transition")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField, Min(0f)] private float showDuration = 0.2f;
        [SerializeField, Min(0f)] private float hideDuration = 0.15f;
        [SerializeField, FormerlySerializedAs("showEase")] private Ease transitionShowEase = Ease.OutBack;
        [SerializeField, FormerlySerializedAs("hideEase")] private Ease transitionHideEase = Ease.InQuad;
        [SerializeField] private bool closeOnBack = true;

        private Tween _transition;
        private bool _navigationInputAllowed = true;
        private bool _opening;
        public bool IsPresented { get; private set; }
        public event Action PresentationStateChanged;

        public void SetNavigationInputAllowed(bool allowed)
        {
            _navigationInputAllowed = allowed;
            ApplyNavigationInput();
        }

        private void ApplyNavigationInput()
        {
            bool allowed = _navigationInputAllowed && IsVisible && !_opening && !IsTransitioning && isActiveAndEnabled;
            GetCanvasGroup().interactable = allowed;
            GetCanvasGroup().blocksRaycasts = allowed;
        }
        private int _transitionVersion;

        protected float ShowDuration => showDuration;
        protected float HideDuration => hideDuration;
        public bool IsVisible { get; private set; }
        public bool IsTransitioning => _opening || (_transition != null && _transition.IsActive());
        public bool CloseOnBack => closeOnBack;
        public virtual bool TryHandleBack()
        {
            if (IsPresented && !IsTransitioning && IsVisible && closeOnBack) Hide();
            return IsPresented || !IsVisible;
        }
        protected CanvasGroup CanvasGroup => GetCanvasGroup();

        public event Action<BasePopup> Showing;
        public event Action<BasePopup> Hiding;
        public event Action<BasePopup> Hidden;

        protected virtual void Awake() => GetCanvasGroup();

        public void Show(Action onShown = null)
        {
            if (IsVisible && !IsTransitioning)
            {
                onShown?.Invoke();
                return;
            }

            int version = BeginTransition();
            IsPresented = true;
            _opening = true;
            gameObject.SetActive(true);
            IsVisible = true;
            CanvasGroup group = GetCanvasGroup();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            PresentationStateChanged?.Invoke();
            OnBeforeShow();
            OnShowStarted();
            Showing?.Invoke(this);

            if (showDuration <= 0f)
            {
                CompleteShow(version, onShown);
                return;
            }

            Tween showTween = CreateShowTween(group, showDuration);
            _transition = showTween
                .SetUpdate(true).SetEase(showTween is Sequence ? Ease.Linear : transitionShowEase).SetTarget(this)
                .OnComplete(() => CompleteShow(version, onShown));
        }

        public void Hide() => Hide(null);

        public void Hide(Action onHidden)
        {
            if (!IsVisible)
            {
                onHidden?.Invoke();
                return;
            }

            int version = BeginTransition();
            IsVisible = false;
            CanvasGroup group = GetCanvasGroup();
            group.interactable = false;
            group.blocksRaycasts = false;
            _opening = false;
            PresentationStateChanged?.Invoke();
            OnBeforeHide();
            Hiding?.Invoke(this);

            if (hideDuration <= 0f)
            {
                CompleteHide(version, onHidden);
                return;
            }

            Tween hideTween = CreateHideTween(group, hideDuration);
            _transition = hideTween
                .SetUpdate(true).SetEase(hideTween is Sequence ? Ease.Linear : transitionHideEase).SetTarget(this)
                .OnComplete(() => CompleteHide(version, onHidden));
            PresentationStateChanged?.Invoke();
        }

        internal void InitializeHidden()
        {
            BeginTransition();
            IsVisible = false;
            IsPresented = false;
            _opening = false;
            CanvasGroup group = GetCanvasGroup();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        protected virtual Tween CreateShowTween(CanvasGroup group, float duration) =>
            DOTween.To(() => group.alpha, value => group.alpha = value, 1f, duration);

        protected virtual Tween CreateHideTween(CanvasGroup group, float duration) =>
            DOTween.To(() => group.alpha, value => group.alpha = value, 0f, duration);

        protected virtual void OnBeforeShow() { }
        protected virtual void OnShowStarted() { }
        protected virtual void OnShown() { }
        protected virtual void OnBeforeHide() { }
        protected virtual void OnHidden() { }

        protected virtual void OnDisable()
        {
            if (!Application.isPlaying || (!IsPresented && !IsVisible && !IsTransitioning)) return;
            int version = BeginTransition();
            IsVisible = false;
            IsPresented = false;
            _opening = false;
            CanvasGroup group = GetCanvasGroup();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            PresentationStateChanged?.Invoke();
            OnHidden();
            if (version == _transitionVersion && !IsVisible && this != null)
            {
                Hidden?.Invoke(this);
            }
        }

        protected virtual void OnDestroy() => _transition?.Kill(false);

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        }
#endif

        private int BeginTransition()
        {
            _transitionVersion++;
            _transition?.Kill(false);
            _transition = null;
            return _transitionVersion;
        }

        private void CompleteShow(int version, Action callback)
        {
            if (version != _transitionVersion || !IsVisible) return;
            _transition = null;
            CanvasGroup group = GetCanvasGroup();
            group.alpha = 1f;
            _opening = false;
            ApplyNavigationInput();
            PresentationStateChanged?.Invoke();
            OnShown();
            callback?.Invoke();
        }

        private void CompleteHide(int version, Action callback)
        {
            if (version != _transitionVersion || IsVisible) return;
            _transition = null;
            IsPresented = false;
            _opening = false;
            CanvasGroup group = GetCanvasGroup();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            PresentationStateChanged?.Invoke();
            OnHidden();
            if (version != _transitionVersion || IsVisible || this == null) return;
            Hidden?.Invoke(this);
            if (version != _transitionVersion || IsVisible || this == null) return;
            callback?.Invoke();
            // A close callback may reopen this same cached instance.
            if (version == _transitionVersion && !IsVisible && this != null)
            {
                gameObject.SetActive(false);
            }
        }

        private CanvasGroup GetCanvasGroup()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            return canvasGroup;
        }
    }
}
