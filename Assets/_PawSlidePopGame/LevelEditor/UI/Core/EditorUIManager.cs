using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Core.Boostrap;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.UI
{
    public sealed class EditorUIManager : MonoBehaviour, IAppService
    {
        [SerializeField] private Transform screenRoot;
        [SerializeField] private Transform popupRoot;
        [SerializeField] private Transform overlayRoot;
        [SerializeField] private EditorScreen initialScreen;

        private readonly Dictionary<int, EditorPopup> _popupCache = new Dictionary<int, EditorPopup>();
        private readonly Stack<EditorPopup> _popupStack = new Stack<EditorPopup>();

        private EditorScreen _currentScreen;
        private bool _isInitialized;

        public void Init()
        {
            HideCurrentScreen();
            ClearPopupInstances();

            if (initialScreen != null)
            {
                ShowScreen(initialScreen);
            }

            _isInitialized = true;
        }

        private void Start()
        {
            if (_isInitialized)
            {
                return;
            }

            Init();
        }

        public void ShowScreen(EditorScreen screen)
        {
            if (screen == null)
            {
                return;
            }

            if (_currentScreen != null && _currentScreen != screen)
            {
                _currentScreen.Hide();
            }

            MoveToRoot(screen.transform, screenRoot);
            screen.transform.SetAsLastSibling();
            screen.Show();
            _currentScreen = screen;
        }

        public T ShowPopup<T>(T popupPrefab) where T : EditorPopup
        {
            if (popupPrefab == null || popupRoot == null)
            {
                return null;
            }

            int key = popupPrefab.GetInstanceID();
            if (!_popupCache.TryGetValue(key, out EditorPopup instance) || instance == null)
            {
                instance = Instantiate(popupPrefab, popupRoot);
                instance.gameObject.SetActive(false);
                _popupCache[key] = instance;
            }

            T typedPopup = instance as T;
            if (typedPopup == null)
            {
                return null;
            }

            PushPopup(typedPopup);
            typedPopup.transform.SetAsLastSibling();
            typedPopup.Show();
            return typedPopup;
        }

        public void CloseTopPopup()
        {
            if (_popupStack.Count == 0)
            {
                return;
            }

            EditorPopup popup = _popupStack.Pop();
            popup?.Hide();
        }

        public void ClosePopup(EditorPopup popup)
        {
            if (popup == null)
            {
                return;
            }

            Stack<EditorPopup> reorderedStack = new Stack<EditorPopup>();
            while (_popupStack.Count > 0)
            {
                EditorPopup current = _popupStack.Pop();
                if (current == popup)
                {
                    current.Hide();
                    break;
                }

                reorderedStack.Push(current);
            }

            while (reorderedStack.Count > 0)
            {
                _popupStack.Push(reorderedStack.Pop());
            }
        }

        public Transform GetOverlayRoot()
        {
            return overlayRoot;
        }

        private void HideCurrentScreen()
        {
            if (_currentScreen != null)
            {
                _currentScreen.Hide();
                _currentScreen = null;
            }

            if (initialScreen != null)
            {
                initialScreen.Hide();
            }
        }

        private void ClearPopupInstances()
        {
            foreach (KeyValuePair<int, EditorPopup> pair in _popupCache)
            {
                if (pair.Value != null)
                {
                    pair.Value.Hide();
                }
            }

            _popupStack.Clear();
        }

        private void PushPopup(EditorPopup popup)
        {
            if (popup == null)
            {
                return;
            }

            Stack<EditorPopup> reorderedStack = new Stack<EditorPopup>();
            while (_popupStack.Count > 0)
            {
                EditorPopup current = _popupStack.Pop();
                if (current == popup)
                {
                    continue;
                }

                reorderedStack.Push(current);
            }

            while (reorderedStack.Count > 0)
            {
                _popupStack.Push(reorderedStack.Pop());
            }

            _popupStack.Push(popup);
        }

        private static void MoveToRoot(Transform target, Transform root)
        {
            if (target == null || root == null || target.parent == root)
            {
                return;
            }

            target.SetParent(root, false);
        }
    }
}
