using System.IO;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Data.LevelProvider
{
    public static class LevelPathUtility
    {
        public const string ResourcesLevelsFolder = "Levels";
        public const string AssetLevelsFolder = "Assets/_PawSlidePopGame/Content/Shared/Resources/Levels";

        public static string SanitizeLevelId(string levelId)
        {
            if (string.IsNullOrWhiteSpace(levelId))
            {
                return string.Empty;
            }

            string sanitized = levelId.Trim();
            foreach (char invalidChar in Path.GetInvalidFileNameChars())
            {
                sanitized = sanitized.Replace(invalidChar.ToString(), string.Empty);
            }

            sanitized = sanitized.Replace("/", string.Empty).Replace("\\", string.Empty);
            return sanitized;
        }

        public static string GetResourcePath(string levelId)
        {
            string sanitized = SanitizeLevelId(levelId);
            return string.IsNullOrEmpty(sanitized) ? string.Empty : $"{ResourcesLevelsFolder}/{sanitized}";
        }

        public static string GetAssetPath(string levelId)
        {
            string sanitized = SanitizeLevelId(levelId);
            return string.IsNullOrEmpty(sanitized) ? string.Empty : $"{AssetLevelsFolder}/{sanitized}.json";
        }

        public static string GetAbsoluteAssetPath(string levelId)
        {
            string assetPath = GetAssetPath(levelId);
            if (string.IsNullOrEmpty(assetPath))
            {
                return string.Empty;
            }

            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot))
            {
                return string.Empty;
            }

            return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
