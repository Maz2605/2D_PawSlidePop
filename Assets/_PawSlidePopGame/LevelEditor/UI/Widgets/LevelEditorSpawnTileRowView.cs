using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Model;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.UI
{
    public sealed class LevelEditorSpawnTileRowView : MonoBehaviour
    {
        [SerializeField] private Image tileIcon;
        [SerializeField] private TMP_Text tileLabel;
        [SerializeField] private Toggle enabledToggle;
        [SerializeField] private TMP_InputField weightInput;

        private int _tileId;
        private Action<int, bool, int> _onChanged;

        public void Bind(LevelEditorPaletteEntryData entry, bool isEnabled, int weight, Action<int, bool, int> onChanged)
        {
            _tileId = entry.Id;
            _onChanged = onChanged;

            if (tileIcon != null)
            {
                tileIcon.sprite = entry.Icon;
                tileIcon.gameObject.SetActive(entry.Icon != null);
            }

            if (tileLabel != null)
            {
                tileLabel.text = entry.Label;
            }

            if (enabledToggle != null)
            {
                enabledToggle.onValueChanged.RemoveListener(HandleToggleChanged);
                enabledToggle.isOn = isEnabled;
                enabledToggle.onValueChanged.AddListener(HandleToggleChanged);
            }

            if (weightInput != null)
            {
                weightInput.onEndEdit.RemoveListener(HandleWeightInputChanged);
                weightInput.text = weight.ToString();
                weightInput.onEndEdit.AddListener(HandleWeightInputChanged);
                weightInput.interactable = isEnabled;
            }
        }

        private void HandleToggleChanged(bool isOn)
        {
            if (weightInput != null)
            {
                weightInput.interactable = isOn;
            }
            TriggerChanged();
        }

        private void HandleWeightInputChanged(string text)
        {
            TriggerChanged();
        }

        private void TriggerChanged()
        {
            bool isEnabled = enabledToggle != null ? enabledToggle.isOn : true;
            int weight = 100;
            if (weightInput != null && int.TryParse(weightInput.text, out int parsedWeight))
            {
                weight = Mathf.Max(0, parsedWeight);
            }

            _onChanged?.Invoke(_tileId, isEnabled, weight);
        }
    }
}
