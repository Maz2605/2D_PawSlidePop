using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Contracts;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Services;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Persistence;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Validation;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;
using UnityEditor;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Gameplay.Meta.MapManager.Editor
{
    public sealed class MapLevelBatchGeneratorWindow : EditorWindow
    {
        private const string DefaultDatabasePath = "Assets/_PawSlidePopGame/_Data/Tiles/Database.asset";

        [SerializeField] private LevelGenerationProfileSO profile;
        [SerializeField] private Match3TileDatabaseSO tileDatabase;
        [SerializeField, Min(1)] private int retryCount = 3;
        [SerializeField] private bool overwriteExisting = true;

        [MenuItem("Tools/Paw Slide Pop/Map/Generate Map Levels")]
        public static void Open()
        {
            MapLevelBatchGeneratorWindow window = GetWindow<MapLevelBatchGeneratorWindow>("Map Level Generator");
            window.minSize = new Vector2(380f, 180f);
            window.TryLoadDefaultDatabase();
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Generate playable level JSON for MapSubScreen.", EditorStyles.boldLabel);
            EditorGUILayout.Space(8f);

            profile = (LevelGenerationProfileSO)EditorGUILayout.ObjectField("Profile", profile, typeof(LevelGenerationProfileSO), false);
            tileDatabase = (Match3TileDatabaseSO)EditorGUILayout.ObjectField("Tile Database", tileDatabase, typeof(Match3TileDatabaseSO), false);
            retryCount = EditorGUILayout.IntSlider("Retry Count", retryCount, 1, 20);
            overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing", overwriteExisting);

            using (new EditorGUI.DisabledScope(profile == null || tileDatabase == null))
            {
                if (GUILayout.Button("Generate Levels", GUILayout.Height(32f)))
                {
                    GenerateLevels();
                }
            }

            if (profile == null)
            {
                EditorGUILayout.HelpBox("Create and assign a LevelGenerationProfileSO first.", MessageType.Info);
            }
        }

        private void TryLoadDefaultDatabase()
        {
            if (tileDatabase != null)
            {
                return;
            }

            tileDatabase = AssetDatabase.LoadAssetAtPath<Match3TileDatabaseSO>(DefaultDatabasePath);
        }

        private void GenerateLevels()
        {
            if (profile == null || tileDatabase == null)
            {
                Debug.LogError("[MapLevelGenerator] Missing profile or tile database.");
                return;
            }

            LevelGenerationService generator = new LevelGenerationService();
            LevelJsonExportService exporter = new LevelJsonExportService();
            int generatedCount = 0;

            for (int levelNumber = profile.FirstLevelNumber; levelNumber <= profile.LastLevelNumber; levelNumber++)
            {
                if (!TryGenerateOneLevel(generator, exporter, levelNumber))
                {
                    Debug.LogError($"[MapLevelGenerator] Stopped at level {levelNumber:000}. Fix profile constraints before continuing.");
                    AssetDatabase.Refresh();
                    return;
                }

                generatedCount++;
            }

            AssetDatabase.Refresh();
            Debug.Log($"[MapLevelGenerator] Generated {generatedCount} levels from {profile.FormatLevelId(profile.FirstLevelNumber)} to {profile.FormatLevelId(profile.LastLevelNumber)}.");
        }

        private bool TryGenerateOneLevel(LevelGenerationService generator, LevelJsonExportService exporter, int levelNumber)
        {
            LevelGenerationResult lastResult = null;
            for (int attempt = 1; attempt <= retryCount; attempt++)
            {
                LevelGenerationRequest request = profile.CreateRequest(levelNumber);
                lastResult = generator.Generate(request, tileDatabase);
                if (!lastResult.success)
                {
                    continue;
                }

                if (!exporter.TryExport(lastResult.document, request.levelId, overwriteExisting))
                {
                    Debug.LogError($"[MapLevelGenerator] Export failed for {request.levelId}.");
                    return false;
                }

                Debug.Log($"[MapLevelGenerator] Generated {request.levelId}. Moves={request.movesLimit}, Targets={request.targets.Count}, Overlays={request.requiredOverlayPlacements.Count}.");
                return true;
            }

            LogGenerationFailure(levelNumber, lastResult);
            return false;
        }

        private void LogGenerationFailure(int levelNumber, LevelGenerationResult result)
        {
            string levelId = profile.FormatLevelId(levelNumber);
            Debug.LogError($"[MapLevelGenerator] Failed to generate {levelId}. {result?.message}");
            Match3LevelDataValidationResult validation = result?.validationResult;
            if (validation == null)
            {
                return;
            }

            for (int i = 0; i < validation.Issues.Count; i++)
            {
                Debug.LogError($"[MapLevelGenerator] {levelId} {validation.Issues[i].Code}: {validation.Issues[i].Message}");
            }
        }
    }
}
