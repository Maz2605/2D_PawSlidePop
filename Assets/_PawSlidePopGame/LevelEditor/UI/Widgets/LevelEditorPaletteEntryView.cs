using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.UI
{
    public sealed class LevelEditorPaletteEntryView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private Image iconImage;
        [SerializeField] private GameObject selectedMarker;

        private int _contentId;
        private Action<int> _onClicked;

        public void Bind(int contentId, string label, Sprite icon, bool selected, Action<int> onClicked)
        {
            _contentId = contentId;
            _onClicked = onClicked;

            if (labelText != null)
            {
                labelText.text = label ?? string.Empty;
            }

            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }

            if (selectedMarker != null)
            {
                selectedMarker.SetActive(selected);
            }

            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
                button.onClick.AddListener(HandleClick);
            }
        }

        private void HandleClick()
        {
            _onClicked?.Invoke(_contentId);
        }
    }
}
