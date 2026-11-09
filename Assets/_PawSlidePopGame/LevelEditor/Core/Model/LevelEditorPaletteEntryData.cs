using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Model
{
    public sealed class LevelEditorPaletteEntryData
    {
        public int Id { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
        public Sprite Icon { get; set; }
        public BoardLayer Layer { get; set; }
        public bool SupportsTargetObjective { get; set; }
        public LevelEditorPaletteSectionType SectionType { get; set; }
        public int SortOrder { get; set; }
    }
}
