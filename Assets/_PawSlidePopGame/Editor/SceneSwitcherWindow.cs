#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Utils.DevTest
{
    
    public class SceneSwitcherWindow : EditorWindow
    {
        private const string GameScenePath = "Assets/_PawSlidePopGame/Scenes/GameplayScene.unity";
        private const string EditorScenePath = "Assets/_PawSlidePopGame/Scenes/EditorScene.unity";

        [MenuItem("Tools/Fast Scene Switcher Window")]
        public static void ShowWindow()
        {
            GetWindow<SceneSwitcherWindow>("Scene Switcher");
        }

        private void OnGUI()
        {
            GUILayout.Label("Cấu hình Scene (Đang cấu hình trong Script)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox($"Game Scene Path: {GameScenePath}\nEditor Scene Path: {EditorScenePath}",
                MessageType.Info);

            GUILayout.Space(15);

            if (GUILayout.Button("Switch Scene (Hoặc bấm F12)", GUILayout.Height(40)))
            {
                ToggleScene();
            }
        }

        // Gán phím tắt F12 để chuyển scene cực nhanh không cần mở Window
        [MenuItem("Tools/Switch To Next Scene _F12")]
        public static void ToggleScene()
        {
            if (string.IsNullOrEmpty(GameScenePath) || string.IsNullOrEmpty(EditorScenePath))
            {
                Debug.LogError(
                    "[Scene Switcher] Đường dẫn Scene đang trống. Vui lòng mở file script 'SceneSwitcherWindow.cs' để cấu hình lại.");
                return;
            }

            string currentScenePath = EditorSceneManager.GetActiveScene().path;

            // Xác định scene mục tiêu để chuyển đổi
            string targetPath = (currentScenePath == GameScenePath) ? EditorScenePath : GameScenePath;

            // [Lead Tips] Kiểm tra xem đường dẫn điền trong code có bị gõ sai chính tả không trước khi mở
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(targetPath);
            if (sceneAsset == null)
            {
                Debug.LogError(
                    $"[Scene Switcher] KHÔNG tìm thấy Scene tại đường dẫn: \"{targetPath}\". Bạn hãy kiểm tra lại chính tả hoặc Copy Path lại vào code nhé!");
                return;
            }

            // Bật popup hỏi lưu lại scene hiện tại nếu có chỉnh sửa chưa save
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(targetPath);
            }
        }
    }
    
}
#endif