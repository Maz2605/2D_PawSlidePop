using System;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Model
{
    [Serializable]
    public sealed class LevelEditorCellArtDefinition
    {
        [Min(1)]
        [SerializeField] private int id = 1;
        [SerializeField] private string label = "Cell Art";
        [SerializeField] private Sprite sprite;
        [SerializeField] private string category = "Default";
        [SerializeField] private int sortOrder;

        public int Id => id;
        public string Label => label;
        public Sprite Sprite => sprite;
        public string Category => category;
        public int SortOrder => sortOrder;
    }
}
