using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Model;
using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Services
{
    public static class LevelEditorCellArtPaletteBuilder
    {
        public static List<LevelEditorPaletteEntryData> Build(LevelEditorCellArtCatalogSO catalog)
        {
            List<LevelEditorPaletteEntryData> entries = new List<LevelEditorPaletteEntryData>();
            if (catalog == null)
            {
                return entries;
            }

            IReadOnlyList<LevelEditorCellArtDefinition> definitions = catalog.Entries;
            for (int i = 0; i < definitions.Count; i++)
            {
                LevelEditorCellArtDefinition definition = definitions[i];
                if (definition == null || definition.Id <= 0)
                {
                    continue;
                }

                entries.Add(new LevelEditorPaletteEntryData
                {
                    Id = definition.Id,
                    Label = definition.Label,
                    Description = string.IsNullOrWhiteSpace(definition.Label) ? $"Cell Art {definition.Id}" : definition.Label,
                    Icon = definition.Sprite,
                    Layer = BoardLayer.Underlay,
                    SupportsTargetObjective = false,
                    SectionType = LevelEditorPaletteSectionType.CellArt,
                    SortOrder = definition.SortOrder
                });
            }

            entries.Sort((left, right) =>
            {
                int orderCompare = left.SortOrder.CompareTo(right.SortOrder);
                return orderCompare != 0 ? orderCompare : left.Id.CompareTo(right.Id);
            });
            return entries;
        }
    }
}
