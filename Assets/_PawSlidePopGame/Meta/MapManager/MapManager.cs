using System.Collections.Generic;
using System.Globalization;
using _PawSlidePopGame._Scripts.UI.Screens.SubScreens;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Gameplay.Meta.MapManager
{
    public class MapManager
    {
        public sealed class MapSectionLayout
        {
            public MapSectionLayout(int sectionIndex, MapSectionView prefab, float anchoredY, float sectionHeight, int startEntryIndex, int entryCount)
            {
                SectionIndex = sectionIndex;
                Prefab = prefab;
                AnchoredY = anchoredY;
                SectionHeight = sectionHeight;
                StartEntryIndex = startEntryIndex;
                EntryCount = entryCount;
            }

            public int SectionIndex { get; }
            public MapSectionView Prefab { get; }
            public float AnchoredY { get; }
            public float SectionHeight { get; }
            public int StartEntryIndex { get; }
            public int EntryCount { get; }
            public float CenterY => AnchoredY + (SectionHeight * 0.5f);
        }

        public sealed class MapLayoutData
        {
            public static readonly MapLayoutData Empty = new MapLayoutData(
                new List<MapLevelEntry>(),
                new List<MapSectionLayout>(),
                0f);

            public MapLayoutData(
                IReadOnlyList<MapLevelEntry> entries,
                IReadOnlyList<MapSectionLayout> sections,
                float contentHeight)
            {
                Entries = entries ?? new List<MapLevelEntry>();
                Sections = sections ?? new List<MapSectionLayout>();
                ContentHeight = Mathf.Max(0f, contentHeight);
            }

            public IReadOnlyList<MapLevelEntry> Entries { get; }
            public IReadOnlyList<MapSectionLayout> Sections { get; }
            public float ContentHeight { get; }
        }

        public MapLayoutData BuildLayout(
            IReadOnlyList<string> levelIds,
            MapWorldConfigSO config,
            int highestUnlockedLevelNumber = int.MaxValue,
            int currentLevelNumber = 1,
            LevelProgressRepository progressRepository = null,
            bool skipSectionZero = false)
        {
            List<MapLevelEntry> entries = new List<MapLevelEntry>();
            List<MapSectionLayout> sections = new List<MapSectionLayout>();
            if (levelIds == null || levelIds.Count == 0)
            {
                return MapLayoutData.Empty;
            }

            int count = Mathf.Min(levelIds.Count, config != null ? config.MaxVisibleLevels : levelIds.Count);
            if (config == null || !config.HasSectionPrefabs)
            {
                return new MapLayoutData(entries, sections, 0f);
            }

            float currentSectionY = config.BottomPadding;
            int levelIndex = 0;
            int sectionIndex = 0;
            int safetyGuard = Mathf.Max(16, count * 4);

            while (levelIndex < count && safetyGuard-- > 0)
            {
                MapSectionView sectionPrefab = config.GetSectionPrefab(sectionIndex);
                if (sectionPrefab == null)
                {
                    Debug.LogWarning($"[MapManager] Missing section prefab for scenic section index {sectionIndex}.");
                    sectionIndex++;
                    continue;
                }

                int capacity = sectionPrefab.Capacity;
                if (capacity <= 0 && sectionIndex != 0)
                {
                    Debug.LogWarning($"[MapManager] Section prefab '{sectionPrefab.name}' has no node anchors.");
                    sectionIndex++;
                    continue;
                }

                int sectionEntryCount = (skipSectionZero && sectionIndex == 0) ? 0 : Mathf.Min(capacity, count - levelIndex);
                float sectionHeight = sectionPrefab.SectionHeight;
                int startEntryIndex = entries.Count;

                for (int slotIndex = 0; slotIndex < sectionEntryCount; slotIndex++)
                {
                    string levelId = levelIds[levelIndex];
                    int displayNumber = TryParseLevelNumber(levelId, out int parsedNumber) ? parsedNumber : levelIndex + 1;
                    LevelProgressEntryData progress = progressRepository?.GetProgress(levelId);
                    int bestStars = progress != null ? progress.bestStars : 0;
                    int bestScore = progress != null ? progress.bestScore : 0;
                    bool isHardLevel = config.IsHardLevel(displayNumber);
                    MapLevelState state = ResolveState(displayNumber, highestUnlockedLevelNumber, currentLevelNumber, bestStars, isHardLevel);
                    Vector2 localAnchorPosition = ResolveAnchorLocalPosition(sectionPrefab, slotIndex);

                    entries.Add(new MapLevelEntry(
                        levelId,
                        displayNumber,
                        sectionIndex,
                        slotIndex,
                        new Vector2(localAnchorPosition.x, currentSectionY + localAnchorPosition.y),
                        state,
                        bestStars,
                        bestScore,
                        isHardLevel));

                    levelIndex++;
                }

                sections.Add(new MapSectionLayout(
                    sectionIndex,
                    sectionPrefab,
                    currentSectionY,
                    sectionHeight,
                    startEntryIndex,
                    sectionEntryCount));

                currentSectionY += sectionHeight;
                sectionIndex++;

                if (levelIndex < count)
                {
                    currentSectionY += config.SectionSpacing;
                }
            }

            if (sections.Count > 0)
            {
                MapSectionView overflowSectionPrefab = config.GetSectionPrefab(sectionIndex);
                if (overflowSectionPrefab != null)
                {
                    float overflowAnchoredY = currentSectionY + config.SectionSpacing;
                    float overflowHeight = overflowSectionPrefab.SectionHeight;
                    sections.Add(new MapSectionLayout(
                        sectionIndex,
                        overflowSectionPrefab,
                        overflowAnchoredY,
                        overflowHeight,
                        entries.Count,
                        0));

                    currentSectionY = overflowAnchoredY + overflowHeight;
                }
            }

            float contentHeight = entries.Count > 0
                ? currentSectionY
                : 0f;

            return new MapLayoutData(entries, sections, contentHeight);
        }

        public float CalculateContentHeight(MapLayoutData layout, MapWorldConfigSO config)
        {
            return layout != null ? Mathf.Max(0f, layout.ContentHeight) : 0f;
        }

        public static bool TryParseLevelNumber(string levelId, out int levelNumber)
        {
            levelNumber = 0;
            if (string.IsNullOrWhiteSpace(levelId))
            {
                return false;
            }

            int lastSeparatorIndex = levelId.LastIndexOf('_');
            string numberText = lastSeparatorIndex >= 0 && lastSeparatorIndex < levelId.Length - 1
                ? levelId.Substring(lastSeparatorIndex + 1)
                : levelId;

            return int.TryParse(numberText, NumberStyles.Integer, CultureInfo.InvariantCulture, out levelNumber) &&
                   levelNumber > 0;
        }

        private static Vector2 ResolveAnchorLocalPosition(
            MapSectionView sectionPrefab,
            int slotIndex)
        {
            if (sectionPrefab != null && slotIndex >= 0 && slotIndex < sectionPrefab.Capacity)
            {
                return sectionPrefab.GetAnchorLocalPosition(slotIndex);
            }

            return Vector2.zero;
        }

        private static MapLevelState ResolveState(
            int displayNumber,
            int highestUnlockedLevelNumber,
            int currentLevelNumber,
            int bestStars,
            bool isHardLevel)
        {
            if (displayNumber > highestUnlockedLevelNumber)
            {
                return MapLevelState.Locked;
            }

            if (displayNumber == currentLevelNumber)
            {
                return MapLevelState.Current;
            }

            if (bestStars >= 4)
            {
                return MapLevelState.Perfect;
            }

            if (bestStars > 0)
            {
                return MapLevelState.Completed;
            }

            if (isHardLevel)
            {
                return MapLevelState.Hard;
            }

            return MapLevelState.Unplayed;
        }
    }
}
