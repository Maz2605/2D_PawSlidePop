using System.Collections.Generic;
using UnityEngine.InputSystem;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Model;
using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.UI.Panels
{
    public sealed class LevelEditorPalettePresenter : MonoBehaviour
    {
        [SerializeField] private LevelEditorPaletteSectionController cellArtSection;
        [SerializeField] private LevelEditorPaletteSectionController itemNormalSection;
        [SerializeField] private LevelEditorPaletteSectionController itemSpecialSection;
        [SerializeField] private LevelEditorPaletteSectionController overlaySection;
        [SerializeField] private LevelEditorPaletteSectionController underlaySection;

        private LevelEditorUIController _service;
        private LevelEditorPaletteSectionType _activeSection = LevelEditorPaletteSectionType.ItemNormal;
        private bool _hasActiveSection = true;
        private LevelEditorPaletteSectionController[] _sections;
        private int[] _initialSiblingIndices;

        public void Bind(LevelEditorUIController service)
        {
            _service = service;
            EnsureCellArtSection();
            CacheSections();
            ConfigureSections();
            _service?.SetActiveSection(_activeSection);
            ShowActiveSectionInstant();
        }

        public void Refresh()
        {
            if (_service == null)
            {
                return;
            }

            cellArtSection?.SetEntries(
                _service.GetCellArtEntries(),
                _service.Selection != null ? _service.Selection.SelectedCellArtId : 0,
                id => _service.SetSelectedSectionLayer(LevelEditorPaletteSectionType.CellArt, id));

            itemNormalSection?.SetEntries(
                _service.GetPaletteEntries(LevelEditorPaletteSectionType.ItemNormal),
                _service.Selection != null ? _service.Selection.SelectedTileId : 0,
                id => _service.SetSelectedSectionLayer(LevelEditorPaletteSectionType.ItemNormal, id));

            itemSpecialSection?.SetEntries(
                _service.GetPaletteEntries(LevelEditorPaletteSectionType.ItemSpecial),
                _service.Selection != null ? _service.Selection.SelectedTileId : 0,
                id => _service.SetSelectedSectionLayer(LevelEditorPaletteSectionType.ItemSpecial, id));

            overlaySection?.SetEntries(
                _service.GetPaletteEntries(LevelEditorPaletteSectionType.Overlay),
                _service.Selection != null ? _service.Selection.SelectedOverlayId : 0,
                id => _service.SetSelectedSectionLayer(LevelEditorPaletteSectionType.Overlay, id));

            underlaySection?.SetEntries(
                _service.GetPaletteEntries(LevelEditorPaletteSectionType.Underlay),
                _service.Selection != null ? _service.Selection.SelectedUnderlayId : 0,
                id => _service.SetSelectedSectionLayer(LevelEditorPaletteSectionType.Underlay, id));
        }

        private void Update()
        {
            if (_service == null)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (IsTypingIntoInputField())
            {
                return;
            }

            if (keyboard.digit1Key.wasPressedThisFrame)
            {
                ToggleSection(LevelEditorPaletteSectionType.ItemNormal);
            }
            else if (keyboard.digit2Key.wasPressedThisFrame)
            {
                ToggleSection(LevelEditorPaletteSectionType.ItemSpecial);
            }
            else if (keyboard.digit3Key.wasPressedThisFrame)
            {
                ToggleSection(LevelEditorPaletteSectionType.Overlay);
            }
            else if (keyboard.digit4Key.wasPressedThisFrame)
            {
                ToggleSection(LevelEditorPaletteSectionType.Underlay);
            }
            else if (keyboard.digit5Key.wasPressedThisFrame)
            {
                ToggleSection(LevelEditorPaletteSectionType.CellArt);
            }
        }

        private void ConfigureSections()
        {
            cellArtSection?.ConfigureTitleAndShortcut("Cell Art", "5");
            cellArtSection?.BindToggle(() => ToggleSection(LevelEditorPaletteSectionType.CellArt));

            itemNormalSection?.ConfigureTitleAndShortcut("Normal Tiles", "1");
            itemNormalSection?.BindToggle(() => ToggleSection(LevelEditorPaletteSectionType.ItemNormal));

            itemSpecialSection?.ConfigureTitleAndShortcut("Special Tiles", "2");
            itemSpecialSection?.BindToggle(() => ToggleSection(LevelEditorPaletteSectionType.ItemSpecial));

            overlaySection?.ConfigureTitleAndShortcut("Overlay Tiles", "3");
            overlaySection?.BindToggle(() => ToggleSection(LevelEditorPaletteSectionType.Overlay));

            underlaySection?.ConfigureTitleAndShortcut("Underlay Tiles", "4");
            underlaySection?.BindToggle(() => ToggleSection(LevelEditorPaletteSectionType.Underlay));
        }

        private void ToggleSection(LevelEditorPaletteSectionType nextSection)
        {
            if (_hasActiveSection && _activeSection == nextSection)
            {
                _hasActiveSection = false;
                ApplySectionVisibility(false);
                return;
            }

            _activeSection = nextSection;
            _hasActiveSection = true;
            _service?.SetActiveSection(_activeSection);
            ApplySectionVisibility(false);
        }

        private void ShowActiveSectionInstant()
        {
            ApplySectionVisibility(true);
        }

        private void ApplySectionVisibility(bool instant)
        {
            ApplySectionSorting();
            UpdateSectionVisibility(cellArtSection, LevelEditorPaletteSectionType.CellArt, instant);
            UpdateSectionVisibility(itemNormalSection, LevelEditorPaletteSectionType.ItemNormal, instant);
            UpdateSectionVisibility(itemSpecialSection, LevelEditorPaletteSectionType.ItemSpecial, instant);
            UpdateSectionVisibility(overlaySection, LevelEditorPaletteSectionType.Overlay, instant);
            UpdateSectionVisibility(underlaySection, LevelEditorPaletteSectionType.Underlay, instant);
        }

        private void UpdateSectionVisibility(LevelEditorPaletteSectionController section, LevelEditorPaletteSectionType sectionType, bool instant)
        {
            if (section == null)
            {
                return;
            }

            bool shouldShow = _hasActiveSection && sectionType == _activeSection;
            if (instant)
            {
                if (shouldShow)
                {
                    section.ShowInstant();
                }
                else
                {
                    section.HideInstant();
                }
            }
            else
            {
                if (shouldShow)
                {
                    section.PlayShow();
                }
                else
                {
                    section.PlayHide();
                }
            }
        }

        private void CacheSections()
        {
            List<LevelEditorPaletteSectionController> sections = new List<LevelEditorPaletteSectionController>
            {
                cellArtSection,
                itemNormalSection,
                itemSpecialSection,
                overlaySection,
                underlaySection
            };

            _sections = sections.ToArray();

            _initialSiblingIndices = new int[_sections.Length];
            for (int i = 0; i < _sections.Length; i++)
            {
                _initialSiblingIndices[i] = _sections[i] != null ? _sections[i].GetSiblingIndex() : -1;
            }
        }

        private void ApplySectionSorting()
        {
            if (_sections == null || _initialSiblingIndices == null)
            {
                return;
            }

            if (!_hasActiveSection)
            {
                RestoreInitialSorting();
                return;
            }

            int nextIndex = GetLowestInitialSiblingIndex();
            LevelEditorPaletteSectionController activeSection = GetSection(_activeSection);
            if (activeSection != null)
            {
                activeSection.SetSiblingIndex(nextIndex);
                nextIndex++;
            }

            for (int i = 0; i < _sections.Length; i++)
            {
                LevelEditorPaletteSectionController section = _sections[i];
                if (section == null || section == activeSection)
                {
                    continue;
                }

                section.SetSiblingIndex(nextIndex);
                nextIndex++;
            }
        }

        private void RestoreInitialSorting()
        {
            for (int i = 0; i < _sections.Length; i++)
            {
                LevelEditorPaletteSectionController section = _sections[i];
                if (section == null || _initialSiblingIndices[i] < 0)
                {
                    continue;
                }

                section.SetSiblingIndex(_initialSiblingIndices[i]);
            }
        }

        private int GetLowestInitialSiblingIndex()
        {
            int lowestIndex = int.MaxValue;
            for (int i = 0; i < _initialSiblingIndices.Length; i++)
            {
                int siblingIndex = _initialSiblingIndices[i];
                if (siblingIndex >= 0 && siblingIndex < lowestIndex)
                {
                    lowestIndex = siblingIndex;
                }
            }

            return lowestIndex == int.MaxValue ? 0 : lowestIndex;
        }

        private LevelEditorPaletteSectionController GetSection(LevelEditorPaletteSectionType sectionType)
        {
            switch (sectionType)
            {
                case LevelEditorPaletteSectionType.ItemNormal:
                    return itemNormalSection;
                case LevelEditorPaletteSectionType.ItemSpecial:
                    return itemSpecialSection;
                case LevelEditorPaletteSectionType.Overlay:
                    return overlaySection;
                case LevelEditorPaletteSectionType.Underlay:
                    return underlaySection;
                case LevelEditorPaletteSectionType.CellArt:
                    return cellArtSection;
                default:
                    return null;
            }
        }

        private void EnsureCellArtSection()
        {
            if (cellArtSection != null || underlaySection == null)
            {
                return;
            }

            cellArtSection = Instantiate(underlaySection, underlaySection.transform.parent);
            cellArtSection.name = "CellArtSectionRuntime";
            cellArtSection.transform.SetAsLastSibling();
        }

        private static bool IsTypingIntoInputField()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return false;
            }

            GameObject selectedObject = eventSystem.currentSelectedGameObject;
            if (selectedObject == null)
            {
                return false;
            }

            return selectedObject.GetComponent<TMP_InputField>() != null
                || selectedObject.GetComponent<InputField>() != null;
        }
    }
}
