using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Model;
using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Services
{
    public static class LevelEditorPaletteBuilder
    {
        public static List<LevelEditorPaletteEntryData> Build(Match3TileDatabaseSO database, BoardLayer layer)
        {
            List<LevelEditorPaletteEntryData> entries = new List<LevelEditorPaletteEntryData>();
            if (database == null)
            {
                return entries;
            }

            IReadOnlyList<BoardContentDefinitionSO> definitions = database.Tiles;
            for (int i = 0; i < definitions.Count; i++)
            {
                BoardContentDefinitionSO definition = definitions[i];
                if (definition == null || definition.ContentLayer != layer)
                {
                    continue;
                }

                entries.Add(new LevelEditorPaletteEntryData
                {
                    Id = definition.TileId,
                    Label = $"{definition.TileId} - {definition.name}",
                    Description = definition.name,
                    Icon = definition.Icon,
                    Layer = definition.ContentLayer,
                    SupportsTargetObjective = definition.SupportsTargetObjective,
                    SectionType = ResolveSectionType(definition)
                });
            }

            entries.Sort((left, right) => left.Id.CompareTo(right.Id));
            return entries;
        }

        public static List<LevelEditorPaletteEntryData> BuildSection(Match3TileDatabaseSO database, LevelEditorPaletteSectionType sectionType)
        {
            List<LevelEditorPaletteEntryData> entries = new List<LevelEditorPaletteEntryData>();
            if (database == null)
            {
                return entries;
            }

            IReadOnlyList<BoardContentDefinitionSO> definitions = database.Tiles;
            for (int i = 0; i < definitions.Count; i++)
            {
                BoardContentDefinitionSO definition = definitions[i];
                if (!MatchesSection(definition, sectionType))
                {
                    continue;
                }

                entries.Add(new LevelEditorPaletteEntryData
                {
                    Id = definition.TileId,
                    Label = $"{definition.TileId} - {definition.name}",
                    Description = definition.name,
                    Icon = definition.Icon,
                    Layer = definition.ContentLayer,
                    SupportsTargetObjective = definition.SupportsTargetObjective,
                    SectionType = ResolveSectionType(definition)
                });
            }

            entries.Sort((left, right) => left.Id.CompareTo(right.Id));
            return entries;
        }

        public static List<LevelEditorPaletteEntryData> BuildTargetEntries(Match3TileDatabaseSO database)
        {
            return BuildTargetEntries(database, null);
        }

        public static List<LevelEditorPaletteEntryData> BuildTargetEntries(Match3TileDatabaseSO database, LevelEditorPaletteSectionType? sectionType)
        {
            List<LevelEditorPaletteEntryData> entries = new List<LevelEditorPaletteEntryData>();
            if (database == null)
            {
                return entries;
            }

            IReadOnlyList<BoardContentDefinitionSO> definitions = database.Tiles;
            for (int i = 0; i < definitions.Count; i++)
            {
                BoardContentDefinitionSO definition = definitions[i];
                if (definition == null || !definition.SupportsTargetObjective)
                {
                    continue;
                }

                LevelEditorPaletteSectionType resolvedSectionType = ResolveSectionType(definition);
                if (sectionType.HasValue && resolvedSectionType != sectionType.Value)
                {
                    continue;
                }

                entries.Add(new LevelEditorPaletteEntryData
                {
                    Id = definition.TileId,
                    Label = $"{definition.TileId} - {definition.name}",
                    Description = definition.name,
                    Icon = definition.Icon,
                    Layer = definition.ContentLayer,
                    SupportsTargetObjective = true,
                    SectionType = resolvedSectionType
                });
            }

            entries.Sort((left, right) => left.Id.CompareTo(right.Id));
            return entries;
        }

        private static bool MatchesSection(BoardContentDefinitionSO definition, LevelEditorPaletteSectionType sectionType)
        {
            if (definition == null)
            {
                return false;
            }

            switch (sectionType)
            {
                case LevelEditorPaletteSectionType.ItemNormal:
                    return definition is NormalTileDefinitionSO;
                case LevelEditorPaletteSectionType.ItemSpecial:
                    return definition is TileDefinitionSO tileDefinition && tileDefinition.TileKind != TileKind.Normal;
                case LevelEditorPaletteSectionType.Overlay:
                    return definition.ContentLayer == BoardLayer.Overlay;
                case LevelEditorPaletteSectionType.Underlay:
                    return definition.ContentLayer == BoardLayer.Underlay;
                default:
                    return false;
            }
        }

        private static LevelEditorPaletteSectionType ResolveSectionType(BoardContentDefinitionSO definition)
        {
            if (definition is NormalTileDefinitionSO)
            {
                return LevelEditorPaletteSectionType.ItemNormal;
            }

            if (definition is TileDefinitionSO tileDefinition && tileDefinition.TileKind != TileKind.Normal)
            {
                return LevelEditorPaletteSectionType.ItemSpecial;
            }

            if (definition != null && definition.ContentLayer == BoardLayer.Overlay)
            {
                return LevelEditorPaletteSectionType.Overlay;
            }

            return LevelEditorPaletteSectionType.Underlay;
        }
    }
}
