using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Model;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.UI.Panels
{
    public sealed class LevelEditorPaletteSectionController : MonoBehaviour
    {
        [SerializeField] private LevelEditorSectionPanelVisual sectionVisual;
        [SerializeField] private RectTransform sectionRoot;
        [SerializeField] private Button sectionToggleButton;
        [SerializeField] private TMP_Text sectionTitleText;
        [SerializeField] private TMP_Text subTitleText;
        [SerializeField] private TMP_Text shortcutKeyText;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private LevelEditorItemTileButton itemTemplate;
        [SerializeField] private Vector2 shownAnchoredPosition;
        [SerializeField] private Vector2 hiddenAnchoredPosition;
        [SerializeField] private float showDuration = 0.2f;
        [SerializeField] private Ease showEase = Ease.OutCubic;
        [SerializeField] private float hideDuration = 0.18f;
        [SerializeField] private Ease hideEase = Ease.InCubic;

        private readonly List<LevelEditorItemTileButton> _spawnedItems = new List<LevelEditorItemTileButton>();
        private Tween _moveTween;

        public void BindToggle(UnityAction onToggleRequested)
        {
            Button toggleButton = sectionVisual != null ? sectionVisual.SectionToggleButton : sectionToggleButton;
            if (toggleButton == null)
            {
                return;
            }

            toggleButton.onClick.RemoveAllListeners();
            if (onToggleRequested != null)
            {
                toggleButton.onClick.AddListener(onToggleRequested);
            }
        }

        public void ConfigureTitleAndShortcut(string title, string shortcut)
        {
            TMP_Text titleText = sectionVisual != null ? sectionVisual.SectionTitleText : sectionTitleText;
            TMP_Text subtitleText = sectionVisual != null ? sectionVisual.SubTitleText : subTitleText;
            TMP_Text keyText = sectionVisual != null ? sectionVisual.ShortcutKeyText : shortcutKeyText;

            if (titleText != null)
            {
                titleText.text = title ?? string.Empty;
            }

            if (subtitleText != null)
            {
                subtitleText.text = title ?? string.Empty;
            }

            if (keyText != null)
            {
                keyText.text = shortcut ?? string.Empty;
            }
        }

        public void SetEntries(IReadOnlyList<LevelEditorPaletteEntryData> entries, int selectedId, Action<int> onSelected)
        {
            ClearSpawnedItems();

            RectTransform resolvedContentRoot = sectionVisual != null ? sectionVisual.ContentRoot : contentRoot;
            LevelEditorItemTileButton resolvedItemTemplate = sectionVisual != null ? sectionVisual.ItemTemplate : itemTemplate;
            ScrollRect resolvedScrollRect = sectionVisual != null ? sectionVisual.ScrollRect : scrollRect;

            if (resolvedContentRoot == null || resolvedItemTemplate == null || entries == null)
            {
                return;
            }

            resolvedItemTemplate.gameObject.SetActive(false);

            for (int i = 0; i < entries.Count; i++)
            {
                LevelEditorItemTileButton itemView = Instantiate(resolvedItemTemplate, resolvedContentRoot);
                itemView.gameObject.SetActive(true);
                itemView.Bind(entries[i], entries[i].Id == selectedId, onSelected);
                _spawnedItems.Add(itemView);
            }

            if (resolvedScrollRect != null)
            {
                resolvedScrollRect.verticalNormalizedPosition = 1f;
            }
        }

        public void ShowInstant()
        {
            KillTween();
            RectTransform resolvedSectionRoot = sectionVisual != null ? sectionVisual.SectionRoot : sectionRoot;
            if (resolvedSectionRoot != null)
            {
                resolvedSectionRoot.anchoredPosition = shownAnchoredPosition;
            }
        }

        public void HideInstant()
        {
            KillTween();
            RectTransform resolvedSectionRoot = sectionVisual != null ? sectionVisual.SectionRoot : sectionRoot;
            if (resolvedSectionRoot != null)
            {
                resolvedSectionRoot.anchoredPosition = hiddenAnchoredPosition;
            }
        }

        public void PlayShow()
        {
            RectTransform resolvedSectionRoot = sectionVisual != null ? sectionVisual.SectionRoot : sectionRoot;
            if (resolvedSectionRoot == null)
            {
                return;
            }

            KillTween();
            _moveTween = resolvedSectionRoot.DOAnchorPos(shownAnchoredPosition, showDuration)
                .SetEase(showEase)
                .SetLink(gameObject);
        }

        public void PlayHide()
        {
            RectTransform resolvedSectionRoot = sectionVisual != null ? sectionVisual.SectionRoot : sectionRoot;
            if (resolvedSectionRoot == null)
            {
                return;
            }

            KillTween();
            _moveTween = resolvedSectionRoot.DOAnchorPos(hiddenAnchoredPosition, hideDuration)
                .SetEase(hideEase)
                .SetLink(gameObject);
        }

        public int GetSiblingIndex()
        {
            RectTransform resolvedSectionRoot = sectionVisual != null ? sectionVisual.SectionRoot : sectionRoot;
            return resolvedSectionRoot != null ? resolvedSectionRoot.GetSiblingIndex() : -1;
        }

        public void SetSiblingIndex(int siblingIndex)
        {
            RectTransform resolvedSectionRoot = sectionVisual != null ? sectionVisual.SectionRoot : sectionRoot;
            if (resolvedSectionRoot == null)
            {
                return;
            }

            resolvedSectionRoot.SetSiblingIndex(siblingIndex);
        }

        private void OnDisable()
        {
            KillTween();
        }

        private void OnDestroy()
        {
            KillTween();
        }

        private void ClearSpawnedItems()
        {
            for (int i = 0; i < _spawnedItems.Count; i++)
            {
                if (_spawnedItems[i] != null)
                {
                    DestroyViewObject(_spawnedItems[i].gameObject);
                }
            }

            _spawnedItems.Clear();
        }

        private static void DestroyViewObject(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        private void KillTween()
        {
            if (_moveTween != null)
            {
                _moveTween.Kill(false);
                _moveTween = null;
            }
        }
    }
}
