using System;
using System.Collections.Generic;
using _PawSlidePopGame._Scripts.UI.Base;
using _PawSlidePopGame._Scripts.UI.Components.NavigationBar;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.UI.Screens
{
    [Serializable]
    public struct TabSubScreenBinding
    {
        public MainTabID tabId;
        public BaseSubScreen subScreen;
    }

    public class GameMenuScreen : BaseScreen
    {
        [Header("--- Tab Navigation ---")]
        [SerializeField] private NavigationBar navigationBar;
        [SerializeField] private MainTabID defaultTab = MainTabID.MapTab;
        [SerializeField] private TabSubScreenBinding[] tabSubScreens;

        private readonly Dictionary<MainTabID, BaseSubScreen> _subScreenLookup = new Dictionary<MainTabID, BaseSubScreen>();

        private bool _isInitialized;
        private MainTabID _currentTab = (MainTabID)(-1);
        private BaseSubScreen _currentSubScreen;

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void OnBeforeShow()
        {
            EnsureInitialized();
            HideAllSubScreens();
            _currentTab = (MainTabID)(-1);
            _currentSubScreen = null;

            if (navigationBar == null)
            {
                return;
            }

            // [ĐÃ SỬA LỖI]: Gọi hàm ChangeTab với 2 tham số theo bản refactor mới nhất
            navigationBar.ChangeTab(defaultTab, true);
            HandleTabChanged(defaultTab);
        }

        private void OnDestroy()
        {
            if (navigationBar != null)
            {
                navigationBar.OnTabClicked -= HandleTabChanged;
            }
        }


        private void EnsureInitialized()
        {
            if (_isInitialized)
            {
                return;
            }

            ValidateBindings();

            if (navigationBar == null)
            {
                Debug.LogError($"[GameMenuScreen] Missing NavigationBar reference on '{name}'.", this);
                return;
            }

            navigationBar.Init();
            
            // Xóa đăng ký trước để phòng double-trigger event
            navigationBar.OnTabClicked -= HandleTabChanged;
            navigationBar.OnTabClicked += HandleTabChanged;

            if (tabSubScreens != null)
            {
                foreach (var binding in tabSubScreens)
                {
                    if (binding.subScreen == null)
                    {
                        continue;
                    }

                    binding.subScreen.Init();
                    binding.subScreen.Hide();
                }
            }

            _isInitialized = true;
        }

        private void HandleTabChanged(MainTabID tabId)
        {
            if (_currentTab == tabId && _currentSubScreen != null)
            {
                return;
            }

            if (!TryGetSubScreen(tabId, out BaseSubScreen nextSubScreen))
            {
                Debug.LogError($"[GameMenuScreen] No subScreen mapped for tab '{tabId}' on '{name}'.", this);
                return;
            }

            _currentSubScreen?.Hide();

            _currentTab = tabId;
            _currentSubScreen = nextSubScreen;
            _currentSubScreen.Show();
        }

        private bool TryGetSubScreen(MainTabID tabId, out BaseSubScreen subScreen)
        {
            if (_subScreenLookup.TryGetValue(tabId, out subScreen) && subScreen != null)
            {
                return true;
            }

            subScreen = null;
            return false;
        }

        private void HideAllSubScreens()
        {
            if (tabSubScreens == null)
            {
                return;
            }

            foreach (var binding in tabSubScreens)
            {
                binding.subScreen?.Hide();
            }
        }

        private void ValidateBindings()
        {
            _subScreenLookup.Clear();

            if (tabSubScreens == null || tabSubScreens.Length == 0)
            {
                Debug.LogWarning($"[GameMenuScreen] No tab-subScreen bindings configured on '{name}'.", this);
                return;
            }

            for (int i = 0; i < tabSubScreens.Length; i++)
            {
                TabSubScreenBinding binding = tabSubScreens[i];

                if (binding.subScreen == null)
                {
                    Debug.LogError($"[GameMenuScreen] Binding index {i} for tab '{binding.tabId}' is missing subScreen reference on '{name}'.", this);
                    continue;
                }

                if (_subScreenLookup.ContainsKey(binding.tabId))
                {
                    Debug.LogError($"[GameMenuScreen] Duplicate binding found for tab '{binding.tabId}' on '{name}'.", this);
                    continue;
                }

                _subScreenLookup.Add(binding.tabId, binding.subScreen);
            }

            if (!_subScreenLookup.ContainsKey(defaultTab))
            {
                Debug.LogError($"[GameMenuScreen] Default tab '{defaultTab}' has no subScreen binding on '{name}'.", this);
            }
        }
    }
}
