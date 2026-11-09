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
    public sealed class LevelEditorPopupSectionController : MonoBehaviour
    {
        [SerializeField] private LevelEditorSectionPanelVisual sectionVisual;
        [SerializeField] private Vector2 expandedRootAnchoredPosition;
        [SerializeField] private Vector2 collapsedRootAnchoredPosition;
        [SerializeField] private Vector2 expandedBodySizeDelta;
        [SerializeField] private Vector2 collapsedBodySizeDelta;
        [SerializeField] private float expandDuration = 0.2f;
        [SerializeField] private Ease expandEase = Ease.OutCubic;
        [SerializeField] private float collapseDuration = 0.16f;
        [SerializeField] private Ease collapseEase = Ease.InCubic;

        private readonly List<LevelEditorItemTileButton> _spawnedItems = new List<LevelEditorItemTileButton>();
        private Tween _rootTween;
        private Tween _bodyTween;

        public void BindToggle(UnityAction onToggleRequested)
        {
            Button toggleButton = sectionVisual != null ? sectionVisual.SectionToggleButton : null;
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

        public void ConfigureTitle(string title, string subTitle)
        {
            TMP_Text titleText = sectionVisual != null ? sectionVisual.SectionTitleText : null;
            TMP_Text subtitleText = sectionVisual != null ? sectionVisual.SubTitleText : null;
            if (titleText != null)
            {
                titleText.text = title ?? string.Empty;
            }

            if (subtitleText != null)
            {
                subtitleText.text = subTitle ?? string.Empty;
            }
        }

        public void SetEntries(IReadOnlyList<LevelEditorPaletteEntryData> entries, int selectedId, Action<int> onSelected)
        {
            ClearSpawnedItems();

            if (sectionVisual == null || sectionVisual.ContentRoot == null || sectionVisual.ItemTemplate == null || entries == null)
            {
                return;
            }

            sectionVisual.ItemTemplate.gameObject.SetActive(false);

            for (int i = 0; i < entries.Count; i++)
            {
                LevelEditorItemTileButton itemView = Instantiate(sectionVisual.ItemTemplate, sectionVisual.ContentRoot);
                itemView.gameObject.SetActive(true);
                itemView.Bind(entries[i], entries[i].Id == selectedId, onSelected);
                _spawnedItems.Add(itemView);
            }

            if (sectionVisual.ScrollRect != null)
            {
                sectionVisual.ScrollRect.verticalNormalizedPosition = 1f;
            }
        }

        public void PlayExpand()
        {
            RectTransform sectionRoot = sectionVisual != null ? sectionVisual.SectionRoot : null;
            RectTransform animatedBody = sectionVisual != null ? sectionVisual.AnimatedBody : null;
            if (sectionRoot == null)
            {
                return;
            }

            KillTweens();
            _rootTween = sectionRoot.DOAnchorPos(expandedRootAnchoredPosition, expandDuration)
                .SetEase(expandEase)
                .SetLink(gameObject);

            if (animatedBody != null)
            {
                _bodyTween = animatedBody.DOSizeDelta(expandedBodySizeDelta, expandDuration)
                    .SetEase(expandEase)
                    .SetLink(gameObject);
            }
        }

        public void PlayCollapse()
        {
            RectTransform sectionRoot = sectionVisual != null ? sectionVisual.SectionRoot : null;
            RectTransform animatedBody = sectionVisual != null ? sectionVisual.AnimatedBody : null;
            if (sectionRoot == null)
            {
                return;
            }

            KillTweens();
            _rootTween = sectionRoot.DOAnchorPos(collapsedRootAnchoredPosition, collapseDuration)
                .SetEase(collapseEase)
                .SetLink(gameObject);

            if (animatedBody != null)
            {
                _bodyTween = animatedBody.DOSizeDelta(collapsedBodySizeDelta, collapseDuration)
                    .SetEase(collapseEase)
                    .SetLink(gameObject);
            }
        }

        public void SetSiblingIndex(int siblingIndex)
        {
            RectTransform sectionRoot = sectionVisual != null ? sectionVisual.SectionRoot : null;
            if (sectionRoot == null)
            {
                return;
            }

            sectionRoot.SetSiblingIndex(siblingIndex);
        }

        public int GetSiblingIndex()
        {
            RectTransform sectionRoot = sectionVisual != null ? sectionVisual.SectionRoot : null;
            return sectionRoot != null ? sectionRoot.GetSiblingIndex() : -1;
        }

        public void SetVisible(bool visible)
        {
            if (sectionVisual != null && sectionVisual.SectionRoot != null)
            {
                sectionVisual.SectionRoot.gameObject.SetActive(visible);
            }
        }

        public bool HasVisual()
        {
            return sectionVisual != null;
        }

        private void OnDisable()
        {
            KillTweens();
        }

        private void OnDestroy()
        {
            KillTweens();
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

        private void KillTweens()
        {
            if (_rootTween != null)
            {
                _rootTween.Kill(false);
                _rootTween = null;
            }

            if (_bodyTween != null)
            {
                _bodyTween.Kill(false);
                _bodyTween = null;
            }
        }
    }
}
