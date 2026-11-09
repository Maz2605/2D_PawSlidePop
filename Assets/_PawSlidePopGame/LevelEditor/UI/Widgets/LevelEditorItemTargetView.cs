using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Model;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.UI
{
    public sealed class LevelEditorItemTargetView : MonoBehaviour
    {
        [SerializeField] private Button selectButton;
        [SerializeField] private Button removeButton;
        [SerializeField] private TMP_InputField countInput;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image shadowImage;

        private int _slotIndex;
        private Action<int> _onSelectRequested;
        private Action<int, int> _onCountChanged;
        private Action<int> _onRemoveRequested;

        public void Bind(
            LevelEditorPaletteEntryData entry,
            int requiredCount,
            int slotIndex,
            Action<int> onSelectRequested,
            Action<int, int> onCountChanged,
            Action<int> onRemoveRequested)
        {
            _slotIndex = slotIndex;
            _onSelectRequested = onSelectRequested;
            _onCountChanged = onCountChanged;
            _onRemoveRequested = onRemoveRequested;

            gameObject.SetActive(true);

            if (iconImage != null)
            {
                iconImage.sprite = entry.Icon;
                iconImage.enabled = entry.Icon != null;
            }

            if (shadowImage != null)
            {
                shadowImage.sprite = entry.Icon;
                shadowImage.enabled = entry.Icon != null;
            }

            if (countInput != null)
            {
                countInput.onEndEdit.RemoveListener(HandleCountEndEdit);
                countInput.SetTextWithoutNotify(Mathf.Max(1, requiredCount).ToString());
                countInput.onEndEdit.AddListener(HandleCountEndEdit);
            }

            if (selectButton != null)
            {
                selectButton.onClick.RemoveAllListeners();
                selectButton.onClick.AddListener(HandleSelectClicked);
            }

            if (removeButton != null)
            {
                removeButton.onClick.RemoveAllListeners();
                removeButton.onClick.AddListener(HandleRemoveClicked);
            }
        }

        public void ShowEmpty()
        {
            if (removeButton != null)
            {
                removeButton.onClick.RemoveAllListeners();
            }

            if (countInput != null)
            {
                countInput.onEndEdit.RemoveListener(HandleCountEndEdit);
                countInput.SetTextWithoutNotify(string.Empty);
            }

            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }

            if (shadowImage != null)
            {
                shadowImage.sprite = null;
                shadowImage.enabled = false;
            }

            gameObject.SetActive(false);
        }

        private void HandleSelectClicked()
        {
            _onSelectRequested?.Invoke(_slotIndex);
        }

        private void HandleCountEndEdit(string rawValue)
        {
            int requiredCount = 1;
            if (int.TryParse(rawValue, out int parsedCount))
            {
                requiredCount = Mathf.Max(1, parsedCount);
            }

            if (countInput != null)
            {
                countInput.SetTextWithoutNotify(requiredCount.ToString());
            }

            _onCountChanged?.Invoke(_slotIndex, requiredCount);
        }

        private void HandleRemoveClicked()
        {
            _onRemoveRequested?.Invoke(_slotIndex);
        }
    }
}
