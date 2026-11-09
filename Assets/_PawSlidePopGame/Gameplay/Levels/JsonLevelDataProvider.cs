using Newtonsoft.Json;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Data.LevelProvider
{
    public class JsonLevelDataProvider : ILevelDataProvider
    {
        public bool TryGetLevelData(string levelId, out Match3LevelData levelData)
        {
            levelData = null;
            if (!TryGetLevelDocument(levelId, out LevelSaveData document))
            {
                return false;
            }

            levelData = document.levelData;
            return levelData != null;
        }

        public bool TryGetLevelDocument(string levelId, out LevelSaveData document)
        {
            document = null;
            string resourcePath = LevelPathUtility.GetResourcePath(levelId);
            if (string.IsNullOrEmpty(resourcePath))
            {
                Debug.LogError("[LevelEditor] Cannot load level. Level id is empty.");
                return false;
            }

#if UNITY_EDITOR
            string absolutePath = LevelPathUtility.GetAbsoluteAssetPath(levelId);
            if (!string.IsNullOrEmpty(absolutePath) && System.IO.File.Exists(absolutePath))
            {
                try
                {
                    string fileContent = System.IO.File.ReadAllText(absolutePath);
                    document = JsonConvert.DeserializeObject<LevelSaveData>(fileContent);
                }
                catch (System.Exception exception)
                {
                    Debug.LogWarning($"[LevelEditor] Failed to load level '{levelId}' directly from file system: {exception.Message}. Falling back to Resources.");
                }
            }
#endif

            if (document == null)
            {
                TextAsset textAsset = Resources.Load<TextAsset>(resourcePath);
                if (textAsset == null)
                {
                    Debug.LogError($"[LevelEditor] Level JSON not found at Resources path '{resourcePath}'.");
                    return false;
                }

                try
                {
                    document = JsonConvert.DeserializeObject<LevelSaveData>(textAsset.text);
                }
                catch (JsonException exception)
                {
                    Debug.LogError($"[LevelEditor] Failed to deserialize level '{levelId}'. {exception.Message}");
                    return false;
                }
            }

            if (document == null)
            {
                Debug.LogError($"[LevelEditor] Level document '{levelId}' is null after deserialization.");
                return false;
            }

            document.levelId = string.IsNullOrWhiteSpace(document.levelId)
                ? LevelPathUtility.SanitizeLevelId(levelId)
                : document.levelId;

            if (document.levelData == null)
            {
                document.levelData = new Match3LevelData();
            }

            if (string.IsNullOrWhiteSpace(document.levelData.levelID))
            {
                document.levelData.levelID = document.levelId;
            }

            return true;
        }
    }
}
