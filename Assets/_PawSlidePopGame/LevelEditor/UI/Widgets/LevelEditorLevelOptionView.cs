using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.UI
{
    public sealed class LevelEditorLevelOptionView : MonoBehaviour
    {
        [SerializeField] private Button selectButton;
        [SerializeField] private TMP_Text levelNameText;

        public void Bind(string levelId, Action<string> onSelected)
        {
            if (levelNameText != null)
            {
                levelNameText.text = levelId ?? string.Empty;
            }

            if (selectButton != null)
            {
                selectButton.onClick.RemoveAllListeners();
                selectButton.onClick.AddListener(() => onSelected?.Invoke(levelId));
            }
        }
    }
}
