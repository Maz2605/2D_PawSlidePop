using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Data.LevelProvider
{
    public sealed class ResourcesLevelCatalogProvider : ILevelCatalogProvider
    {
        public IReadOnlyList<string> GetLevelIds(string filter = null)
        {
            TextAsset[] assets = Resources.LoadAll<TextAsset>(LevelPathUtility.ResourcesLevelsFolder);
            IEnumerable<string> levelIds = assets
                .Where(asset => asset != null)
                .Select(asset => LevelPathUtility.SanitizeLevelId(asset.name))
                .Where(levelId => !string.IsNullOrWhiteSpace(levelId))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(levelId => levelId, StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(filter))
            {
                string normalizedFilter = filter.Trim();
                levelIds = levelIds.Where(levelId => levelId.IndexOf(normalizedFilter, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            return levelIds.ToList();
        }
    }
}
