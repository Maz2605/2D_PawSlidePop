using System;
using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Feature.Match3.Boosters;
using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Move;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Resolution;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Rules;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;
using _PawSlidePopGame._Scripts.Feature.Match3.Presentation;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Presenter
{
    public class Match3GameManager : MonoBehaviour
    {
        public static Match3GameManager Instance { get; private set; }

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

        [Header("Data")]
        [SerializeField] private Match3TileDatabaseSO tileDatabase;

        [Header("Runtime Config")]
        [SerializeField] private int randomSeed = 12345;
        [SerializeField] private bool randomizeSeedEachGame = true;
        [SerializeField] private bool resolveBoardOnStart = true;

        private BoardModel _board;
        private BoardRuleSet _ruleSet;
        private System.Random _random;
        private BoardResolutionResult _lastMoveResult;
        private BoardMoveExecutionResult _lastExecutionResult;
        private bool _isInitialized;
        private Match3LevelData _activeLevelData;
        private readonly List<BoosterDefinitionSO> _preLevelBoosters = new List<BoosterDefinitionSO>();

        public BoardModel Board => _board;
        public Match3LevelData LevelData => _activeLevelData;
        public Match3TileDatabaseSO TileDatabase => tileDatabase;
        public BoardResolutionResult LastMoveResult => _lastMoveResult;
        public BoardMoveExecutionResult LastExecutionResult => _lastExecutionResult;
        public bool IsInitialized => _isInitialized;
        public System.Random Random => _random;
        public BoardRuleSet RuleSet => _ruleSet;
        public IReadOnlyList<BoosterDefinitionSO> PreLevelBoosters => _preLevelBoosters;

        public event Action<BoardModel> OnBoardInitialized;
        public event Action<BoardMoveExecutionResult> OnMoveExecuted;

        public void SetLevelData(Match3LevelData levelData)
        {
            _activeLevelData = levelData;
        }

        public void SetPreLevelBoosters(IReadOnlyList<BoosterDefinitionSO> preLevelBoosters)
        {
            _preLevelBoosters.Clear();
            if (preLevelBoosters == null)
            {
                return;
            }

            for (int i = 0; i < preLevelBoosters.Count; i++)
            {
                BoosterDefinitionSO definition = preLevelBoosters[i];
                if (definition != null && definition.UsagePhase == BoosterUsagePhase.PreLevel)
                {
                    _preLevelBoosters.Add(definition);
                }
            }
        }

        public void InitializeGame()
        {
            if (_isInitialized)
            {
                return;
            }

            if (tileDatabase == null)
            {
                Debug.LogError("[Match3GameManager] Missing tile database.");
                return;
            }

            Match3LevelData levelData = _activeLevelData;
            if (levelData == null)
            {
                Debug.LogError("[Match3GameManager] Missing active level data. Inject level data before InitializeGame().");
                return;
            }

            int seed = ResolveGameSeed();
            _random = new System.Random(seed);
            _ruleSet = BoardRuleSet.Default;
            _board = new BoardModel(levelData);
            tileDatabase.RebuildCache();
            _board.PopulateBoard(levelData.underlayLayout, levelData.tileLayout, levelData.overlayLayout, tileDatabase);

            if (resolveBoardOnStart)
            {
                BoardRefillService.Apply(_board, levelData, tileDatabase, _random);
                BoardResolutionService.ResolveBoard(_board, levelData, tileDatabase, _random, _ruleSet);

                int safetyCount = 10;
                while (!BoardResolutionService.HasPossibleMoves(_board, _ruleSet.MatchRule) && safetyCount > 0)
                {
                    BoosterResolutionService.ExecuteShuffle(_board, levelData, tileDatabase, _random, _ruleSet);
                    safetyCount--;
                }
            }

            _lastMoveResult = new BoardResolutionResult();
            _lastExecutionResult = new BoardMoveExecutionResult
            {
                ResolutionResult = _lastMoveResult
            };
            _isInitialized = true;
            Debug.Log($"[Match3GameManager] Initialized level '{levelData.levelID}' with seed {seed}.", this);
            OnBoardInitialized?.Invoke(_board);
        }

        public void ResetGame()
        {
            _board = null;
            _ruleSet = null;
            _random = null;
            _lastMoveResult = null;
            _lastExecutionResult = null;
            _activeLevelData = null;
            _preLevelBoosters.Clear();
            _isInitialized = false;
        }

        public BoardMoveExecutionResult ExecuteMove(BoardMoveRequest request)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("[Match3GameManager] Game is not initialized.");
                return new BoardMoveExecutionResult();
            }

            _lastExecutionResult = BoardResolutionService.ExecuteMove(
                _board,
                request,
                LevelData,
                tileDatabase,
                _random,
                _ruleSet);

            _lastMoveResult = _lastExecutionResult.ResolutionResult;
            if (_lastExecutionResult.IsApplied)
            {
                OnMoveExecuted?.Invoke(_lastExecutionResult);
            }

            return _lastExecutionResult;
        }

        public bool TryResolveMove(BoardMoveRequest request)
        {
            return ExecuteMove(request).IsAccepted;
        }

        public BoardMoveExecutionResult ExecuteTileActivation(int x, int y)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("[Match3GameManager] Game is not initialized.");
                return new BoardMoveExecutionResult();
            }

            _lastExecutionResult = BoardResolutionService.ExecuteTileActivation(
                _board,
                x,
                y,
                LevelData,
                tileDatabase,
                _random,
                _ruleSet);

            _lastMoveResult = _lastExecutionResult.ResolutionResult;
            if (_lastExecutionResult.IsApplied)
            {
                OnMoveExecuted?.Invoke(_lastExecutionResult);
            }

            return _lastExecutionResult;
        }

        public CellModel FindStartingTileCell(HashSet<CellModel> occupiedCells)
        {
            if (_board == null)
            {
                return null;
            }

            List<CellModel> candidates = CollectStartingTileCandidates(occupiedCells, requireNormalTile: true);
            if (candidates.Count == 0)
            {
                candidates = CollectStartingTileCandidates(occupiedCells, requireNormalTile: false);
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            int index = _random != null ? _random.Next(0, candidates.Count) : UnityEngine.Random.Range(0, candidates.Count);
            return candidates[index];
        }

        public void PlaceStartingTileAt(CellModel cell, int tileId)
        {
            if (cell == null || tileId <= 0 || _board == null || tileDatabase == null)
            {
                return;
            }
            _board.SetTileFromDefinitionId(cell, tileId, tileDatabase);
        }

        private void ApplyPreLevelBoosters()
        {
            if (_preLevelBoosters.Count == 0 || _board == null)
            {
                return;
            }

            HashSet<CellModel> occupiedCells = new HashSet<CellModel>();
            for (int i = 0; i < _preLevelBoosters.Count; i++)
            {
                BoosterDefinitionSO definition = _preLevelBoosters[i];
                if (definition == null)
                {
                    continue;
                }

                switch (definition.PreLevelEffectType)
                {
                    case PreLevelBoosterEffectType.ExtraMoves:
                        _board.AddMoves(definition.ExtraMovesAmount);
                        break;
                    case PreLevelBoosterEffectType.PlaceStartingTile:
                        TryPlaceStartingTile(definition.StartingTileId, occupiedCells);
                        break;
                }
            }
        }

        private int ResolveGameSeed()
        {
            if (!randomizeSeedEachGame)
            {
                return randomSeed;
            }

            unchecked
            {
                int seed = Environment.TickCount;
                seed = (seed * 397) ^ Guid.NewGuid().GetHashCode();
                seed = (seed * 397) ^ DateTime.UtcNow.Ticks.GetHashCode();
                return seed;
            }
        }

        private bool TryPlaceStartingTile(int tileId, HashSet<CellModel> occupiedCells)
        {
            if (tileId <= 0 || tileDatabase == null)
            {
                return false;
            }

            List<CellModel> candidates = CollectStartingTileCandidates(occupiedCells, requireNormalTile: true);
            if (candidates.Count == 0)
            {
                candidates = CollectStartingTileCandidates(occupiedCells, requireNormalTile: false);
            }

            if (candidates.Count == 0)
            {
                Debug.LogWarning($"[Match3GameManager] Could not place pre-level starting tile {tileId}: no valid board cell.", this);
                return false;
            }

            int index = _random != null ? _random.Next(0, candidates.Count) : UnityEngine.Random.Range(0, candidates.Count);
            CellModel targetCell = candidates[index];
            _board.SetTileFromDefinitionId(targetCell, tileId, tileDatabase);
            occupiedCells?.Add(targetCell);
            return targetCell.Tile != null && targetCell.Tile.TileId == tileId;
        }

        private List<CellModel> CollectStartingTileCandidates(HashSet<CellModel> occupiedCells, bool requireNormalTile)
        {
            List<CellModel> candidates = new List<CellModel>();
            if (_board == null)
            {
                return candidates;
            }

            foreach (CellModel cell in _board.GetAllCells())
            {
                if (cell == null ||
                    !cell.IsPlayable ||
                    occupiedCells != null && occupiedCells.Contains(cell) ||
                    cell.Underlay != null ||
                    cell.Overlay != null ||
                    cell.Tile == null)
                {
                    continue;
                }

                if (requireNormalTile && cell.Tile.TileKind != TileKind.Normal)
                {
                    continue;
                }

                candidates.Add(cell);
            }

            return candidates;
        }
    }
}

