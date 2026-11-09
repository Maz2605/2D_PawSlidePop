#if UNITY_EDITOR
using MazFramework.Audio;
using _PawSlidePopGame._Scripts.Data.Config;
using UnityEditor;
using UnityEngine;

namespace _PawSlidePopGame.Editor
{
    public static class SetupAudioAndUI
    {
        [MenuItem("Tools/PawSlidePop/Setup Audio And UI")]
        public static void RunSetup()
        {
            const string configPath = "Assets/_PawSlidePopGame/Content/Shared/Resources/Configs/AudioConfig.asset";
            const string prefabPath = "Assets/_PawSlidePopGame/Content/Prefabs/Services/PawAudioManager.prefab";
            var config = AssetDatabase.LoadAssetAtPath<PawSlidePopAudioConfig>(configPath);
            if (config == null)
            {
                Debug.LogError("[SetupAudioAndUI] Missing game AudioConfig: " + configPath);
                return;
            }
            GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var audio = contents.GetComponent<AudioManager>();
                if (audio == null)
                {
                    Debug.LogError("[SetupAudioAndUI] PawAudioManager prefab has no base AudioManager.");
                    return;
                }
                var serialized = new SerializedObject(audio);
                serialized.FindProperty("audioClipConfig").objectReferenceValue = config;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                Debug.Log("[SetupAudioAndUI] Config assigned on PawAudioManager prefab. No scenes or UI prefabs changed.");
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
        }
    }
}
#endif
