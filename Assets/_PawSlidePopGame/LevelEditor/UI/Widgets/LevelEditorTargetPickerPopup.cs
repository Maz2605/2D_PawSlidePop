using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Model;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.UI.Panels;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.UI
{
    public sealed class LevelEditorTargetPickerPopup : EditorPopup
    {
        [Serializable]
        private sealed class PopupSectionBinding
        {
            public LevelEditorPaletteSectionType sectionType;
            public LevelEditorPopupSectionController controller;
            public string title;
            public string subTitle;
        }

        [SerializeField] private TMP_Text titleText;
        [SerializeField] private string addModeTitle = "Add Target";
        [SerializeField] private string editModeTitle = "Edit Target";
        [SerializeField] private Button closeButton;
        [SerializeField] private PopupSectionBinding[] sectionBindings;

        private readonly Dictionary<LevelEditorPaletteSectionType, int> _originalSiblingIndices = new Dictionary<LevelEditorPaletteSectionType, int>();
        private Action _onDismissRequested;
        private Action<int> _onAddTileSelected;
        private Action<int, int> _onEditTileSelected;
        private bool _isEditMode;
        private int _slotIndex;
        private int _selectedTileId;
        private LevelEditorPaletteSectionType? _activeSectionType;

        public void ShowForAdd(IReadOnlyList<LevelEditorPaletteEntryData> options, Action<int> onTileSelected, Action onDismissRequested)
        {
            _isEditMode = false;
            _slotIndex = -1;
            _selectedTileId = 0;
            _onAddTileSelected = onTileSelected;
            _onEditTileSelected = null;
            _onDismissRequested = onDismissRequested;
            BindCloseButton();
            Populate(options, addModeTitle);
        }

        public void ShowForEdit(int slotIndex, int selectedTileId, IReadOnlyList<LevelEditorPaletteEntryData> options, Action<int, int> onTileSelected, Action onDismissRequested)
        {
            _isEditMode = true;
            _slotIndex = slotIndex;
            _selectedTileId = selectedTileId;
            _onAddTileSelected = null;
            _onEditTileSelected = onTileSelected;
            _onDismissRequested = onDismissRequested;
            BindCloseButton();
            Populate(options, editModeTitle);
        }

        protected override void OnHidden()
        {
            _activeSectionType = null;
        }

        private void Populate(IReadOnlyList<LevelEditorPaletteEntryData> options, string popupTitle)
        {
            if (titleText != null)
            {
                titleText.text = popupTitle ?? string.Empty;
            }

            _originalSiblingIndices.Clear();
            if (sectionBindings == null || sectionBindings.Length == 0)
            {
                return;
            }

            LevelEditorPaletteSectionType? defaultSection = null;
            for (int i = 0; i < sectionBindings.Length; i++)
            {
                PopupSectionBinding binding = sectionBindings[i];
                if (binding == null || binding.controller == null)
                {
                    continue;
                }

                binding.controller.ConfigureTitle(binding.title, binding.subTitle);
                LevelEditorPaletteSectionType bindingSectionType = binding.sectionType;
                binding.controller.BindToggle(() => ActivateSection(bindingSectionType));

                List<LevelEditorPaletteEntryData> sectionEntries = FilterEntries(options, binding.sectionType);
                bool hasEntries = sectionEntries.Count > 0;
                binding.controller.SetVisible(hasEntries);
                if (!hasEntries)
                {
                    continue;
                }

                binding.controller.SetEntries(sectionEntries, _selectedTileId, HandleTileSelected);
                _originalSiblingIndices[binding.sectionType] = binding.controller.GetSiblingIndex();

                if (!defaultSection.HasValue || binding.sectionType == LevelEditorPaletteSectionType.ItemNormal)
                {
                    defaultSection = binding.sectionType;
                }
            }

            if (defaultSection.HasValue)
            {
                ActivateSection(defaultSection.Value);
            }
        }

        private void ActivateSection(LevelEditorPaletteSectionType sectionType)
        {
            _activeSectionType = sectionType;

            // First, collapse all other sections
            for (int i = 0; i < sectionBindings.Length; i++)
            {
                PopupSectionBinding binding = sectionBindings[i];
                if (binding?.controller == null)
                {
                    continue;
                }

                if (binding.sectionType != sectionType)
                {
                    binding.controller.PlayCollapse();
                }
            }

            // Assign lower sibling indices to non-active sections to clear the way
            int index = 0;
            for (int i = 0; i < sectionBindings.Length; i++)
            {
                PopupSectionBinding binding = sectionBindings[i];
                if (binding?.controller == null)
                {
                    continue;
                }

                if (binding.sectionType != sectionType)
                {
                    binding.controller.SetSiblingIndex(index++);
                }
            }

            // Put the active section on top (highest sibling index) and expand it
            for (int i = 0; i < sectionBindings.Length; i++)
            {
                PopupSectionBinding binding = sectionBindings[i];
                if (binding?.controller == null)
                {
                    continue;
                }

                if (binding.sectionType == sectionType)
                {
                    binding.controller.SetSiblingIndex(99);
                    binding.controller.PlayExpand();
                }
            }
        }

        private void HandleTileSelected(int tileId)
        {
            if (_isEditMode)
            {
                _onEditTileSelected?.Invoke(_slotIndex, tileId);
            }
            else
            {
                _onAddTileSelected?.Invoke(tileId);
            }
            _onDismissRequested?.Invoke();
        }

        private void BindCloseButton()
        {
            if (closeButton == null)
            {
                return;
            }

            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => _onDismissRequested?.Invoke());
        }

        private static List<LevelEditorPaletteEntryData> FilterEntries(IReadOnlyList<LevelEditorPaletteEntryData> options, LevelEditorPaletteSectionType sectionType)
        {
            List<LevelEditorPaletteEntryData> entries = new List<LevelEditorPaletteEntryData>();
            if (options == null)
            {
                return entries;
            }

            for (int i = 0; i < options.Count; i++)
            {
                if (options[i] == null || !options[i].SupportsTargetObjective || options[i].SectionType != sectionType)
                {
                    continue;
                }

                entries.Add(options[i]);
            }

            return entries;
        }
    }
}
