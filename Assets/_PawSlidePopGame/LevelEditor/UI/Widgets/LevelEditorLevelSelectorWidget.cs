using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using _PawSlidePopGame._Scripts.Data.LevelProvider;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.UI
{
    public sealed class LevelEditorLevelSelectorWidget : MonoBehaviour
    {
        [SerializeField] private string levelPrefix = "Level_";
        [SerializeField] private TMP_InputField levelSuffixInput;
        [SerializeField] private TMP_Dropdown levelDropdown;
        [SerializeField] private TMP_Text selectedLevelText;
        [SerializeField] private TMP_InputField searchInput;

        private readonly ILevelCatalogProvider _catalogProvider = new ResourcesLevelCatalogProvider();
        private readonly List<string> _displayedLevelIds = new List<string>();

        private string _currentLevelId = string.Empty;
        private bool _suppressCallbacks;

        public string CurrentLevelId => NormalizeLevelId(_currentLevelId, DefaultLevelId);
        public bool IsEditingLevelId => levelSuffixInput != null && levelSuffixInput.isFocused;

        public void Bind()
        {
            if (levelSuffixInput != null)
            {
                levelSuffixInput.onValueChanged.RemoveListener(HandleLevelIdChanged);
                levelSuffixInput.onValueChanged.AddListener(HandleLevelIdChanged);
                levelSuffixInput.onEndEdit.RemoveListener(HandleLevelIdEndEdit);
                levelSuffixInput.onEndEdit.AddListener(HandleLevelIdEndEdit);
                SetLevelInputPlaceholder();
            }

            if (searchInput != null)
            {
                searchInput.onValueChanged.RemoveListener(HandleSearchChanged);
                searchInput.onValueChanged.AddListener(HandleSearchChanged);
            }

            if (levelDropdown != null)
            {
                levelDropdown.onValueChanged.RemoveListener(HandleDropdownSelectionChanged);
                levelDropdown.onValueChanged.AddListener(HandleDropdownSelectionChanged);
            }

            ShowDefaultInputPrefix();
        }

        public void Refresh()
        {
            if (string.IsNullOrWhiteSpace(_currentLevelId) || IsPrefixOnly(_currentLevelId))
            {
                ShowDefaultInputPrefix();
                return;
            }

            SetLevelId(CurrentLevelId);
        }

        public void SetLevelId(string levelId)
        {
            _currentLevelId = NormalizeLevelId(levelId, DefaultLevelId);

            _suppressCallbacks = true;
            try
            {
                if (levelSuffixInput != null)
                {
                    levelSuffixInput.SetTextWithoutNotify(_currentLevelId);
                }

                SyncSelectedText();
                RebuildDropdownOptions(string.Empty);
                SelectDropdownValue(_currentLevelId);
            }
            finally
            {
                _suppressCallbacks = false;
            }
        }

        private void HandleLevelIdChanged(string levelId)
        {
            if (_suppressCallbacks)
            {
                return;
            }

            _currentLevelId = IsPrefixOnly(levelId) ? LevelPrefix : NormalizeLevelId(levelId, DefaultLevelId);
            SyncSelectedText();
            RebuildDropdownOptions(GetActiveFilter());
        }

        private void HandleLevelIdEndEdit(string levelId)
        {
            if (_suppressCallbacks)
            {
                return;
            }

            if (IsPrefixOnly(levelId))
            {
                ShowDefaultInputPrefix();
                return;
            }

            SetLevelId(levelId);
        }

        private void HandleSearchChanged(string _)
        {
            if (_suppressCallbacks)
            {
                return;
            }

            RebuildDropdownOptions(GetActiveFilter());
        }

        private void HandleDropdownSelectionChanged(int selectedIndex)
        {
            if (_suppressCallbacks || selectedIndex < 0 || selectedIndex >= _displayedLevelIds.Count)
            {
                return;
            }

            SetLevelId(_displayedLevelIds[selectedIndex]);
        }

        private void RebuildDropdownOptions(string filter)
        {
            if (levelDropdown == null)
            {
                return;
            }

            _displayedLevelIds.Clear();
            _displayedLevelIds.AddRange(_catalogProvider.GetLevelIds(filter));

            if (_displayedLevelIds.Count == 0)
            {
                _displayedLevelIds.Add(CurrentLevelId);
            }

            levelDropdown.ClearOptions();
            levelDropdown.AddOptions(_displayedLevelIds);
            SyncSelectedText();
        }

        private void SelectDropdownValue(string levelId)
        {
            if (levelDropdown == null)
            {
                return;
            }

            int optionIndex = _displayedLevelIds.FindIndex(option => string.Equals(option, levelId, StringComparison.OrdinalIgnoreCase));
            if (optionIndex < 0)
            {
                optionIndex = 0;
            }

            levelDropdown.SetValueWithoutNotify(optionIndex);
            levelDropdown.RefreshShownValue();
            SyncSelectedText();
        }

        private void SyncSelectedText()
        {
            if (selectedLevelText != null)
            {
                selectedLevelText.text = CurrentLevelId;
            }
        }

        private string GetActiveFilter()
        {
            if (searchInput != null && !string.IsNullOrWhiteSpace(searchInput.text))
            {
                return searchInput.text.Trim();
            }

            if (levelSuffixInput != null && !string.IsNullOrWhiteSpace(levelSuffixInput.text))
            {
                if (IsPrefixOnly(levelSuffixInput.text))
                {
                    return string.Empty;
                }

                return NormalizeLevelId(levelSuffixInput.text, string.Empty);
            }

            return string.Empty;
        }

        private string NormalizeLevelId(string rawLevelId, string fallback)
        {
            string normalizedPrefix = LevelPrefix;
            string trimmedLevelId = string.IsNullOrWhiteSpace(rawLevelId) ? string.Empty : rawLevelId.Trim();
            string fallbackLevelId = string.IsNullOrWhiteSpace(fallback) ? string.Empty : fallback.Trim();
            if (string.IsNullOrWhiteSpace(trimmedLevelId))
            {
                return LevelPathUtility.SanitizeLevelId(fallbackLevelId);
            }

            string fullLevelId = trimmedLevelId.StartsWith(normalizedPrefix, StringComparison.OrdinalIgnoreCase)
                ? trimmedLevelId
                : $"{normalizedPrefix}{trimmedLevelId}";
            string sanitizedLevelId = LevelPathUtility.SanitizeLevelId(fullLevelId);
            return string.Equals(sanitizedLevelId, normalizedPrefix, StringComparison.OrdinalIgnoreCase)
                ? LevelPathUtility.SanitizeLevelId(fallbackLevelId)
                : sanitizedLevelId;
        }

        private void ShowDefaultInputPrefix()
        {
            _currentLevelId = LevelPrefix;

            _suppressCallbacks = true;
            try
            {
                if (levelSuffixInput != null)
                {
                    levelSuffixInput.SetTextWithoutNotify(LevelPrefix);
                }

                SyncSelectedText();
                RebuildDropdownOptions(string.Empty);
                SelectDropdownValue(DefaultLevelId);
            }
            finally
            {
                _suppressCallbacks = false;
            }
        }

        private bool IsPrefixOnly(string levelId)
        {
            return string.Equals(
                string.IsNullOrWhiteSpace(levelId) ? string.Empty : levelId.Trim(),
                LevelPrefix,
                StringComparison.OrdinalIgnoreCase);
        }

        private void SetLevelInputPlaceholder()
        {
            if (levelSuffixInput?.placeholder is TMP_Text placeholderText)
            {
                placeholderText.text = LevelPrefix;
            }
        }

        private string LevelPrefix => string.IsNullOrWhiteSpace(levelPrefix) ? "Level_" : levelPrefix.Trim();
        private string DefaultLevelId => $"{LevelPrefix}001";
    }
}
