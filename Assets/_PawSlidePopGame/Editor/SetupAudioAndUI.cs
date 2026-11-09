#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using _PawSlidePopGame._Scripts.Core.Audio;
using _PawSlidePopGame._Scripts.Data.Audio;
using _PawSlidePopGame._Scripts.Data.Config;

namespace _PawSlidePopGame.Editor
{
    [InitializeOnLoad]
    public static class SetupAudioAndUI
    {
        static SetupAudioAndUI()
        {
            // Run automatically on compilation/editor startup
            EditorApplication.delayCall += () =>
            {
                if (!SessionState.GetBool("SetupAudioAndUI_AutoRan", false))
                {
                    SessionState.SetBool("SetupAudioAndUI_AutoRan", true);
                    RunSetup();
                }
            };
        }

        [MenuItem("Tools/PawSlidePop/Setup Audio And UI")]
        public static void RunSetup()
        {
            Debug.Log("[SetupAudioAndUI] Starting setup...");

            // 1. Load AudioConfig
            string configPath = "Assets/Resources/Configs/AudioConfig.asset";
            var audioConfig = AssetDatabase.LoadAssetAtPath<PawSlidePopAudioConfig>(configPath);
            if (audioConfig == null)
            {
                Debug.LogError($"[SetupAudioAndUI] AudioConfig asset not found at '{configPath}'!");
                return;
            }
            Debug.Log($"[SetupAudioAndUI] Loaded AudioConfig: {audioConfig.name}");

            // 2. Configure AppBootstrap prefab
            string bootstrapPath = "Assets/Resources/Core/AppBootstrap.prefab";
            var bootstrapGo = PrefabUtility.LoadPrefabContents(bootstrapPath);
            if (bootstrapGo == null)
            {
                Debug.LogError($"[SetupAudioAndUI] AppBootstrap prefab not found at '{bootstrapPath}'!");
                return;
            }

            var audioController = bootstrapGo.GetComponent<AudioController>();
            if (audioController == null)
            {
                audioController = bootstrapGo.AddComponent<AudioController>();
                Debug.Log("[SetupAudioAndUI] Added AudioController to AppBootstrap prefab.");
            }

            // Set the audioConfig field via SerializedObject
            var serializedController = new SerializedObject(audioController);
            var configProp = serializedController.FindProperty("audioConfig");
            if (configProp != null)
            {
                configProp.objectReferenceValue = audioConfig;
                serializedController.ApplyModifiedProperties();
                Debug.Log("[SetupAudioAndUI] Set audioConfig reference on AudioController.");
            }
            else
            {
                Debug.LogError("[SetupAudioAndUI] Failed to find 'audioConfig' field on AudioController!");
            }

            // Set dontDestroyOnLoad to true
            var dontDestroyProp = serializedController.FindProperty("dontDestroyOnLoad");
            if (dontDestroyProp != null)
            {
                dontDestroyProp.boolValue = true;
                serializedController.ApplyModifiedProperties();
            }

            PrefabUtility.SaveAsPrefabAsset(bootstrapGo, bootstrapPath);
            PrefabUtility.UnloadPrefabContents(bootstrapGo);
            Debug.Log($"[SetupAudioAndUI] Saved AppBootstrap prefab: {bootstrapPath}");

            // 3. Remove AudioController from GameplayScene if it exists
            string gameplayScenePath = "Assets/_PawSlidePopGame/Scenes/GameplayScene.unity";
            if (File.Exists(gameplayScenePath))
            {
                var scene = EditorSceneManager.OpenScene(gameplayScenePath, OpenSceneMode.Single);
                var controllerInScene = Object.FindAnyObjectByType<AudioController>();
                if (controllerInScene != null)
                {
                    Debug.Log($"[SetupAudioAndUI] Found AudioController in GameplayScene on GameObject '{controllerInScene.gameObject.name}'.");
                    var components = controllerInScene.gameObject.GetComponents<Component>();
                    if (components.Length <= 2) // Transform and AudioController
                    {
                        Debug.Log("[SetupAudioAndUI] Destroying standalone AudioController GameObject in GameplayScene.");
                        Object.DestroyImmediate(controllerInScene.gameObject);
                    }
                    else
                    {
                        Debug.Log("[SetupAudioAndUI] Removing AudioController component from GameObject in GameplayScene.");
                        Object.DestroyImmediate(controllerInScene);
                    }
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    Debug.Log("[SetupAudioAndUI] Saved GameplayScene.");
                }
                else
                {
                    Debug.Log("[SetupAudioAndUI] No AudioController found in GameplayScene.");
                }
            }
            else
            {
                Debug.LogWarning($"[SetupAudioAndUI] GameplayScene not found at '{gameplayScenePath}'");
            }

            // 4. Configure all UI prefabs
            string[] searchFolders = new[] { "Assets/Resources/UI" };
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", searchFolders);
            Debug.Log($"[SetupAudioAndUI] Found {prefabGuids.Length} prefabs under Assets/Resources/UI.");

            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = PrefabUtility.LoadPrefabContents(path);
                if (prefab == null) continue;

                var buttons = prefab.GetComponentsInChildren<Button>(true);
                bool changed = false;

                foreach (var button in buttons)
                {
                    var btnSound = button.GetComponent<UIButtonSound>();
                    if (btnSound == null)
                    {
                        btnSound = button.gameObject.AddComponent<UIButtonSound>();
                        changed = true;
                        Debug.Log($"[SetupAudioAndUI] Added UIButtonSound to button '{button.name}' in prefab '{prefab.name}'");
                    }

                    // Map name to sound type
                    string name = button.gameObject.name.ToLower();
                    UISoundType targetSound = UISoundType.ClickNormal;

                    if (name.Contains("close") || name.Contains("back") || name.Contains("exit") || name.Contains("btn_x") || name.Contains("btnx") || name == "x")
                    {
                        targetSound = UISoundType.ClickBack;
                    }
                    else if (name.Contains("cancel") || name.Contains("no") || name.Contains("btncancel") || name.Contains("btnno"))
                    {
                        targetSound = UISoundType.ClickCancel;
                    }
                    else if (name.Contains("confirm") || name.Contains("ok") || name.Contains("yes") || name.Contains("accept") || name.Contains("play") || name.Contains("buy") || name.Contains("claim") || name.Contains("unlock") || name.Contains("purchase"))
                    {
                        targetSound = UISoundType.ClickConfirm;
                    }

                    if (btnSound.soundType != targetSound)
                    {
                        btnSound.soundType = targetSound;
                        changed = true;
                        Debug.Log($"[SetupAudioAndUI] Updated soundType on button '{button.name}' in prefab '{prefab.name}' to {targetSound}");
                    }

                    if (!btnSound.playVibration)
                    {
                        btnSound.playVibration = true;
                        changed = true;
                        Debug.Log($"[SetupAudioAndUI] Enabled playVibration on button '{button.name}' in prefab '{prefab.name}'");
                    }
                }

                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(prefab, path);
                    Debug.Log($"[SetupAudioAndUI] Saved configured prefab '{prefab.name}' at '{path}'");
                }
                PrefabUtility.UnloadPrefabContents(prefab);
            }

            Debug.Log("[SetupAudioAndUI] Setup complete successfully!");
        }
    }
}
#endif
