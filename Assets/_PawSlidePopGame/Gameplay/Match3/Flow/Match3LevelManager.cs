using _PawSlidePopGame._Scripts.Data.LevelProvider;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Validation;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Flow
{
    [DisallowMultipleComponent]
    public sealed class Match3LevelManager : MonoBehaviour
    {
        public static Match3LevelManager Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        [SerializeField] private string defaultLevelId = "Level_001";
        [SerializeField] private Match3TileDatabaseSO tileDatabase;

        private ILevelDataProvider _levelDataProvider;
        private string _requestedLevelId;

        public string CurrentLevelId { get; private set; }
        public Match3LevelData CurrentLevelData { get; private set; }
        public Match3TileDatabaseSO TileDatabase => tileDatabase;

        public void SetRequestedLevelId(string levelId)
        {
            _requestedLevelId = string.IsNullOrWhiteSpace(levelId)
                ? string.Empty
                : levelId.Trim();
        }

        public bool TryLoadCurrentLevel(out Match3LevelData levelData)
        {
            string levelId = !string.IsNullOrWhiteSpace(_requestedLevelId)
                ? _requestedLevelId
                : defaultLevelId;
            return TryLoadLevel(levelId, out levelData);
        }

        public bool TryLoadLevel(string levelId, out Match3LevelData levelData)
        {
            levelData = null;
            string resolvedLevelId = string.IsNullOrWhiteSpace(levelId)
                ? defaultLevelId
                : levelId.Trim();
            if (string.IsNullOrWhiteSpace(resolvedLevelId))
            {
                Debug.LogError("[LevelManager] Cannot load level because level id is empty.", this);
                return false;
            }

            if (!GetLevelDataProvider().TryGetLevelData(resolvedLevelId, out levelData) || levelData == null)
            {
                Debug.LogError($"[LevelManager] Failed to load level '{resolvedLevelId}' from JSON.", this);
                return false;
            }

            Match3LevelDataValidationResult validation = Match3LevelDataValidator.Validate(levelData, tileDatabase);
            if (!validation.IsValid)
            {
                Debug.LogError($"[LevelManager] Gameplay boot aborted because level '{resolvedLevelId}' is invalid.", this);
                for (int i = 0; i < validation.Issues.Count; i++)
                {
                    if (validation.Issues[i].Severity == Match3LevelDataValidationSeverity.Error)
                    {
                        Debug.LogError($"[LevelManager] {validation.Issues[i].Code}: {validation.Issues[i].Message}", this);
                    }
                }

                return false;
            }

            CurrentLevelId = resolvedLevelId;
            CurrentLevelData = levelData;
            _requestedLevelId = resolvedLevelId;
            Debug.Log($"[LevelManager] Loaded level '{resolvedLevelId}' from JSON.", this);
            return true;
        }

        public void ClearLoadedLevel()
        {
            CurrentLevelId = null;
            CurrentLevelData = null;
        }

        private ILevelDataProvider GetLevelDataProvider()
        {
            if (_levelDataProvider == null)
            {
                _levelDataProvider = new JsonLevelDataProvider();
            }

            return _levelDataProvider;
        }
    }
}
