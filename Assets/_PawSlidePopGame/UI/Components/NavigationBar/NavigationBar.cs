using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace _PawSlidePopGame._Scripts.UI.Components.NavigationBar
{
    [Serializable]
    public class NavTab
    {
        public MainTabID tabID;
        public Button button;
        
        [Header("Visual References")]
        public RectTransform iconRoot;
        public TextMeshProUGUI label;

        [NonSerialized] public Vector2 defaultIconAnchoredPos;
        [NonSerialized] public Sequence animSeq;
        [NonSerialized] public UnityAction clickHandler;
    }

    [RequireComponent(typeof(RectTransform))]
    public class NavigationBar : MonoBehaviour
    {
        [Header("--- Tabs Setup ---")]
        [SerializeField] private NavTab[] tabs;

        [Header("--- Visual Settings (Toy Blast Style) ---")]
        [SerializeField] private float transitionDuration = 0.25f;
        [SerializeField] private float activeContentOffsetY = 35f;
        [SerializeField] private float activeIconScale = 1.15f;
        [SerializeField] private Ease bounceEase = Ease.OutBack;

        public event Action<MainTabID> OnTabClicked;
        
        public MainTabID CurrentTab => _currentTab;
        private MainTabID _currentTab = (MainTabID)(-1);
        private bool _isInitialized;

        public void Init()
        {
            if (_isInitialized) return;

            if (tabs == null || tabs.Length == 0) return;

            foreach (var tab in tabs)
            {
                if (tab == null) continue;

                // Cache vị trí mặc định ban đầu của Icon để nâng/hạ chính xác
                if (tab.iconRoot != null)
                {
                    tab.defaultIconAnchoredPos = tab.iconRoot.anchoredPosition;
                }

                // Thiết lập trạng thái visual mặc định ban đầu (inactive)
                SetTabVisualInstant(tab, false);

                // Gán sự kiện Click cho Button
                if (tab.button != null)
                {
                    MainTabID id = tab.tabID;
                    if (tab.clickHandler != null) tab.button.onClick.RemoveListener(tab.clickHandler);
                    tab.clickHandler = () => ChangeTab(id);
                    tab.button.onClick.AddListener(tab.clickHandler);
                }
            }

            _isInitialized = true;
        }

        public void ChangeTab(MainTabID tabID, bool instant = false)
        {
            if (_currentTab == tabID && !instant) return;

            bool isNew = _currentTab != tabID;
            _currentTab = tabID;

            foreach (var tab in tabs)
            {
                if (tab == null) continue;
                AnimateTabSelection(tab, tab.tabID == tabID, instant);
            }

            if (isNew)
            {
                OnTabClicked?.Invoke(tabID);
            }
        }

        private void AnimateTabSelection(NavTab tab, bool selected, bool instant)
        {
            tab.animSeq?.Kill();

            Vector2 targetIconPos = tab.defaultIconAnchoredPos;
            Vector3 targetIconScale = Vector3.one;

            if (selected)
            {
                targetIconPos.y += activeContentOffsetY;
                targetIconScale = Vector3.one * activeIconScale;
            }

            if (instant)
            {
                if (tab.iconRoot != null)
                {
                    tab.iconRoot.anchoredPosition = targetIconPos;
                    tab.iconRoot.localScale = targetIconScale;
                }
                if (tab.label != null)
                {
                    tab.label.gameObject.SetActive(true);
                }
                return;
            }

            tab.animSeq = DOTween.Sequence();

            // Nhấc/hạ icon
            if (tab.iconRoot != null)
            {
                tab.animSeq.Join(tab.iconRoot.DOAnchorPos(targetIconPos, transitionDuration).SetEase(selected ? bounceEase : Ease.OutQuad));
                tab.animSeq.Join(tab.iconRoot.DOScale(targetIconScale, transitionDuration).SetEase(selected ? bounceEase : Ease.OutQuad));
            }
        }

        private void SetTabVisualInstant(NavTab tab, bool selected)
        {
            Vector2 targetIconPos = tab.defaultIconAnchoredPos;
            Vector3 targetIconScale = Vector3.one;

            if (selected)
            {
                targetIconPos.y += activeContentOffsetY;
                targetIconScale = Vector3.one * activeIconScale;
            }

            if (tab.iconRoot != null)
            {
                tab.iconRoot.anchoredPosition = targetIconPos;
                tab.iconRoot.localScale = targetIconScale;
            }
            if (tab.label != null)
            {
                tab.label.gameObject.SetActive(true);
            }
        }

        private void OnDestroy()
        {
            if (tabs != null)
            {
                foreach (var tab in tabs)
                {
                    tab?.animSeq?.Kill();
                }
            }
        }
        private void OnEnable()
        {
            if (tabs == null) return;
            foreach (var tab in tabs) if (tab?.button != null && tab.clickHandler != null) { tab.button.onClick.RemoveListener(tab.clickHandler); tab.button.onClick.AddListener(tab.clickHandler); }
        }
        private void OnDisable()
        {
            if (tabs == null) return;
            foreach (var tab in tabs) { if (tab?.button != null && tab.clickHandler != null) tab.button.onClick.RemoveListener(tab.clickHandler); if (DOTween.instance != null) tab?.animSeq?.Kill(); }
        }
    }
}
