#if UNITY_EDITOR
using _PawSlidePopGame._Scripts.Core.System.Boostrap;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _PawSlidePopGame._Scripts.Utils.Editor
{
    public static class AutoDevBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void InjectCoreOnPlay()
        {
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

            // AppBootstrap.Start initializes after scene Awake; AudioMixer is not ready BeforeSceneLoad.
        }

    }
}
#endif

