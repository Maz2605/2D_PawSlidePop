using System;
using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Data.LevelProvider;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Contracts;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Mapping;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Model;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Services;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Persistence;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Scene;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Validation;
using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.UI
{
    public sealed class LevelEditorUIController : MonoBehaviour
    {
        private const string DefaultCellArtCatalogResourcePath = "LevelEditor/LevelEditorCellArtCatalog";

        [SerializeField] private LevelEditorRuntimeBridge runtimeBridge;
        [SerializeField] private LevelEditorSessionStateHolder sessionStateHolder;
        [SerializeField] private LevelEditorSelectionStateHolder selectionStateHolder;
        [SerializeField] private LevelEditorCellArtCatalogSO cellArtCatalog;

        private readonly JsonLevelDataProvider _levelDataProvider = new JsonLevelDataProvider();
        private readonly ResourcesLevelCatalogProvider _levelCatalogProvider = new ResourcesLevelCatalogProvider();
        private readonly LevelJsonExportService _exportService = new LevelJsonExportService();
        private readonly List<LevelTargetRequirement> _targets = new List<LevelTargetRequirement>();

        public event Action Changed;

        public Match3TileDatabaseSO TileDatabase => runtimeBridge != null ? runtimeBridge.TileDatabase : null;
        public LevelEditorCellArtCatalogSO CellArtCatalog => ResolveCellArtCatalog();
        public LevelEditorSessionContext Session => sessionStateHolder != null ? sessionStateHolder.Context : null;
        public LevelEditorSelectionStateHolder Selection => selectionStateHolder;
        public IReadOnlyList<LevelTargetRequirement> Targets => _targets;

        private LevelEditorViewMode _viewMode = LevelEditorViewMode.ShowAll;
        public LevelEditorViewMode ViewMode
        {
            get => _viewMode;
            set
            {
                if (_viewMode != value)
                {
                    _viewMode = value;
                    NotifyChanged();
                }
            }
        }

        private int _hoveredX = -1;
        private int _hoveredY = -1;
        public int HoveredX => _hoveredX;
        public int HoveredY => _hoveredY;

        public void SetHoveredCoordinate(int x, int y)
        {
            if (_hoveredX != x || _hoveredY != y)
            {
                _hoveredX = x;
                _hoveredY = y;
                NotifyChanged();
            }
        }

        private void Awake()
        {
            EnsureSession();
        }

        public void Configure(LevelEditorRuntimeBridge bridge, LevelEditorSessionStateHolder sessionHolder, LevelEditorSelectionStateHolder selectionHolder)
        {
            runtimeBridge = bridge;
            sessionStateHolder = sessionHolder;
            selectionStateHolder = selectionHolder;
            ResolveCellArtCatalog();
            EnsureSession();
        }

        public void EnsureSession()
        {
            if (sessionStateHolder == null)
            {
                return;
            }

            if (sessionStateHolder.Context.board == null || sessionStateHolder.Context.board.CellCount <= 0)
            {
                NewLevel(
                    sessionStateHolder.CurrentLevelId,
                    sessionStateHolder.DisplayLevelNumber,
                    sessionStateHolder.Width,
                    sessionStateHolder.Height,
                    sessionStateHolder.MovesLimit);
            }
        }

        public void NewLevel(string levelId, int displayLevelNumber, int width, int height, int movesLimit)
        {
            bool replacingDirtySession = Session?.isDirty ?? false;
            string previousLevelId = Session != null ? Session.levelId : string.Empty;
            if (replacingDirtySession)
            {
                Debug.LogWarning($"[LevelEditor] New level will replace unsaved changes in '{previousLevelId}'. Save before New if you want to keep them.");
            }

            LevelEditorSessionContext context = LevelEditorDocumentMapper.CreateNew(
                levelId,
                displayLevelNumber,
                width,
                height,
                movesLimit);
            context.spawnableTileIds = BuildDefaultSpawnables();
            context.statusMessage = replacingDirtySession
                ? $"Created new level session '{context.levelId}'. Unsaved changes from '{previousLevelId}' were replaced in editor."
                : $"Created new level session '{context.levelId}'.";
            context.errorMessage = string.Empty;
            _targets.Clear();
            context.lastValidationResult = ValidateDocument(LevelEditorDocumentMapper.BuildDocument(context, TileDatabase, _targets).levelData);
            sessionStateHolder.LoadContext(context);
            selectionStateHolder.ClearSelection();
            Debug.Log($"[LevelEditor] Created new level session '{context.levelId}' with board {context.board.width}x{context.board.height}, moves={context.movesLimit}.");
            NotifyChanged();
        }

        public LevelEditorLoadResult LoadLevel(string levelId)
        {
            string sanitizedLevelId = LevelPathUtility.SanitizeLevelId(levelId);
            bool replacingDirtySession = Session?.isDirty ?? false;
            string previousLevelId = Session != null ? Session.levelId : string.Empty;
            LevelEditorLoadResult result = new LevelEditorLoadResult
            {
                success = false,
                levelId = sanitizedLevelId
            };

            if (string.IsNullOrWhiteSpace(sanitizedLevelId))
            {
                result.message = "Level id is empty. Use a value like Level_001.";
                sessionStateHolder?.SetStatus(string.Empty, result.message);
                Debug.LogWarning($"[LevelEditor] Load blocked. {result.message}");
                NotifyChanged();
                return result;
            }

            if (!LevelExists(sanitizedLevelId))
            {
                result.message = $"Level '{sanitizedLevelId}' does not exist. Use New to create a session, or Save to write it as a JSON level.";
                sessionStateHolder?.SetStatus(string.Empty, result.message);
                Debug.LogWarning($"[LevelEditor] Load blocked. {result.message}");
                NotifyChanged();
                return result;
            }

            if (!_levelDataProvider.TryGetLevelDocument(sanitizedLevelId, out LevelSaveData document))
            {
                result.message = $"Failed to load level '{sanitizedLevelId}'.";
                sessionStateHolder?.SetStatus(string.Empty, result.message);
                Debug.LogError($"[LevelEditor] {result.message}");
                NotifyChanged();
                return result;
            }

            if (replacingDirtySession)
            {
                Debug.LogWarning($"[LevelEditor] Loading '{sanitizedLevelId}' will replace unsaved changes in '{previousLevelId}'. Save before Load if you want to keep them.");
            }

            LevelEditorSessionContext context = LevelEditorDocumentMapper.LoadFromDocument(document);
            context.statusMessage = replacingDirtySession
                ? $"Loaded level '{context.levelId}'. Unsaved changes from '{previousLevelId}' were replaced in editor."
                : $"Loaded level '{context.levelId}'.";
            context.errorMessage = string.Empty;
            context.lastValidationResult = ValidateDocument(document.levelData);
            _targets.Clear();
            _targets.AddRange(LevelEditorDocumentMapper.LoadTargetsFromDocument(document));

            sessionStateHolder.LoadContext(context);
            selectionStateHolder.ClearSelection();

            result.success = true;
            result.message = context.statusMessage;
            result.document = document;
            result.session = context.Clone();
            Debug.Log($"[LevelEditor] Loaded level '{context.levelId}' from {LevelPathUtility.GetAssetPath(context.levelId)}. Board={context.board.width}x{context.board.height}, targets={_targets.Count}.");
            NotifyChanged();
            return result;
        }

        public LevelEditorSaveResult SaveLevel(bool overwrite = true)
        {
            LevelEditorSaveResult result = new LevelEditorSaveResult
            {
                success = false,
                levelId = Session != null ? Session.levelId : string.Empty
            };
            if (Session == null || Session.board == null)
            {
                result.message = "No active level editor session.";
                sessionStateHolder?.SetStatus(string.Empty, result.message);
                Debug.LogWarning($"[LevelEditor] Save blocked. {result.message}");
                NotifyChanged();
                return result;
            }
            LevelSaveData document = LevelEditorDocumentMapper.BuildDocument(Session, TileDatabase, _targets);
            Match3LevelDataValidationResult validation = ValidateDocument(document.levelData);
            result.validationResult = validation;
            result.document = document;
            if (!validation.IsValid)
            {
                result.message = "Save blocked because the level is invalid.";
                sessionStateHolder.SetValidation(validation);
                sessionStateHolder.SetStatus(string.Empty, result.message);
                Debug.LogWarning($"[LevelEditor] Save blocked for '{document.levelId}'. Errors={validation.ErrorCount}, warnings={validation.WarningCount}.");
                NotifyChanged();
                return result;
            }
            if (!_exportService.TryExport(document, document.levelId, overwrite))
            {
                result.message = $"Failed to export level '{document.levelId}'.";
                sessionStateHolder.SetValidation(validation);
                sessionStateHolder.SetStatus(string.Empty, result.message);
                Debug.LogError($"[LevelEditor] {result.message}");
                NotifyChanged();
                return result;
            }
            sessionStateHolder.MarkSaved(document.levelId, validation);
            result.success = true;
            result.message = $"Saved level '{document.levelId}'.";
            Debug.Log($"[LevelEditor] Saved level '{document.levelId}' to {LevelPathUtility.GetAssetPath(document.levelId)}.");
            NotifyChanged();
            return result;
        }

        public void ResizeBoard(int width, int height)
        {
            if (Session == null || Session.board == null)
            {
                Debug.LogWarning("[LevelEditor] Resize Board blocked. No active level editor session.");
                return;
            }

            LevelEditorBoardState previousBoard = Session.board.Clone();
            int previousWidth = previousBoard.width;
            int previousHeight = previousBoard.height;
            LevelEditorSessionContext resized = LevelEditorDocumentMapper.CreateNew(
                Session.levelId,
                Session.displayLevelNumber,
                width,
                height,
                Session.movesLimit,
                Session.spawnableTileIds);

            int copyWidth = Mathf.Min(previousBoard.width, resized.board.width);
            int copyHeight = Mathf.Min(previousBoard.height, resized.board.height);
            for (int y = 0; y < copyHeight; y++)
            {
                for (int x = 0; x < copyWidth; x++)
                {
                    LevelEditorCellState previousCell = previousBoard.GetCell(x, y);
                    int previousIndex = previousBoard.ToIndex(x, y);
                    int nextIndex = resized.board.ToIndex(x, y);
                    resized.board.SetUnderlayId(x, y, previousCell.UnderlayId);
                    resized.board.SetTileId(x, y, previousCell.TileId);
                    resized.board.SetOverlayId(x, y, previousCell.OverlayId);
                    resized.board.SetPlayable(x, y, previousCell.Playable);
                    if (Session.cellArtLayout != null &&
                        previousIndex < Session.cellArtLayout.Length &&
                        nextIndex < resized.cellArtLayout.Length)
                    {
                        resized.cellArtLayout[nextIndex] = Session.cellArtLayout[previousIndex];
                    }
                }
            }

            resized.isDirty = true;
            resized.statusMessage = $"Resized board to {resized.board.width}x{resized.board.height}.";
            resized.errorMessage = string.Empty;
            resized.lastValidationResult = ValidateDocument(LevelEditorDocumentMapper.BuildDocument(resized, TileDatabase, _targets).levelData);
            sessionStateHolder.LoadContext(resized);
            selectionStateHolder.ClearSelection();
            Debug.Log($"[LevelEditor] Resized board for '{resized.levelId}' from {previousWidth}x{previousHeight} to {resized.board.width}x{resized.board.height}. Existing cells preserved inside overlap.");
            NotifyChanged();
        }

        public void SetMetadata(string levelId, int displayLevelNumber, int movesLimit)
        {
            if (Session == null)
            {
                return;
            }

            sessionStateHolder.SetMetadata(levelId, displayLevelNumber, movesLimit);
            RevalidateSession();
            NotifyChanged();
        }

        public void SelectCell(int x, int y)
        {
            if (Session?.board == null || !Session.board.IsInBounds(x, y))
            {
                return;
            }

            selectionStateHolder.SetSelectedCoordinate(x, y);
            sessionStateHolder.SetStatus($"Selected cell ({x}, {y}).", string.Empty);
            NotifyChanged();
        }

        public void SetSelectedLayer(BoardLayer layer, int contentId)
        {
            if (selectionStateHolder == null)
            {
                return;
            }

            selectionStateHolder.SetSelectedLayer(layer, contentId);
            sessionStateHolder?.SetStatus($"Selected {layer} id {Mathf.Max(0, contentId)}.", string.Empty);
            NotifyChanged();
        }

        public void SetActiveSection(LevelEditorPaletteSectionType sectionType)
        {
            if (selectionStateHolder == null)
            {
                return;
            }

            selectionStateHolder.SetActiveSection(sectionType);
            
            switch (sectionType)
            {
                case LevelEditorPaletteSectionType.ItemNormal:
                case LevelEditorPaletteSectionType.ItemSpecial:
                    ViewMode = LevelEditorViewMode.Normal;
                    break;
                case LevelEditorPaletteSectionType.Overlay:
                    ViewMode = LevelEditorViewMode.Overlay;
                    break;
                case LevelEditorPaletteSectionType.Underlay:
                    ViewMode = LevelEditorViewMode.Underlay;
                    break;
                case LevelEditorPaletteSectionType.CellArt:
                    ViewMode = LevelEditorViewMode.ShowAll;
                    break;
            }

            sessionStateHolder?.SetStatus($"Active paint section: {sectionType}.", string.Empty);
            NotifyChanged();
        }

        public void SetSelectedSectionLayer(LevelEditorPaletteSectionType sectionType, int contentId)
        {
            if (selectionStateHolder == null)
            {
                return;
            }

            selectionStateHolder.SetActiveSection(sectionType);
            if (sectionType == LevelEditorPaletteSectionType.CellArt)
            {
                SetSelectedCellArt(contentId);
                return;
            }

            BoardLayer layer = LevelEditorSelectionStateHolder.ResolveBoardLayer(sectionType);
            selectionStateHolder.SetSelectedLayer(layer, contentId);
            sessionStateHolder?.SetStatus($"Selected {sectionType} id {Mathf.Max(0, contentId)}.", string.Empty);
            NotifyChanged();
        }

        public void SetSelectedCellArt(int contentId)
        {
            if (selectionStateHolder == null)
            {
                return;
            }

            selectionStateHolder.SetActiveSection(LevelEditorPaletteSectionType.CellArt);
            selectionStateHolder.SelectedCellArtId = contentId;
            sessionStateHolder?.SetStatus($"Selected {LevelEditorPaletteSectionType.CellArt} id {Mathf.Max(0, contentId)}.", string.Empty);
            NotifyChanged();
        }

        public void HandleCellClicked(int x, int y)
        {
            PaintCell(x, y);
        }

        public void PaintCell(int x, int y)
        {
            if (Session?.board == null || selectionStateHolder == null || !Session.board.IsInBounds(x, y))
            {
                return;
            }

            selectionStateHolder.SetSelectedCoordinate(x, y);
            if (selectionStateHolder.IsCellArtSectionActive)
            {
                PaintCellArt(x, y);
                return;
            }

            BoardLayer activeLayer = selectionStateHolder.ActiveBoardLayer;
            int contentId = selectionStateHolder.GetSelectedContentIdForActiveLayer();
            if (contentId <= 0)
            {
                sessionStateHolder.SetStatus($"Selected cell ({x}, {y}).", $"No {selectionStateHolder.ActiveSectionType} content selected.");
                NotifyChanged();
                return;
            }

            ApplyCellLayerContent(x, y, activeLayer, contentId);
            sessionStateHolder.MarkDirty($"Painted {activeLayer} at ({x}, {y}).");
            RevalidateSession();
            NotifyChanged();
        }

        public void EraseCell(int x, int y)
        {
            if (Session?.board == null || selectionStateHolder == null || !Session.board.IsInBounds(x, y))
            {
                return;
            }

            selectionStateHolder.SetSelectedCoordinate(x, y);
            if (selectionStateHolder.IsCellArtSectionActive)
            {
                EraseCellArt(x, y);
                return;
            }

            BoardLayer activeLayer = selectionStateHolder.ActiveBoardLayer;
            ApplyCellLayerContent(x, y, activeLayer, 0);
            sessionStateHolder.MarkDirty($"Erased {activeLayer} at ({x}, {y}).");
            RevalidateSession();
            NotifyChanged();
        }

        public void ApplySelectionPayload()
        {
            if (Session?.board == null || selectionStateHolder == null || !selectionStateHolder.HasSelectedCell)
            {
                return;
            }

            int x = selectionStateHolder.SelectedX;
            int y = selectionStateHolder.SelectedY;
            int underlayId = selectionStateHolder.SelectedUnderlayId;
            int tileId = selectionStateHolder.SelectedTileId;
            int overlayId = selectionStateHolder.SelectedOverlayId;
            bool hasAnyContent = underlayId > 0 || tileId > 0 || overlayId > 0;

            Session.board.SetUnderlayId(x, y, underlayId);
            Session.board.SetTileId(x, y, tileId);
            Session.board.SetOverlayId(x, y, overlayId);
            Session.board.SetPlayable(x, y, true);

            sessionStateHolder.MarkDirty($"Applied selection payload to ({x}, {y}).");
            RevalidateSession();
            NotifyChanged();
        }

        public void ClearSelectedCell()
        {
            if (Session?.board == null || selectionStateHolder == null || !selectionStateHolder.HasSelectedCell)
            {
                return;
            }

            int x = selectionStateHolder.SelectedX;
            int y = selectionStateHolder.SelectedY;
            Session.board.ClearCell(x, y, false);
            SetCellArtId(x, y, 0);
            sessionStateHolder.MarkDirty($"Cleared cell ({x}, {y}).");
            RevalidateSession();
            NotifyChanged();
        }

        public void ClearSelectionPayload()
        {
            if (selectionStateHolder == null)
            {
                return;
            }

            selectionStateHolder.ClearSelectedLayers();
            sessionStateHolder?.SetStatus("Cleared selection payload.", string.Empty);
            NotifyChanged();
        }

        public void ClearSelectedCellTile()
        {
            ClearSelectedCellLayer(BoardLayer.Tile, "Cleared tile");
        }

        public void ClearSelectedCellUnderlay()
        {
            ClearSelectedCellLayer(BoardLayer.Underlay, "Cleared underlay");
        }

        public void ClearSelectedCellOverlay()
        {
            ClearSelectedCellLayer(BoardLayer.Overlay, "Cleared overlay");
        }

        public void AddGoal()
        {
            AddTarget();
        }

        public void AddTarget()
        {
            if (Session == null)
            {
                return;
            }

            List<LevelEditorPaletteEntryData> targetEntries = GetTargetOptions();
            if (targetEntries.Count == 0)
            {
                sessionStateHolder.SetStatus(string.Empty, "No valid target definitions found in tile database.");
                Debug.LogWarning("[LevelEditor] Add Target blocked. No valid target definitions found in tile database.");
                NotifyChanged();
                return;
            }

            int tileId = targetEntries[0].Id;
            _targets.Add(new LevelTargetRequirement(tileId, 1));
            sessionStateHolder.MarkDirty("Added target.");
            RevalidateSession();
            Debug.Log($"[LevelEditor] Added target #{_targets.Count}. tileId={tileId}, count=1.");
            NotifyChanged();
        }

        public bool AddGoalWithTile(int tileId, int requiredCount = 1)
        {
            return AddTargetWithTile(tileId, requiredCount);
        }

        public bool AddTargetWithTile(int tileId, int requiredCount = 1)
        {
            if (Session == null || _targets.Count >= 3 || tileId <= 0)
            {
                Debug.LogWarning($"[LevelEditor] Add Target blocked. tileId={tileId}, currentTargets={_targets.Count}/3, hasSession={Session != null}.");
                return false;
            }

            int sanitizedCount = Mathf.Max(1, requiredCount);
            _targets.Add(new LevelTargetRequirement(tileId, sanitizedCount));
            sessionStateHolder.MarkDirty("Added target.");
            RevalidateSession();
            Debug.Log($"[LevelEditor] Added target #{_targets.Count}. tileId={tileId}, count={sanitizedCount}.");
            NotifyChanged();
            return true;
        }

        public void UpdateGoal(int index, int tileId, int requiredCount)
        {
            UpdateTarget(index, tileId, requiredCount);
        }

        public void UpdateTarget(int index, int tileId, int requiredCount)
        {
            if (Session == null || index < 0 || index >= _targets.Count)
            {
                Debug.LogWarning($"[LevelEditor] Update Target blocked. Invalid index {index}.");
                return;
            }

            int sanitizedCount = Mathf.Max(1, requiredCount);
            _targets[index] = new LevelTargetRequirement(tileId, sanitizedCount);
            sessionStateHolder.MarkDirty($"Updated target #{index + 1}.");
            RevalidateSession();
            Debug.Log($"[LevelEditor] Updated target #{index + 1}. tileId={tileId}, count={sanitizedCount}.");
            NotifyChanged();
        }

        public void UpdateGoalTile(int index, int tileId)
        {
            UpdateTargetTile(index, tileId);
        }

        public void UpdateTargetTile(int index, int tileId)
        {
            if (Session == null || index < 0 || index >= _targets.Count || tileId <= 0)
            {
                Debug.LogWarning($"[LevelEditor] Update Target tile blocked. index={index}, tileId={tileId}.");
                return;
            }

            LevelTargetRequirement existingGoal = _targets[index];
            _targets[index] = new LevelTargetRequirement(tileId, Mathf.Max(1, existingGoal.requiredCount));
            sessionStateHolder.MarkDirty($"Updated target #{index + 1} tile.");
            RevalidateSession();
            Debug.Log($"[LevelEditor] Updated target #{index + 1} tile. tileId={tileId}, count={existingGoal.requiredCount}.");
            NotifyChanged();
        }

        public void UpdateGoalCount(int index, int requiredCount)
        {
            UpdateTargetCount(index, requiredCount);
        }

        public void UpdateTargetCount(int index, int requiredCount)
        {
            if (Session == null || index < 0 || index >= _targets.Count)
            {
                Debug.LogWarning($"[LevelEditor] Update Target count blocked. Invalid index {index}.");
                return;
            }

            LevelTargetRequirement existingGoal = _targets[index];
            int sanitizedCount = Mathf.Max(1, requiredCount);
            _targets[index] = new LevelTargetRequirement(existingGoal.tileId, sanitizedCount);
            sessionStateHolder.MarkDirty($"Updated target #{index + 1} count.");
            RevalidateSession();
            Debug.Log($"[LevelEditor] Updated target #{index + 1} count. tileId={existingGoal.tileId}, count={sanitizedCount}.");
            NotifyChanged();
        }

        public void RemoveGoal(int index)
        {
            RemoveTarget(index);
        }

        public void RemoveTarget(int index)
        {
            if (Session == null || index < 0 || index >= _targets.Count)
            {
                Debug.LogWarning($"[LevelEditor] Remove Target blocked. Invalid index {index}.");
                return;
            }

            LevelTargetRequirement removedTarget = _targets[index];
            _targets.RemoveAt(index);
            sessionStateHolder.MarkDirty($"Removed target #{index + 1}.");
            RevalidateSession();
            Debug.Log($"[LevelEditor] Removed target #{index + 1}. tileId={removedTarget.tileId}, count={removedTarget.requiredCount}. Remaining targets={_targets.Count}.");
            NotifyChanged();
        }

        public void UpdateSpawnTileConfig(int tileId, bool enabled, int weight)
        {
            if (Session == null)
            {
                return;
            }

            bool updated = false;
            if (Session.spawnableTileConfigs == null)
            {
                Session.spawnableTileConfigs = new List<LevelSpawnableTileConfig>();
            }

            for (int i = 0; i < Session.spawnableTileConfigs.Count; i++)
            {
                if (Session.spawnableTileConfigs[i].tileId == tileId)
                {
                    Session.spawnableTileConfigs[i] = new LevelSpawnableTileConfig(tileId, weight, enabled);
                    updated = true;
                    break;
                }
            }

            if (!updated)
            {
                Session.spawnableTileConfigs.Add(new LevelSpawnableTileConfig(tileId, weight, enabled));
            }

            // Keep spawnableTileIds in sync for compatibility
            if (enabled)
            {
                if (!Session.spawnableTileIds.Contains(tileId))
                {
                    Session.spawnableTileIds.Add(tileId);
                }
            }
            else
            {
                Session.spawnableTileIds.Remove(tileId);
            }

            sessionStateHolder?.MarkDirty("Updated spawn tile config.");
            RevalidateSession();
            NotifyChanged();
        }

        public void UpdateBalancingSettings(bool enableDynamic, float bias)
        {
            if (Session == null)
            {
                return;
            }

            Session.enableDynamicBalancing = enableDynamic;
            Session.targetSpawnBias = Mathf.Max(1f, bias);

            sessionStateHolder?.MarkDirty("Updated dynamic balancing settings.");
            RevalidateSession();
            NotifyChanged();
        }

        public Match3LevelDataValidationResult ValidateCurrentLevel()
        {
            if (Session == null)
            {
                return new Match3LevelDataValidationResult();
            }

            LevelSaveData document = LevelEditorDocumentMapper.BuildDocument(Session, TileDatabase, _targets);
            Match3LevelDataValidationResult validation = ValidateDocument(document.levelData);
            sessionStateHolder.SetValidation(validation);
            NotifyChanged();
            return validation;
        }

        public List<LevelEditorPaletteEntryData> GetPaletteEntries(BoardLayer layer)
        {
            return LevelEditorPaletteBuilder.Build(TileDatabase, layer);
        }

        public List<LevelEditorPaletteEntryData> GetPaletteEntries(LevelEditorPaletteSectionType sectionType)
        {
            return LevelEditorPaletteBuilder.BuildSection(TileDatabase, sectionType);
        }

        public List<LevelEditorPaletteEntryData> GetGoalOptions()
        {
            return GetTargetOptions();
        }

        public List<LevelEditorPaletteEntryData> GetCellArtEntries()
        {
            return LevelEditorCellArtPaletteBuilder.Build(CellArtCatalog);
        }

        public List<LevelEditorPaletteEntryData> GetTargetOptions()
        {
            return LevelEditorPaletteBuilder.BuildTargetEntries(TileDatabase);
        }

        public List<LevelEditorPaletteEntryData> GetTargetOptions(LevelEditorPaletteSectionType sectionType)
        {
            return LevelEditorPaletteBuilder.BuildTargetEntries(TileDatabase, sectionType);
        }

        public IReadOnlyList<string> GetAvailableLevelIds(string filter = null)
        {
            return _levelCatalogProvider.GetLevelIds(filter);
        }

        public string GetContentDisplayName(BoardLayer layer, int contentId)
        {
            if (contentId <= 0 || TileDatabase == null)
            {
                return "None";
            }

            BoardContentDefinitionSO definition = null;
            switch (layer)
            {
                case BoardLayer.Underlay:
                    definition = TileDatabase.GetUnderlayDefinition(contentId);
                    break;
                case BoardLayer.Tile:
                    definition = TileDatabase.GetTileDefinition(contentId);
                    break;
                case BoardLayer.Overlay:
                    definition = TileDatabase.GetOverlayDefinition(contentId);
                    break;
            }

            return definition != null ? definition.name : "None";
        }

        public string GetCellArtDisplayName(int contentId)
        {
            if (contentId <= 0)
            {
                return "None";
            }

            LevelEditorCellArtDefinition definition = CellArtCatalog != null ? CellArtCatalog.GetEntry(contentId) : null;
            return definition != null && !string.IsNullOrWhiteSpace(definition.Label)
                ? definition.Label
                : $"Cell Art {contentId}";
        }

        public int GetCellArtId(int x, int y)
        {
            if (Session?.board == null ||
                Session.cellArtLayout == null ||
                !Session.board.IsInBounds(x, y))
            {
                return 0;
            }

            int index = Session.board.ToIndex(x, y);
            return index >= 0 && index < Session.cellArtLayout.Length
                ? Session.cellArtLayout[index]
                : 0;
        }

        public void PaintCellArt(int x, int y)
        {
            if (Session?.board == null || selectionStateHolder == null || !Session.board.IsInBounds(x, y))
            {
                return;
            }

            selectionStateHolder.SetSelectedCoordinate(x, y);
            int cellArtId = selectionStateHolder.SelectedCellArtId;
            if (cellArtId <= 0)
            {
                sessionStateHolder.SetStatus($"Selected cell ({x}, {y}).", "No Cell Art content selected.");
                NotifyChanged();
                return;
            }

            SetCellArtId(x, y, cellArtId);
            Session.board.SetPlayable(x, y, true);
            sessionStateHolder.MarkDirty($"Painted Cell Art at ({x}, {y}).");
            RevalidateSession();
            NotifyChanged();
        }

        public void EraseCellArt(int x, int y)
        {
            if (Session?.board == null || selectionStateHolder == null || !Session.board.IsInBounds(x, y))
            {
                return;
            }

            selectionStateHolder.SetSelectedCoordinate(x, y);
            Session.board.ClearCell(x, y, false);
            SetCellArtId(x, y, 0);
            sessionStateHolder.MarkDirty($"Erased Cell at ({x}, {y}).");
            RevalidateSession();
            NotifyChanged();
        }

        private Match3LevelDataValidationResult ValidateDocument(Match3LevelData levelData)
        {
            return Match3LevelDataValidator.Validate(levelData, TileDatabase);
        }

        private bool LevelExists(string levelId)
        {
            IReadOnlyList<string> levelIds = _levelCatalogProvider.GetLevelIds();
            for (int i = 0; i < levelIds.Count; i++)
            {
                if (string.Equals(levelIds[i], levelId, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private void ClearSelectedCellLayer(BoardLayer layer, string actionLabel)
        {
            if (Session?.board == null || selectionStateHolder == null || !selectionStateHolder.HasSelectedCell)
            {
                return;
            }

            int x = selectionStateHolder.SelectedX;
            int y = selectionStateHolder.SelectedY;
            LevelEditorCellState cell = Session.board.GetCell(x, y);

            int underlayId = cell.UnderlayId;
            int tileId = cell.TileId;
            int overlayId = cell.OverlayId;

            switch (layer)
            {
                case BoardLayer.Underlay:
                    underlayId = 0;
                    break;
                case BoardLayer.Tile:
                    tileId = 0;
                    break;
                case BoardLayer.Overlay:
                    overlayId = 0;
                    break;
            }

            ApplyCellContent(x, y, underlayId, tileId, overlayId);
            sessionStateHolder.MarkDirty($"{actionLabel} at ({x}, {y}).");
            RevalidateSession();
            NotifyChanged();
        }

        private void ApplyCellLayerContent(int x, int y, BoardLayer layer, int contentId)
        {
            LevelEditorCellState cell = Session.board.GetCell(x, y);
            int underlayId = cell.UnderlayId;
            int tileId = cell.TileId;
            int overlayId = cell.OverlayId;

            switch (layer)
            {
                case BoardLayer.Underlay:
                    underlayId = contentId;
                    break;
                case BoardLayer.Tile:
                    tileId = contentId;
                    break;
                case BoardLayer.Overlay:
                    overlayId = contentId;
                    break;
            }

            ApplyCellContent(x, y, underlayId, tileId, overlayId);
        }

        private void ApplyCellContent(int x, int y, int underlayId, int tileId, int overlayId)
        {
            bool wasPlayable = Session.board.GetCell(x, y).Playable;
            Session.board.SetUnderlayId(x, y, underlayId);
            Session.board.SetTileId(x, y, tileId);
            Session.board.SetOverlayId(x, y, overlayId);
            
            int cellArtId = GetCellArtId(x, y);
            bool hasContent = underlayId > 0 || tileId > 0 || overlayId > 0 || cellArtId > 0;
            Session.board.SetPlayable(x, y, wasPlayable || hasContent);
        }

        private void SetCellArtId(int x, int y, int cellArtId)
        {
            if (Session?.board == null || Session.cellArtLayout == null || !Session.board.IsInBounds(x, y))
            {
                return;
            }

            int index = Session.board.ToIndex(x, y);
            if (index < 0 || index >= Session.cellArtLayout.Length)
            {
                return;
            }

            Session.cellArtLayout[index] = Mathf.Max(0, cellArtId);
        }

        private void RevalidateSession()
        {
            if (Session == null || TileDatabase == null)
            {
                return;
            }

            LevelSaveData document = LevelEditorDocumentMapper.BuildDocument(Session, TileDatabase, _targets);
            sessionStateHolder.SetValidation(ValidateDocument(document.levelData));
        }

        private List<int> BuildDefaultSpawnables()
        {
            List<int> spawnables = new List<int>();
            if (TileDatabase == null)
            {
                return spawnables;
            }

            IReadOnlyList<BoardContentDefinitionSO> entries = TileDatabase.Tiles;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] is TileDefinitionSO tileDefinition &&
                    tileDefinition.TileKind == TileKind.Normal &&
                    tileDefinition.CanSpawnOnRefill &&
                    tileDefinition.SpawnWeight > 0)
                {
                    spawnables.Add(tileDefinition.TileId);
                }
            }

            return spawnables;
        }

        private LevelEditorCellArtCatalogSO ResolveCellArtCatalog()
        {
            if (cellArtCatalog == null)
            {
                cellArtCatalog = Resources.Load<LevelEditorCellArtCatalogSO>(DefaultCellArtCatalogResourcePath);
            }

            return cellArtCatalog;
        }

        private void NotifyChanged()
        {
            Changed?.Invoke();
        }
    }
}
