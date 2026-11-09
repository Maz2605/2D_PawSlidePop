using System.IO;
using System.Text;
using _PawSlidePopGame._Scripts.Data.LevelProvider;
using Newtonsoft.Json;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.Persistence
{
    public sealed class LevelJsonExportService
    {
        public bool TryExport(LevelSaveData document, string levelId, bool overwrite = true)
        {
            if (document == null || document.levelData == null)
            {
                Debug.LogError("[LevelEditor] Cannot export null document.");
                return false;
            }

            string sanitizedLevelId = LevelPathUtility.SanitizeLevelId(levelId);
            string absolutePath = LevelPathUtility.GetAbsoluteAssetPath(sanitizedLevelId);
            if (string.IsNullOrEmpty(sanitizedLevelId) || string.IsNullOrEmpty(absolutePath))
            {
                Debug.LogError("[LevelEditor] Invalid level id for export.");
                return false;
            }

            if (File.Exists(absolutePath) && !overwrite)
            {
                Debug.LogWarning($"[LevelEditor] Export skipped because file exists: {absolutePath}");
                return false;
            }

            string directory = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            document.levelId = sanitizedLevelId;
            document.levelData.levelID = sanitizedLevelId;
            string json = JsonConvert.SerializeObject(document, Formatting.Indented);
            File.WriteAllText(absolutePath, json, new UTF8Encoding(false));

#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
            return true;
        }
    }
}