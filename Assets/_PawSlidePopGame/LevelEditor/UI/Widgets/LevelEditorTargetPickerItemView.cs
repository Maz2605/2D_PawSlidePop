using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Model;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.UI
{
    public sealed class LevelEditorTargetPickerItemView : MonoBehaviour
    {
        [SerializeField] private Button selectButton;
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image shadowImage;

        public void Bind(LevelEditorPaletteEntryData entry, Action<int> onSelected)
        {
            if (labelText != null)
            {
                labelText.text = entry.Description ?? string.Empty;
            }

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

            if (selectButton != null)
            {
                selectButton.onClick.RemoveAllListeners();
                selectButton.onClick.AddListener(() => onSelected?.Invoke(entry.Id));
            }
        }
    }
}
