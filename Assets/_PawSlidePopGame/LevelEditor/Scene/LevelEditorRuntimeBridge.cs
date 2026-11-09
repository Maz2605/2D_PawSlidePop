using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Contracts;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Services;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Temporary;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.Scene
{
    public sealed class LevelEditorRuntimeBridge : MonoBehaviour
    {
        [SerializeField] private Match3TileDatabaseSO tileDatabase;

        private readonly LevelGenerationService _generationService = new LevelGenerationService();

        public Match3TileDatabaseSO TileDatabase => tileDatabase;

        public void SetTileDatabaseForEditor(Match3TileDatabaseSO database)
        {
            tileDatabase = database;
        }

        public bool TryGenerateTempLevel(out LevelGenerationResult result)
        {
            result = null;
            if (tileDatabase == null)
            {
                Debug.LogError("[LevelEditor] Runtime bridge is missing tile database.");
                return false;
            }

            result = _generationService.Generate(TempLevel001GenerationProfile.CreateRequest(), tileDatabase);
            return result != null && result.success;
        }
    }
}
