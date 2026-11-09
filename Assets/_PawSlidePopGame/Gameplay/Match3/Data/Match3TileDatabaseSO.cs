using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Data
{
    [CreateAssetMenu(fileName = "Match3TileDatabase", menuName = "_PawSlidePopGame/Match3/Tile Database")]
    public class Match3TileDatabaseSO : ScriptableObject
    {
        [Header("Authoring")]
        [SerializeField] private List<NormalTileDefinitionSO> normalTileDefinitions = new List<NormalTileDefinitionSO>();
        [SerializeField] private List<BoosterTileDefinitionSO> boosterTileDefinitions = new List<BoosterTileDefinitionSO>();
        [SerializeField] private List<BlockerTileDefinitionSO> blockerTileDefinitions = new List<BlockerTileDefinitionSO>();
        [SerializeField] private List<TargetTileDefinitionSO> targetTileDefinitions = new List<TargetTileDefinitionSO>();
        [SerializeField] private List<OverlayDefinitionSO> overlayDefinitions = new List<OverlayDefinitionSO>();
        [SerializeField] private List<UnderlayDefinitionSO> underlayDefinitions = new List<UnderlayDefinitionSO>();
        [SerializeField, HideInInspector] private List<TileDefinitionSO> tileDefinitions = new List<TileDefinitionSO>();

        private readonly Dictionary<int, BoardContentDefinitionSO> _definitionsById = new Dictionary<int, BoardContentDefinitionSO>();
        private readonly List<TileDefinitionSO> _spawnableTiles = new List<TileDefinitionSO>();
        private readonly List<BoardContentDefinitionSO> _allDefinitions = new List<BoardContentDefinitionSO>();

        public IReadOnlyList<BoardContentDefinitionSO> Tiles => _allDefinitions;
        public int TileCount => _allDefinitions.Count;
        public int TileDefinitionCount => normalTileDefinitions.Count + boosterTileDefinitions.Count + blockerTileDefinitions.Count + targetTileDefinitions.Count;
        public int OverlayDefinitionCount => overlayDefinitions.Count;
        public int UnderlayDefinitionCount => underlayDefinitions.Count;
        public int NormalTileCount => normalTileDefinitions.Count;
        public int BoosterTileCount => boosterTileDefinitions.Count;
        public int BlockerTileCount => blockerTileDefinitions.Count;
        public int TargetTileCount => targetTileDefinitions.Count;
        public int CachedTileCount => _definitionsById.Count;
        public int SpawnableTileCount => _spawnableTiles.Count;

        private void OnEnable()
        {
            RebuildCache();
        }

        private void OnValidate()
        {
            RebuildCache();
        }

        public void RebuildCache()
        {
            _definitionsById.Clear();
            _spawnableTiles.Clear();
            _allDefinitions.Clear();
            SynchronizeTypedTileLists();

            HashSet<int> seenIds = new HashSet<int>();
            ValidateAndCacheList(normalTileDefinitions, "normalTileDefinitions", seenIds);
            ValidateAndCacheList(boosterTileDefinitions, "boosterTileDefinitions", seenIds);
            ValidateAndCacheList(blockerTileDefinitions, "blockerTileDefinitions", seenIds);
            ValidateAndCacheList(targetTileDefinitions, "targetTileDefinitions", seenIds);
            ValidateAndCacheList(overlayDefinitions, "overlayDefinitions", seenIds);
            ValidateAndCacheList(underlayDefinitions, "underlayDefinitions", seenIds);
        }

        public void ValidateAndLogSummary()
        {
            RebuildCache();
            Debug.Log(
                $"[Match3TileDatabase] Validation finished for '{name}'. Total Entries: {TileCount}, Tiles: {TileDefinitionCount}, Normal: {NormalTileCount}, Boosters: {BoosterTileCount}, Blockers: {BlockerTileCount}, Targets: {TargetTileCount}, Overlays: {OverlayDefinitionCount}, Underlays: {UnderlayDefinitionCount}, Valid Unique Tiles: {CachedTileCount}, Spawnable Normal Tiles: {SpawnableTileCount}.",
                this);
        }

        public BoardContentDefinitionSO GetContentDefinition(int tileId)
        {
            EnsureCache();
            _definitionsById.TryGetValue(tileId, out BoardContentDefinitionSO definition);
            return definition;
        }

        public BoardContentDefinitionSO GetTargetContentDefinition(int tileId)
        {
            return GetContentDefinition(tileId);
        }

        public TileDefinitionSO GetTileDefinition(int tileId)
        {
            return GetContentDefinition(tileId) as TileDefinitionSO;
        }

        public OverlayDefinitionSO GetOverlayDefinition(int tileId)
        {
            return GetContentDefinition(tileId) as OverlayDefinitionSO;
        }

        public UnderlayDefinitionSO GetUnderlayDefinition(int tileId)
        {
            return GetContentDefinition(tileId) as UnderlayDefinitionSO;
        }

        public bool IsTargetObjectiveSupported(int tileId, out BoardContentDefinitionSO definition, out string reason)
        {
            definition = GetTargetContentDefinition(tileId);
            if (definition == null)
            {
                reason = "Target content id is unknown.";
                return false;
            }

            reason = definition.TargetObjectiveRestrictionReason;
            return definition.SupportsTargetObjective;
        }

        public BoosterTileDefinitionSO GetBoosterDefinition(TileLogicType logicType)
        {
            EnsureCache();

            for (int i = 0; i < boosterTileDefinitions.Count; i++)
            {
                BoosterTileDefinitionSO booster = boosterTileDefinitions[i];
                if (booster != null && booster.LogicType == logicType)
                {
                    return booster;
                }
            }

            return null;
        }

        public BlockerTileDefinitionSO GetBlockerDefinition(int tileId)
        {
            return GetTileDefinition(tileId) as BlockerTileDefinitionSO;
        }

        public TargetTileDefinitionSO GetTargetDefinition(int tileId)
        {
            return GetTileDefinition(tileId) as TargetTileDefinitionSO;
        }

        public int GetRandomSpawnableTileId(System.Random random, IReadOnlyList<int> restrictedTileIds = null)
        {
            EnsureCache();

            if (_spawnableTiles.Count == 0)
            {
                return 0;
            }

            int totalWeight = 0;
            for (int i = 0; i < _spawnableTiles.Count; i++)
            {
                TileDefinitionSO tile = _spawnableTiles[i];
                if (IsRestricted(tile.TileId, restrictedTileIds))
                {
                    continue;
                }

                totalWeight += Mathf.Max(0, tile.SpawnWeight);
            }

            if (totalWeight <= 0)
            {
                return 0;
            }

            int roll = random.Next(0, totalWeight);
            for (int i = 0; i < _spawnableTiles.Count; i++)
            {
                TileDefinitionSO tile = _spawnableTiles[i];
                if (IsRestricted(tile.TileId, restrictedTileIds))
                {
                    continue;
                }

                roll -= Mathf.Max(0, tile.SpawnWeight);
                if (roll < 0)
                {
                    return tile.TileId;
                }
            }

            return 0;
        }

        private void EnsureCache()
        {
            if (_definitionsById.Count == 0)
            {
                RebuildCache();
            }
        }

        private void SynchronizeTypedTileLists()
        {
            if (HasTypedTileDefinitions())
            {
                RebuildLegacyTileList();
                return;
            }

            if (tileDefinitions.Count == 0)
            {
                return;
            }

            normalTileDefinitions.Clear();
            boosterTileDefinitions.Clear();
            blockerTileDefinitions.Clear();
            targetTileDefinitions.Clear();

            for (int i = 0; i < tileDefinitions.Count; i++)
            {
                TileDefinitionSO definition = tileDefinitions[i];
                switch (definition)
                {
                    case NormalTileDefinitionSO normal:
                        normalTileDefinitions.Add(normal);
                        break;
                    case BoosterTileDefinitionSO booster:
                        boosterTileDefinitions.Add(booster);
                        break;
                    case BlockerTileDefinitionSO blocker:
                        blockerTileDefinitions.Add(blocker);
                        break;
                    case TargetTileDefinitionSO target:
                        targetTileDefinitions.Add(target);
                        break;
                }
            }

            RebuildLegacyTileList();
        }

        private bool HasTypedTileDefinitions()
        {
            return normalTileDefinitions.Count > 0 ||
                   boosterTileDefinitions.Count > 0 ||
                   blockerTileDefinitions.Count > 0 ||
                   targetTileDefinitions.Count > 0;
        }

        private void RebuildLegacyTileList()
        {
            tileDefinitions.Clear();
            AppendDefinitions(normalTileDefinitions);
            AppendDefinitions(boosterTileDefinitions);
            AppendDefinitions(blockerTileDefinitions);
            AppendDefinitions(targetTileDefinitions);
        }

        private void ValidateAndCacheList<TDefinition>(List<TDefinition> source, string listName, HashSet<int> seenIds) where TDefinition : BoardContentDefinitionSO
        {
            for (int i = 0; i < source.Count; i++)
            {
                ValidateAndCacheDefinition(source[i], listName, i, seenIds);
            }
        }

        private void ValidateAndCacheDefinition(BoardContentDefinitionSO definition, string listName, int index, HashSet<int> seenIds)
        {
            if (definition == null)
            {
                Debug.LogError($"[Match3TileDatabase] Null definition entry at index {index} in {listName} on database {name}.", this);
                return;
            }

            _allDefinitions.Add(definition);

            if (definition.TileId <= 0)
            {
                Debug.LogError($"[Match3TileDatabase] Definition '{definition.name}' has invalid Tile Id {definition.TileId}.", definition);
                return;
            }

            if (!seenIds.Add(definition.TileId))
            {
                Debug.LogError($"[Match3TileDatabase] Duplicate Tile Id {definition.TileId} found in database {name}.", definition);
                return;
            }

            _definitionsById[definition.TileId] = definition;

            if (definition is TileDefinitionSO tileDefinition &&
                tileDefinition.TileKind == TileKind.Normal &&
                tileDefinition.CanSpawnOnRefill &&
                tileDefinition.SpawnWeight > 0)
            {
                _spawnableTiles.Add(tileDefinition);
            }

            if (definition.TileViewPrefab == null)
            {
                Debug.LogWarning($"[Match3TileDatabase] Definition '{definition.name}' has no Tile View Prefab assigned.", definition);
            }
        }

        private void AppendDefinitions<TDefinition>(List<TDefinition> source) where TDefinition : TileDefinitionSO
        {
            for (int i = 0; i < source.Count; i++)
            {
                if (source[i] != null)
                {
                    tileDefinitions.Add(source[i]);
                }
            }
        }

        private static bool IsRestricted(int tileId, IReadOnlyList<int> restrictedTileIds)
        {
            if (restrictedTileIds == null)
            {
                return false;
            }

            for (int i = 0; i < restrictedTileIds.Count; i++)
            {
                if (restrictedTileIds[i] == tileId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

