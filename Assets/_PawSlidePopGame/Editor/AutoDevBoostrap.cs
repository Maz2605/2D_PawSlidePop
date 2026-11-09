#if UNITY_EDITOR
using System;
using System.Reflection;
using _PawSlidePopGame._Scripts.Core.Boostrap;
using _PawSlidePopGame._Scripts.Core.System.Boostrap;
using _PawSlidePopGame._Scripts.Core.System.DesignPattern.Singleton;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _PawSlidePopGame._Scripts.Utils.Editor
{
    public static class AutoDevBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void InjectCoreOnPlay()
        {
            ResetSingletonStateForPlayMode();

            if (SceneManager.GetActiveScene().name != "GameplayScene")
            {
                return;
            }

            if (UnityEngine.Object.FindAnyObjectByType<AppBootstrap>() != null) return;

            Debug.Log("<color=yellow>⚙️ [AutoDevBootstrap] Đang tự động tiêm Core Prefab vào Scene hiện tại...</color>");

            GameObject prefab = Resources.Load<GameObject>("Core/AppBootstrap");
            
            if (prefab == null)
            {
                Debug.LogError("❌ KHÔNG TÌM THẤY PREFAB BOOTSTRAP! Kiểm tra lại thư mục Resources/Core/");
                return;
            }

            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            instance.name = "[AUTO_BOOTSTRAP_INJECTED]";

            instance.GetComponent<AppBootstrap>().RunInitFlow(isEditorAutoInject: true);
        }

        private static void ResetSingletonStateForPlayMode()
        {
            var objects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var go in objects)
            {
                if (go.name.StartsWith("[Singleton]"))
                {
                    UnityEngine.Object.DestroyImmediate(go);
                }
            }

            Type singletonBaseDefinition = typeof(Singleton<>);
            foreach (Type type in TypeCache.GetTypesDerivedFrom<MonoBehaviour>())
            {
                Type baseType = type.BaseType;
                while (baseType != null)
                {
                    if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == singletonBaseDefinition)
                    {
                        ResetSingletonStaticFields(baseType);
                        break;
                    }

                    baseType = baseType.BaseType;
                }
            }
        }

        private static void ResetSingletonStaticFields(Type closedSingletonBaseType)
        {
            const BindingFlags staticFieldFlags = BindingFlags.NonPublic | BindingFlags.Static;
            closedSingletonBaseType.GetField("_instance", staticFieldFlags)?.SetValue(null, null);
            closedSingletonBaseType.GetField("_applicationIsQuitting", staticFieldFlags)?.SetValue(null, false);
            closedSingletonBaseType.GetField("<DontDestroyOnLoadEnabled>k__BackingField", staticFieldFlags)?.SetValue(null, true);
        }
    }
}
#endif
