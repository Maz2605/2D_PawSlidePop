using System.Collections.Generic;
using _PawSlidePopGame._Scripts.UI.Screens.SubScreens;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Gameplay.Meta.MapManager
{
    [CreateAssetMenu(fileName = "MapWorldConfig", menuName = "_PawSlidePopGame/Map/World Config")]
    public sealed class MapWorldConfigSO : ScriptableObject
    {
        [Header("Generation")]
        [SerializeField, Min(1)] private int maxVisibleLevels = 100;
        [SerializeField] private bool markHardLevels;
        [SerializeField, Min(1)] private int hardLevelInterval = 10;

        [Header("Layout")]
        [SerializeField, Min(0f)] private float sectionSpacing = 60f;
        [SerializeField, Min(0f)] private float bottomPadding = 160f;
        [SerializeField, Min(0f)] private float topPadding = 220f;

        [Header("Assets")]
        [SerializeField] private List<MapSectionView> sectionPrefabs = new List<MapSectionView>();

        public int MaxVisibleLevels => Mathf.Max(1, maxVisibleLevels);
        public float SectionSpacing => Mathf.Max(0f, sectionSpacing);
        public float BottomPadding => Mathf.Max(0f, bottomPadding);
        public float TopPadding => Mathf.Max(0f, topPadding);
        public IReadOnlyList<MapSectionView> SectionPrefabs => sectionPrefabs;
        public bool HasSectionPrefabs => sectionPrefabs != null && sectionPrefabs.Exists(section => section != null);

        public bool IsHardLevel(int displayLevelNumber)
        {
            return markHardLevels &&
                   hardLevelInterval > 0 &&
                   displayLevelNumber > 0 &&
                   displayLevelNumber % hardLevelInterval == 0;
        }

        public MapSectionView GetSectionPrefab(int sectionIndex)
        {
            if (sectionPrefabs == null || sectionPrefabs.Count == 0)
            {
                return null;
            }

            int count = sectionPrefabs.Count;
            for (int offset = 0; offset < count; offset++)
            {
                int resolvedIndex = Mathf.Abs(sectionIndex + offset) % count;
                MapSectionView sectionPrefab = sectionPrefabs[resolvedIndex];
                if (sectionPrefab != null)
                {
                    return sectionPrefab;
                }
            }

            return null;
        }
    }
}
