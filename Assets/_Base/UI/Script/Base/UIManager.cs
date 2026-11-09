using System;
using System.Collections.Generic;
using MazFramework.Bootstrap;
using MazFramework.Foundation.Singleton;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using MazFramework.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MazFramework.UI
{
    [DefaultExecutionOrder(-1500)]
    [DisallowMultipleComponent, SingletonPrefab]
    public sealed class UIManager : Singleton<UIManager>, IAppSystem, ILoadingOverlay
    {
        [Header("Roots")]
        [SerializeField] private Transform screenRoot;
        [SerializeField] private Transform popupRoot;
        [SerializeField] private Transform topRoot;
        [SerializeField] private Camera uiCamera;

        private const string ScreenResourcesPath = "UI/Screens";
        private const string PopupResourcesPath = "UI/Popups";
        private const string OverlayResourcesPath = "UI/Overlays";

        public void ShowToast(string message, float duration = -1f)
        {
            if (!_ready || topRoot == null) return;
            ToastNotification toast = GetOrCreateOverlay<ToastNotification>();
            if (toast == null) return;
            ShowOverlay<ToastNotification>(duration < 0f ? toast.DefaultStayDuration : duration,
                view => view.SetMessage(message));
        }

        private sealed class LoadingRequest
        {
            public long Id;
            public Action OnCovered;
            public bool Notified;
            public bool Canceled;
        }

        private readonly Dictionary<long, LoadingRequest> _loadingRequests = new Dictionary<long, LoadingRequest>();
        private long _nextLoadingRequestId;
        private int _loadingPresentationVersion;
        private LoadingScreen _loadingView;
        private bool _loadingCovered;

        public LoadingHandle BeginLoading(Action onCovered = null)
        {
            if (!_ready || !isActiveAndEnabled || topRoot == null || !topRoot.gameObject.activeInHierarchy)
            {
                Debug.LogError("[UIManager] Loading requires initialized UI and an active Overlay Root.", this);
                InvokeLoadingCallback(onCovered);
                return new LoadingHandle(null);
            }

            LoadingScreen view;
            try { view = GetOrCreateOverlay<LoadingScreen>(); }
            catch (Exception exception) { Debug.LogException(exception, this); view = null; }
            if (view == null)
            {
                InvokeLoadingCallback(onCovered);
                return new LoadingHandle(null);
            }

            var request = new LoadingRequest { Id = ++_nextLoadingRequestId, OnCovered = onCovered };
            _loadingRequests.Add(request.Id, request);
            var handle = new LoadingHandle(() =>
            {
                request.Canceled = true;
                if (this != null) ReleaseLoadingRequest(request.Id);
            });
            if (_loadingView != view || !view.IsVisible || view.IsClosing)
            {
                _loadingView = view;
                _loadingCovered = false;
                int version = ++_loadingPresentationVersion;
                try { view.Show(-1f, () => NotifyLoadingCovered(version, view)); }
                catch (Exception exception)
                {
                    Debug.LogException(exception, this);
                    var pending = new List<LoadingRequest>(_loadingRequests.Values);
                    CleanupLoading(false);
                    foreach (LoadingRequest failed in pending)
                    {
                        if (this == null || !isActiveAndEnabled) break;
                        if (failed.Notified || failed.Canceled) continue;
                        failed.Notified = true;
                        Action callback = failed.OnCovered;
                        failed.OnCovered = null;
                        InvokeLoadingCallback(callback);
                    }
                }
            }
            else if (_loadingCovered) NotifyLoadingRequest(request);
            return handle;
        }

        private void NotifyLoadingCovered(int version, LoadingScreen view)
        {
            if (this == null || !isActiveAndEnabled || version != _loadingPresentationVersion || view == null || view != _loadingView) return;
            _loadingCovered = true;
            var pending = new List<LoadingRequest>(_loadingRequests.Values);
            foreach (LoadingRequest request in pending)
            {
                if (version != _loadingPresentationVersion || !_loadingCovered) break;
                NotifyLoadingRequest(request);
            }
        }

        private void NotifyLoadingRequest(LoadingRequest request)
        {
            if (request.Notified || request.Canceled || !_loadingRequests.TryGetValue(request.Id, out LoadingRequest current) || !ReferenceEquals(request, current)) return;
            request.Notified = true;
            Action callback = request.OnCovered;
            request.OnCovered = null;
            InvokeLoadingCallback(callback);
        }

        private void InvokeLoadingCallback(Action callback)
        {
            try { callback?.Invoke(); }
            catch (Exception exception) { Debug.LogException(exception, this); }
        }

        private void ReleaseLoadingRequest(long id)
        {
            if (!_loadingRequests.Remove(id) || _loadingRequests.Count != 0) return;
            ++_loadingPresentationVersion;
            _loadingCovered = false;
            if (_loadingView != null) _loadingView.Hide();
        }

        private void CleanupLoading(bool cancelRequests = true)
        {
            ++_loadingPresentationVersion;
            _loadingCovered = false;
            if (cancelRequests)
                foreach (LoadingRequest request in _loadingRequests.Values)
                {
                    request.Canceled = true;
                    request.OnCovered = null;
                }
            _loadingRequests.Clear();
            _loadingView = null;
            if (_activeOverlays.TryGetValue(typeof(LoadingScreen), out BaseOverlay view))
            {
                _activeOverlays.Remove(typeof(LoadingScreen));
                if (view != null)
                {
                    view.Closed -= HandleOverlayClosed;
                    view.gameObject.SetActive(false);
                    Destroy(view.gameObject);
                }
            }
        }

        private readonly Dictionary<Type, BaseScreen> _screenCache = new Dictionary<Type, BaseScreen>();
        private readonly Dictionary<Type, BasePopup> _popupCache = new Dictionary<Type, BasePopup>();
        private readonly Dictionary<Type, BaseOverlay> _activeOverlays = new Dictionary<Type, BaseOverlay>();
        private readonly List<BasePopup> _popupStack = new List<BasePopup>();

        private Dictionary<string, GameObject> _screenPrefabRegistry;
        private Dictionary<string, GameObject> _popupPrefabRegistry;
        private Dictionary<string, GameObject> _overlayPrefabRegistry;

        private BaseScreen _currentScreen;
        private int _uiLayer;
        private bool _ready;
        private UniversalAdditionalCameraData _boundCameraData;
        private bool _addedCameraToStack;

        private Image _inputBlocker;
        public bool IsGameplayInputBlocked { get; private set; }
        public event Action<bool> GameplayInputBlockedChanged;
        public bool IsInitialized => _ready;
        public BaseScreen CurrentScreen => _currentScreen;
        public BasePopup TopPopup
        {
            get
            {
                PrunePopupStack();
                return _popupStack.Count > 0 ? _popupStack[_popupStack.Count - 1] : null;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            if (ExistingInstance == this) return;
            // A duplicate UI root must not leave its child canvases/camera active.
            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        public T ShowScreen<T>(Action<T> setup = null, Action onOpened = null) where T : BaseScreen
        {
            if (!CanShow(screenRoot, "Screen")) return null;

            T screen = GetOrCreateScreen<T>();
            if (screen == null) return null;

            if (_currentScreen != null && _currentScreen != screen)
            {
                _currentScreen.Hide();
            }

            _currentScreen = screen;
            RefreshInputState();
            screen.transform.SetAsLastSibling();
            setup?.Invoke(screen);
            screen.Show(onOpened);
            RefreshInputState();
            return screen;
        }

        public T ShowPopup<T>(Action<T> setup = null, Action onOpened = null) where T : BasePopup
        {
            if (!CanShow(popupRoot, "Popup")) return null;

            T popup = GetOrCreatePopup<T>();
            if (popup == null) return null;

            _popupStack.Remove(popup);
            _popupStack.Add(popup);
            popup.transform.SetAsLastSibling();
            setup?.Invoke(popup);
            popup.Show(onOpened);
            RefreshInputState();
            return popup;
        }

        public T ShowOverlay<T>(float duration = -1f, Action<T> setup = null, Action onOpened = null) where T : BaseOverlay
        {
            if (typeof(T) == typeof(LoadingScreen))
            {
                Debug.LogError("[UIManager] LoadingScreen is managed by BeginLoading handles.", this);
                return null;
            }
            if (float.IsNaN(duration) || float.IsInfinity(duration)) throw new ArgumentOutOfRangeException(nameof(duration));
            if (!CanShow(topRoot, "Overlay")) return null;
            T overlay = GetOrCreateOverlay<T>();
            if (overlay == null) return null;
            setup?.Invoke(overlay);
            overlay.Show(duration, onOpened);
            return overlay;
        }

        public bool CloseOverlay<T>() where T : BaseOverlay
        {
            if (typeof(T) == typeof(LoadingScreen))
            {
                Debug.LogError("[UIManager] Release the loading handle instead of closing LoadingScreen directly.", this);
                return false;
            }
            if (!_activeOverlays.TryGetValue(typeof(T), out BaseOverlay overlay) || overlay == null) return false;
            bool visible = overlay.IsVisible;
            overlay.Hide();
            return visible;
        }

        private T GetOrCreateOverlay<T>() where T : BaseOverlay
        {
            if (_activeOverlays.TryGetValue(typeof(T), out BaseOverlay existing) && existing != null) return (T)existing;
            if (_overlayPrefabRegistry == null) _overlayPrefabRegistry = BuildResourceRegistry(OverlayResourcesPath);
            T prefab = LoadPrefab<T>(OverlayResourcesPath, _overlayPrefabRegistry);
            if (prefab == null) return null;
            T overlay = Instantiate(prefab, topRoot);
            PrepareInstance(overlay.transform, prefab.name);
            overlay.InitializeHidden();
            overlay.PresentationStateChanged += RefreshInputState;
            overlay.Closed += HandleOverlayClosed;
            _activeOverlays[typeof(T)] = overlay;
            return overlay;
        }

        private void HandleOverlayClosed(BaseOverlay overlay)
        {
            overlay.Closed -= HandleOverlayClosed;
            overlay.PresentationStateChanged -= RefreshInputState;
            Type type = overlay.GetType();
            if (_activeOverlays.TryGetValue(type, out BaseOverlay current) && ReferenceEquals(current, overlay))
                _activeOverlays.Remove(type);
        }

        public bool Initialize()
        {
            if (_ready) return true;
            if (ExistingInstance != this || !isActiveAndEnabled) return false;
            if (screenRoot == null || popupRoot == null)
            {
                Debug.LogError("[UIManager] Screen Root and Popup Root are required.", this);
                return false;
            }

            if (screenRoot == popupRoot)
            {
                Debug.LogError("[UIManager] Screen Root and Popup Root must be different transforms.", this);
                return false;
            }

            if (uiCamera == null || !uiCamera.transform.IsChildOf(transform))
            {
                Debug.LogError("[UIManager] UI Camera must reference a camera under this UI root.", this);
                return false;
            }

            if (!uiCamera.TryGetComponent<UniversalAdditionalCameraData>(out var cameraData) ||
                cameraData.renderType != CameraRenderType.Overlay)
            {
                Debug.LogError("[UIManager] UI Camera must be a URP Overlay camera.", this);
                return false;
            }

            if (topRoot != null && (topRoot == screenRoot || topRoot == popupRoot))
            {
                Debug.LogError("[UIManager] Top Root must be separate from Screen Root and Popup Root.", this);
                return false;
            }

            if (!ValidateCanvasRoot(screenRoot, "Screen") || !ValidateCanvasRoot(popupRoot, "Popup") ||
                (topRoot != null && !ValidateCanvasRoot(topRoot, "Top"))) return false;

            _uiLayer = LayerMask.NameToLayer("UI");
            if (_uiLayer < 0)
            {
                Debug.LogError("[UIManager] Required Unity layer 'UI' does not exist.", this);
                return false;
            }

            ConfigureCanvas(screenRoot);
            ConfigureCanvas(popupRoot);
            if (topRoot != null) ConfigureCanvas(topRoot);
            uiCamera.cullingMask = 1 << _uiLayer;

            _ready = true;
            foreach (BaseScreen screen in screenRoot.GetComponentsInChildren<BaseScreen>(true))
            {
                if (_screenCache.ContainsKey(screen.GetType())) continue;
                _screenCache[screen.GetType()] = screen;
                screen.PresentationStateChanged += RefreshInputState;
                screen.InitializeHidden();
            }
            foreach (BasePopup popup in popupRoot.GetComponentsInChildren<BasePopup>(true))
            {
                if (_popupCache.ContainsKey(popup.GetType())) continue;
                _popupCache[popup.GetType()] = popup;
                popup.PresentationStateChanged += RefreshInputState;
                popup.Showing += HandlePopupShowing;
                popup.Hidden += HandlePopupHidden;
                popup.InitializeHidden();
            }
            if (topRoot != null)
            {
                foreach (BaseOverlay overlay in topRoot.GetComponentsInChildren<BaseOverlay>(true))
                {
                    if (_activeOverlays.ContainsKey(overlay.GetType())) continue;
                    _activeOverlays[overlay.GetType()] = overlay;
                    overlay.PresentationStateChanged += RefreshInputState;
                    overlay.Closed += HandleOverlayClosed;
                    overlay.InitializeHidden();
                }
            }
            InitializeInputBlocker();
            RefreshInputState();
            return true;
        }

        private bool ValidateCanvasRoot(Transform root, string label)
        {
            if (root != null && root != transform && root.IsChildOf(transform) && root.GetComponent<Canvas>() != null)
                return true;

            Debug.LogError($"[UIManager] {label} Root must be a Canvas under this UI root.", this);
            return false;
        }

        private void ConfigureCanvas(Transform root)
        {
            ApplyLayerRecursively(root, _uiLayer);
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = uiCamera;
            canvas.planeDistance = 100f;
        }

        public void BindSceneCamera(Camera baseCamera)
        {
            if (baseCamera == null)
            {
                ReleaseSceneCamera();
                return;
            }

            if (uiCamera == null || baseCamera == uiCamera ||
                !baseCamera.TryGetComponent<UniversalAdditionalCameraData>(out var data) ||
                data.renderType != CameraRenderType.Base)
            {
                Debug.LogError("[UIManager] BindSceneCamera requires the assigned UI Camera and a URP Base camera.", this);
                return;
            }

            if (_boundCameraData != data)
            {
                ReleaseSceneCamera();
                _boundCameraData = data;
            }

            // A scene-authored UI root may have been suppressed in favour of this persistent owner.
            data.cameraStack.RemoveAll(camera => camera == null);
            if (!data.cameraStack.Contains(uiCamera))
            {
                data.cameraStack.Add(uiCamera);
                _addedCameraToStack = true;
            }
        }

        private void ReleaseSceneCamera()
        {
            if (_addedCameraToStack && _boundCameraData != null)
                _boundCameraData.cameraStack.Remove(uiCamera);
            _boundCameraData = null;
            _addedCameraToStack = false;
        }

        private Dictionary<string, GameObject> BuildResourceRegistry(string resourcesFolder)
        {
            // LoadAll includes subfolders. Build each category only on its first use.
            GameObject[] prefabs = Resources.LoadAll<GameObject>(resourcesFolder);
            var registry = new Dictionary<string, GameObject>(StringComparer.Ordinal);
            foreach (GameObject prefab in prefabs)
            {
                if (prefab == null) continue;

                if (registry.ContainsKey(prefab.name))
                {
                    // Do not silently select a prefab based on LoadAll ordering.
                    registry[prefab.name] = null;
                    Debug.LogError($"[UIManager] Duplicate prefab name '{prefab.name}' under Resources/{resourcesFolder}. Names must be unique across subfolders.", prefab);
                    continue;
                }

                registry.Add(prefab.name, prefab);
            }

            return registry;
        }

        private T LoadPrefab<T>(string resourcesFolder, Dictionary<string, GameObject> registry) where T : Component
        {
            string prefabName = typeof(T).Name;
            if (!registry.TryGetValue(prefabName, out GameObject prefab))
            {
                Debug.LogError($"[UIManager] Missing prefab '{prefabName}' under Resources/{resourcesFolder} (including subfolders).", this);
                return null;
            }

            if (prefab == null)
            {
                Debug.LogError($"[UIManager] Cannot load '{prefabName}': duplicate prefab names under Resources/{resourcesFolder}.", this);
                return null;
            }

            T component = prefab.GetComponent<T>();
            if (component == null)
            {
                Debug.LogError($"[UIManager] Prefab '{prefabName}' under Resources/{resourcesFolder} requires '{typeof(T).Name}' on its root.", prefab);
            }

            return component;
        }

        private T GetOrCreateScreen<T>() where T : BaseScreen
        {
            if (_screenCache.TryGetValue(typeof(T), out BaseScreen cached) && cached != null)
            {
                return cached as T;
            }

            if (_screenPrefabRegistry == null)
            {
                _screenPrefabRegistry = BuildResourceRegistry(ScreenResourcesPath);
            }

            T prefab = LoadPrefab<T>(ScreenResourcesPath, _screenPrefabRegistry);
            if (prefab == null) return null;

            T instance = Instantiate(prefab, screenRoot);
            PrepareInstance(instance.transform, prefab.name);
            instance.PresentationStateChanged += RefreshInputState;
            instance.InitializeHidden();
            _screenCache[typeof(T)] = instance;
            return instance;
        }

        private T GetOrCreatePopup<T>() where T : BasePopup
        {
            if (_popupCache.TryGetValue(typeof(T), out BasePopup cached) && cached != null)
            {
                return cached as T;
            }

            if (_popupPrefabRegistry == null)
            {
                _popupPrefabRegistry = BuildResourceRegistry(PopupResourcesPath);
            }

            T prefab = LoadPrefab<T>(PopupResourcesPath, _popupPrefabRegistry);
            if (prefab == null) return null;

            T instance = Instantiate(prefab, popupRoot);
            PrepareInstance(instance.transform, prefab.name);
            instance.InitializeHidden();
            instance.PresentationStateChanged += RefreshInputState;
            instance.Showing += HandlePopupShowing;
            instance.Hidden += HandlePopupHidden;
            _popupCache[typeof(T)] = instance;
            return instance;
        }

        private void PrepareInstance(Transform instance, string prefabName)
        {
            instance.name = prefabName;
            ApplyLayerRecursively(instance, _uiLayer);
        }

        private void HandlePopupShowing(BasePopup popup)
        {
            if (!_popupStack.Contains(popup))
            {
                _popupStack.Add(popup);
                popup.transform.SetAsLastSibling();
            }
            RefreshInputState();
        }

        private void HandlePopupHidden(BasePopup popup)
        {
            _popupStack.Remove(popup);
            RefreshInputState();
        }

        private void PrunePopupStack()
        {
            for (int i = _popupStack.Count - 1; i >= 0; i--)
            {
                BasePopup popup = _popupStack[i];
                if (popup == null || !popup.IsPresented || !popup.isActiveAndEnabled)
                {
                    _popupStack.RemoveAt(i);
                }
            }
        }

        private bool CanShow(Transform root, string label)
        {
            if (!_ready)
            {
                LogNotReady();
                return false;
            }

            if (root == null)
            {
                Debug.LogError($"[UIManager] {label} Root is missing.", this);
                return false;
            }

            return true;
        }

        private void LogNotReady()
        {
            Debug.LogError("[UIManager] Manager is not configured correctly.", this);
        }

        private static void ApplyLayerRecursively(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            for (int i = 0; i < root.childCount; i++)
            {
                ApplyLayerRecursively(root.GetChild(i), layer);
            }
        }

        public void CloseTopPopup() => TopPopup?.Hide();

        public bool ClosePopup<T>() where T : BasePopup
        {
            if (!_popupCache.TryGetValue(typeof(T), out BasePopup popup) || popup == null) return false;
            bool visible = popup.IsVisible;
            popup.Hide();
            return visible;
        }

        public bool IsPopupVisible<T>() where T : BasePopup =>
            _popupCache.TryGetValue(typeof(T), out BasePopup popup) && popup != null && popup.IsVisible;

        public void ClearAllPopups()
        {
            // Closing popups remain in the stack until Hidden, so input cannot fall through their animation.
            foreach (BasePopup popup in _popupStack.ToArray())
                if (popup != null) popup.Hide();
        }

        public void HideCurrentScreen()
        {
            BaseScreen current = _currentScreen;
            _currentScreen = null;
            if (current != null) current.Hide();
            RefreshInputState();
        }

        private void InitializeInputBlocker()
        {
            var blocker = new GameObject("UIInputBlocker", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            blocker.layer = _uiLayer;
            blocker.transform.SetParent(popupRoot, false);
            var rect = (RectTransform)blocker.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            _inputBlocker = blocker.GetComponent<Image>();
            _inputBlocker.color = Color.clear;
            _inputBlocker.raycastTarget = true;
            _inputBlocker.canvasRenderer.cullTransparentMesh = false;
            blocker.SetActive(false);
            if (GetComponent<UIBackInput>() == null) gameObject.AddComponent<UIBackInput>();
        }

        private bool HasBlockingOverlay()
        {
            foreach (BaseOverlay overlay in _activeOverlays.Values)
                if (overlay != null && overlay.IsInputBlocking) return true;
            return false;
        }

        private void RefreshInputState()
        {
            if (!_ready) return;
            bool active = isActiveAndEnabled;
            bool overlayBlocks = active && HasBlockingOverlay();
            BasePopup top = active ? TopPopup : null;
            bool screenTransition = active && _currentScreen != null && _currentScreen.IsTransitioning;

            foreach (BaseScreen screen in _screenCache.Values)
                if (screen != null) screen.SetNavigationInputAllowed(active && screen == _currentScreen && !overlayBlocks && top == null);
            foreach (BasePopup popup in _popupCache.Values)
                if (popup != null) popup.SetNavigationInputAllowed(active && popup == top && !overlayBlocks);

            bool blocked = active && (overlayBlocks || top != null || screenTransition);
            if (_inputBlocker != null)
            {
                _inputBlocker.gameObject.SetActive(blocked);
                if (blocked)
                {
                    // One blocker covers loading, popup transitions and screen transitions.
                    _inputBlocker.transform.SetParent(overlayBlocks ? topRoot : popupRoot, false);
                    _inputBlocker.transform.SetAsLastSibling();
                    if (!overlayBlocks && top != null && top.IsVisible && !top.IsTransitioning)
                        top.transform.SetAsLastSibling();
                }
            }
            SetGameplayInputBlocked(blocked);

            // Drop invalid focus; no focus history or automatic selection policy.
            var eventSystem = EventSystem.current;
            var selected = eventSystem != null ? eventSystem.currentSelectedGameObject : null;
            if (selected != null && (!selected.activeInHierarchy ||
                (selected.TryGetComponent<Selectable>(out var control) && !control.IsInteractable())))
                eventSystem.SetSelectedGameObject(null);
        }

        private void SetGameplayInputBlocked(bool blocked)
        {
            if (blocked == IsGameplayInputBlocked) return;
            IsGameplayInputBlocked = blocked;
            GameplayInputBlockedChanged?.Invoke(blocked);
        }

        public bool TryHandleBack()
        {
            if (!_ready || !isActiveAndEnabled) return true;
            if (HasBlockingOverlay()) return true;
            BasePopup top = TopPopup;
            if (top == null) return _currentScreen != null && _currentScreen.IsTransitioning;
            if (top.IsVisible && !top.IsTransitioning) top.TryHandleBack();
            return true;
        }

        private void OnEnable() => RefreshInputState();

        private void OnDisable()
        {
            if (ExistingInstance == this) { CleanupLoading(); RefreshInputState(); }
        }

        protected override void OnApplicationQuit()
        {
            base.OnApplicationQuit();
            if (ExistingInstance == this) gameObject.SetActive(false);
        }

        protected override void OnDestroy()
        {
            if (ExistingInstance == this)
            {
                _ready = false;
                CleanupLoading();
                ReleaseSceneCamera();
                foreach (BaseScreen screen in _screenCache.Values)
                    if (screen != null) screen.PresentationStateChanged -= RefreshInputState;
                foreach (BasePopup popup in _popupCache.Values)
                {
                    if (popup == null) continue;
                    popup.PresentationStateChanged -= RefreshInputState;
                    popup.Showing -= HandlePopupShowing;
                    popup.Hidden -= HandlePopupHidden;
                }
                foreach (BaseOverlay overlay in _activeOverlays.Values)
                    if (overlay != null)
                    {
                        overlay.PresentationStateChanged -= RefreshInputState;
                        overlay.Closed -= HandleOverlayClosed;
                    }
                _activeOverlays.Clear();
                _screenCache.Clear();
                _popupCache.Clear();
                _popupStack.Clear();
                SetGameplayInputBlocked(false);
                GameplayInputBlockedChanged = null;
            }
            base.OnDestroy();
        }
    }
}
