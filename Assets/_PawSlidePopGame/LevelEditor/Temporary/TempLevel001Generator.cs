#if UNITY_EDITOR
using System.Linq;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Contracts;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Services;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Persistence;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;
using UnityEditor;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.Temporary
{
    public static class TempLevel001Generator
    {
        [MenuItem("Tools/Paw Slide Pop/Level Editor/Generate Temp Level 001")]
        public static void GenerateMenu()
        {
            Match3TileDatabaseSO database = AssetDatabase.LoadAssetAtPath<Match3TileDatabaseSO>("Assets/_PawSlidePopGame/_Data/Tiles/Database.asset");
            if (database == null)
            {
                Debug.LogError("[LevelEditor] Tile database asset not found.");
                return;
            }

            TryGenerateAndExport(database);
        }

        public static bool TryGenerateAndExport(Match3TileDatabaseSO database)
        {
            LevelGenerationRequest request = TempLevel001GenerationProfile.CreateRequest();
            LevelGenerationService generator = new LevelGenerationService();
            LevelJsonExportService exporter = new LevelJsonExportService();

            var result = generator.Generate(request, database);
            if (!result.success)
            {
                Debug.LogError($"[LevelEditor] Temp level generation failed. {result.message}");
                if (result.validationResult != null)
                {
                    for (int i = 0; i < result.validationResult.Issues.Count; i++)
                    {
                        Debug.LogError($"[LevelEditor] {result.validationResult.Issues[i].Code}: {result.validationResult.Issues[i].Message}");
                    }
                }

                return false;
            }

            if (!exporter.TryExport(result.document, request.levelId, true))
            {
                Debug.LogError("[LevelEditor] Temp level export failed.");
                return false;
            }

            int distinctBaseTileCount = result.levelData.tileLayout.Distinct().Count(tileId => tileId > 0);
            Debug.Log(
                $"[LevelEditor] Generated {request.levelId}. Board={request.width}x{request.height}, Moves={request.movesLimit}, Targets=105x10|301x5, Spawnables={string.Join(",", result.levelData.spawnableTileIds)}, IceCount={result.placedIceCount}, DistinctBaseTiles={distinctBaseTileCount}, HasInitialMatches={result.hasInitialMatches}");
            return true;
        }
    }
}
#endif
