using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.UI
{
    public sealed class LevelEditorGoalRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown tileDropdown;
        [SerializeField] private TMP_InputField countInput;
        [SerializeField] private Button removeButton;

        private readonly List<int> _optionIds = new List<int>();
        private Action<int, int> _onChanged;

        public void Bind(IReadOnlyList<(int id, string label)> options, int selectedTileId, int requiredCount, Action<int, int> onChanged, Action onRemove)
        {
            _onChanged = onChanged;
            _optionIds.Clear();

            if (tileDropdown != null)
            {
                tileDropdown.onValueChanged.RemoveListener(HandleDropdownChanged);
                tileDropdown.ClearOptions();

                List<TMP_Dropdown.OptionData> dropdownOptions = new List<TMP_Dropdown.OptionData>();
                int selectedIndex = 0;
                for (int i = 0; i < options.Count; i++)
                {
                    _optionIds.Add(options[i].id);
                    dropdownOptions.Add(new TMP_Dropdown.OptionData(options[i].label));
                    if (options[i].id == selectedTileId)
                    {
                        selectedIndex = i;
                    }
                }

                tileDropdown.AddOptions(dropdownOptions);
                tileDropdown.SetValueWithoutNotify(selectedIndex);
                tileDropdown.onValueChanged.AddListener(HandleDropdownChanged);
            }

            if (countInput != null)
            {
                countInput.onEndEdit.RemoveListener(HandleCountChanged);
                countInput.SetTextWithoutNotify(Mathf.Max(1, requiredCount).ToString());
                countInput.onEndEdit.AddListener(HandleCountChanged);
            }

            if (removeButton != null)
            {
                removeButton.onClick.RemoveAllListeners();
                removeButton.onClick.AddListener(() => onRemove?.Invoke());
            }
        }

        private void HandleDropdownChanged(int index)
        {
            TriggerChanged(index);
        }

        private void HandleCountChanged(string _)
        {
            TriggerChanged(tileDropdown != null ? tileDropdown.value : 0);
        }

        private void TriggerChanged(int dropdownIndex)
        {
            int tileId = dropdownIndex >= 0 && dropdownIndex < _optionIds.Count ? _optionIds[dropdownIndex] : 0;
            int requiredCount = 1;
            if (countInput != null && int.TryParse(countInput.text, out int parsedCount))
            {
                requiredCount = Mathf.Max(1, parsedCount);
            }

            _onChanged?.Invoke(tileId, requiredCount);
        }
    }
}
