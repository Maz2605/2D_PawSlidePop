using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Move;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Entities;
using _PawSlidePopGame._Scripts.Feature.Match3.Presentation;
using _PawSlidePopGame._Scripts.Feature.Match3.View.Factory;
using _PawSlidePopGame._Scripts.Feature.Match3.Boosters;
using _PawSlidePopGame._Scripts.Core.Audio;
using _PawSlidePopGame._Scripts.Core.Vibration;
using _PawSlidePopGame.Scripts.DesignPattern.ObjectPooling;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.View
{
    public class Match3BoardView : MonoBehaviour
    {
        public static Match3BoardView Instance { get; private set; }



        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        [Header("Authoring")]
        [SerializeField] private Match3CellView cellPrefab;
        [SerializeField] private Transform cellRoot;
        [SerializeField] private Transform tileRoot;

        [Header("Layout")]
        [SerializeField] private float cellStepX = 1f;
        [SerializeField] private float cellStepY = 1f;
        [SerializeField] private Vector2 boardOffset;

        [Header("FX Timing")]
        [SerializeField] private float moveDuration = 0.18f;
        [SerializeField] private float rollbackDuration = 0.14f;
        [SerializeField] private float gravityDurationPerCell = 0.09f;
        [SerializeField] private float refillDurationPerCell = 0.1f;
        [SerializeField] private float phaseGap = 0.04f;
        [SerializeField] private int spawnableTilePrewarmReserve = 2;

        [Header("Intro/Outro Timing")]
        [SerializeField] private float introDuration = 0.9f;
        [SerializeField] private float loseOutroDuration = 0.8f;
        [SerializeField] private float loseOutroDelayBeforePopup = 1.8f;

        public float LoseOutroDelayBeforePopup => loseOutroDelayBeforePopup;

        private readonly Dictionary<CellModel, Match3CellView> _cellViews = new Dictionary<CellModel, Match3CellView>();
        private readonly Dictionary<int, Match3TileView> _tileViews = new Dictionary<int, Match3TileView>();
        private readonly HashSet<int> _previewedTileIds = new HashSet<int>();
        private readonly Match3CellViewFactory _cellViewFactory = new Match3CellViewFactory();
        private readonly Match3TileViewFactory _tileViewFactory = new Match3TileViewFactory();
        private BoardModel _board;
        private Match3LevelData _levelData;
        private Match3TileDatabaseSO _tileDatabase;
        private bool _isIdleEnabled = true;
        private _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Model.LevelEditorCellArtCatalogSO _cellArtCatalog;

        private Vector3 _initialTileRootPos;
        private Vector3 _initialCellRootPos;

        public event Action<TileActivateOp> OnTileActivatePlaybackStarted;
        public event Action<TileClearOp, Vector3> OnTileClearPlaybackStarted;
        public event Action<SpecialCreateOp> OnSpecialCreatePlaybackStarted;
        public event Action<ScoreGainOp> OnScoreGainPlaybackStarted;

        private void Awake()
        {
            Instance = this;
            if (tileRoot != null) _initialTileRootPos = tileRoot.localPosition;
            if (cellRoot != null) _initialCellRootPos = cellRoot.localPosition;
        }

        public void ShakeBoard(float duration, float strength)
        {
            if (tileRoot != null)
            {
                tileRoot.DOComplete();
                tileRoot.localPosition = _initialTileRootPos;
                tileRoot.DOShakePosition(duration, strength, 14, 90f, false, true)
                    .OnComplete(() => tileRoot.localPosition = _initialTileRootPos);
            }
            if (cellRoot != null)
            {
                cellRoot.DOComplete();
                cellRoot.localPosition = _initialCellRootPos;
                cellRoot.DOShakePosition(duration, strength * 0.7f, 14, 90f, false, true)
                    .OnComplete(() => cellRoot.localPosition = _initialCellRootPos);
            }
        }

        public void Bind(BoardModel board, Match3LevelData levelData, Match3TileDatabaseSO tileDatabase)
        {
            _board = board;
            _levelData = levelData;
            _tileDatabase = tileDatabase;
            if (_cellArtCatalog == null)
            {
                _cellArtCatalog = Resources.Load<_PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Model.LevelEditorCellArtCatalogSO>("LevelEditor/LevelEditorCellArtCatalog");
            }
            Rebuild();
        }

        public void ClearBoardVisuals()
        {
            ClearPreview();
            ClearCells();
            ClearTileViews();
            _board = null;
            _levelData = null;
            _tileDatabase = null;
        }

        public CellModel GetCellAtScreenPosition(Vector2 screenPosition)
        {
            if (Camera.main == null)
            {
                return null;
            }

            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(screenPosition);
            worldPosition.z = 0f;
            return GetCellAtWorldPosition(worldPosition);
        }

        public CellModel GetCellAtWorldPosition(Vector3 worldPosition)
        {
            if (_board == null)
            {
                return null;
            }

            Vector3 localPosition = transform.InverseTransformPoint(worldPosition);

            float offsetX = -((_board.Width - 1) * cellStepX) * 0.5f + boardOffset.x;
            float offsetY = ((_board.Height - 1) * cellStepY) * 0.5f + boardOffset.y;

            float left = offsetX - (cellStepX * 0.5f);
            float top = offsetY + (cellStepY * 0.5f);

            float relativeX = localPosition.x - left;
            float relativeY = top - localPosition.y;

            if (relativeX < 0f || relativeY < 0f)
            {
                return null;
            }

            int x = Mathf.FloorToInt(relativeX / cellStepX);
            int y = Mathf.FloorToInt(relativeY / cellStepY);

            if (x < 0 || x >= _board.Width || y < 0 || y >= _board.Height)
            {
                return null;
            }

            CellModel cell = _board.GetCell(x, y);
            return cell != null && cell.IsPlayable ? cell : null;
        }

        public Match3TileView GetTileView(int tileInstanceId)
        {
            if (_tileViews.TryGetValue(tileInstanceId, out Match3TileView tileView))
            {
                return tileView;
            }
            return null;
        }

        public void SetIdleEnabled(bool isEnabled)
        {
            _isIdleEnabled = isEnabled;

            foreach (KeyValuePair<int, Match3TileView> pair in _tileViews)
            {
                if (pair.Value != null)
                {
                    pair.Value.SetIdleEnabled(isEnabled);
                }
            }
        }

        public void ShowPressPreview(CellModel cell)
        {
            ClearPreview();

            if (cell?.TopTile == null)
            {
                return;
            }

            if (_tileViews.TryGetValue(cell.TopTile.InstanceId, out Match3TileView tileView) && tileView != null)
            {
                tileView.SetShadowState(TileShadowState.Active);
                tileView.SetSelectedScale(true);
                _previewedTileIds.Add(cell.TopTile.InstanceId);
            }
        }

        public void ShowLinePreview(BoardLinePreview preview)
        {
            if (preview.FocusedCell == null)
            {
                ClearPreview();
                return;
            }

            ClearPreview();
            ShowPressPreview(preview.FocusedCell);

            if (_board == null || !preview.HasLockedLine || !preview.Axis.HasValue)
            {
                return;
            }

            List<CellModel> cells = _board.GetPlayableCellsForMove(preview.Axis.Value, preview.LineIndex);
            for (int i = 0; i < cells.Count; i++)
            {
                TileModel tile = cells[i].TopTile;
                if (tile == null || !_tileViews.TryGetValue(tile.InstanceId, out Match3TileView tileView) || tileView == null)
                {
                    continue;
                }

                TileShadowState state = preview.FocusedCell.TopTile != null &&
                                        preview.FocusedCell.TopTile.InstanceId == tile.InstanceId
                    ? TileShadowState.Active
                    : TileShadowState.Preview;
                tileView.SetShadowState(state);
                tileView.SetSelectedScale(state == TileShadowState.Active);
                _previewedTileIds.Add(tile.InstanceId);
            }
        }

        public void ClearPreview()
        {
            DOTween.Kill("HintSequence");

            if (_previewedTileIds.Count == 0)
            {
                return;
            }

            foreach (int tileInstanceId in _previewedTileIds)
            {
                if (_tileViews.TryGetValue(tileInstanceId, out Match3TileView tileView) && tileView != null)
                {
                    // Kill hint tweens, reset visual states, restore bright state
                    DOTween.Kill(tileView.transform, false);
                    tileView.ResetVisualState();
                    tileView.ApplyDimmedState(false);

                    if (_board != null && tileView.Tile != null)
                    {
                        foreach (CellModel cell in _board.GetAllCells())
                        {
                            if (cell != null && cell.IsPlayable && (cell.Tile == tileView.Tile || cell.Overlay == tileView.Tile))
                            {
                                TileStackLayer layer = cell.Tile == tileView.Tile ? TileStackLayer.Base : TileStackLayer.Overlay;
                                tileView.SnapToLocalPosition(GetTileLocalPosition(cell.X, cell.Y, layer));
                                break;
                            }
                        }
                    }
                }
            }

            _previewedTileIds.Clear();
        }

        public void ShowRowColumnHighlight(CellModel cell)
        {
            ClearPreview();

            if (cell == null || _board == null)
            {
                return;
            }

            HashSet<int> highlightTileIds = new HashSet<int>();

            List<CellModel> rowCells = _board.GetPlayableCellsInRow(cell.Y);
            for (int i = 0; i < rowCells.Count; i++)
            {
                TileModel tile = rowCells[i].Tile;
                if (tile != null)
                {
                    highlightTileIds.Add(tile.InstanceId);
                }
                TileModel overlay = rowCells[i].Overlay;
                if (overlay != null)
                {
                    highlightTileIds.Add(overlay.InstanceId);
                }
            }

            List<CellModel> colCells = _board.GetPlayableCellsInColumn(cell.X);
            for (int i = 0; i < colCells.Count; i++)
            {
                TileModel tile = colCells[i].Tile;
                if (tile != null)
                {
                    highlightTileIds.Add(tile.InstanceId);
                }
                TileModel overlay = colCells[i].Overlay;
                if (overlay != null)
                {
                    highlightTileIds.Add(overlay.InstanceId);
                }
            }

            // Prefer highlighting the base tile as the focused tile if it exists, so only it scales up and gets active shadow
            int focusedTileId = -1;
            if (cell.Tile != null)
            {
                focusedTileId = cell.Tile.InstanceId;
            }
            else if (cell.Overlay != null)
            {
                focusedTileId = cell.Overlay.InstanceId;
            }

            if (focusedTileId != -1)
            {
                highlightTileIds.Add(focusedTileId);
            }
            if (cell.Overlay != null)
            {
                highlightTileIds.Add(cell.Overlay.InstanceId);
            }
            if (cell.Tile != null)
            {
                highlightTileIds.Add(cell.Tile.InstanceId);
            }

            foreach (KeyValuePair<int, Match3TileView> pair in _tileViews)
            {
                Match3TileView tileView = pair.Value;
                if (tileView == null)
                {
                    continue;
                }

                int tileId = pair.Key;
                bool isHighlighted = highlightTileIds.Contains(tileId);

                if (isHighlighted)
                {
                    bool isFocused = tileId == focusedTileId;
                    tileView.SetShadowState(isFocused ? TileShadowState.Active : TileShadowState.Preview);
                    tileView.SetSelectedScale(isFocused);
                    tileView.ApplyDimmedState(false);
                }
                else
                {
                    tileView.SetShadowState(TileShadowState.Off);
                    tileView.SetSelectedScale(false);
                    tileView.ApplyDimmedState(true);
                }

                _previewedTileIds.Add(tileId);
            }
        }

        private bool IsLineMoveable(MoveAxis axis, int lineIndex)
        {
            if (_board == null)
            {
                return false;
            }

            List<CellModel> affectedCells = _board.GetPlayableCellsForMove(axis, lineIndex);
            if (affectedCells.Count <= 1)
            {
                return false;
            }

            bool hasAnyMovableState = false;
            for (int i = 0; i < affectedCells.Count; i++)
            {
                CellModel cell = affectedCells[i];
                if (cell == null)
                {
                    continue;
                }

                if (cell.LocksLine(axis))
                {
                    return false;
                }

                if (cell.Tile != null)
                {
                    hasAnyMovableState = true;
                    if (!cell.Tile.CanBeMoved())
                    {
                        return false;
                    }
                }

                if (cell.Overlay != null)
                {
                    hasAnyMovableState = true;
                    if (!cell.Overlay.CanBeMoved())
                    {
                        return false;
                    }
                }
            }

            return hasAnyMovableState;
        }

        public void HighlightHint(BoardMoveRequest hint)
        {
            ClearPreview();
            if (_board == null)
            {
                return;
            }

            HashSet<int> hintTileIds = new HashSet<int>();

            if (hint.LineIndex >= 0)
            {
                List<CellModel> cells = _board.GetPlayableCellsForMove(hint.Axis, hint.LineIndex);
                for (int i = 0; i < cells.Count; i++)
                {
                    TileModel tile = cells[i].Tile; // Chỉ lấy Base Tile
                    if (tile != null)
                    {
                        hintTileIds.Add(tile.InstanceId);
                    }
                }
            }
            else if (hint.HasSource)
            {
                CellModel cell = _board.GetCell(hint.SourceX, hint.SourceY);
                if (cell?.Tile != null)
                {
                    hintTileIds.Add(cell.Tile.InstanceId);
                }
            }

            // Dim all tiles not in the hint, and highlight/scale up the hint tiles!
            foreach (KeyValuePair<int, Match3TileView> pair in _tileViews)
            {
                Match3TileView tileView = pair.Value;
                if (tileView == null)
                {
                    continue;
                }

                int tileId = pair.Key;
                bool isHint = hintTileIds.Contains(tileId);

                if (isHint)
                {
                    tileView.SetShadowState(TileShadowState.Active);
                    tileView.SetSelectedScale(true);
                    tileView.ApplyDimmedState(false);
                }
                else
                {
                    tileView.SetShadowState(TileShadowState.Off);
                    tileView.SetSelectedScale(false);
                    tileView.ApplyDimmedState(true);
                }

                _previewedTileIds.Add(tileId);
            }

            // Play nudge/wobble animation loop for the hint
            if (hint.LineIndex >= 0)
            {
                List<CellModel> cells = _board.GetPlayableCellsForMove(hint.Axis, hint.LineIndex);
                float nudgeAmount = 0.3f; // Slightly larger for better prominence
                float duration = 0.22f; // Snappier
                Vector3 directionOffset = Vector3.zero;

                if (hint.Axis == MoveAxis.Row)
                {
                    float dir = hint.Direction == LineSlideDirection.Right ? 1f : -1f;
                    directionOffset = new Vector3(dir * cellStepX * nudgeAmount, 0f, 0f);
                }
                else
                {
                    float dir = hint.Direction == LineSlideDirection.Up ? 1f : -1f;
                    directionOffset = new Vector3(0f, dir * cellStepY * nudgeAmount, 0f);
                }

                for (int i = 0; i < cells.Count; i++)
                {
                    TileModel tile = cells[i].Tile; // Chỉ lấy Base Tile
                    if (tile != null && _tileViews.TryGetValue(tile.InstanceId, out Match3TileView tileView) && tileView != null)
                    {
                        Vector3 normalPos = GetTileLocalPosition(cells[i].X, cells[i].Y, TileStackLayer.Base); // Luôn ở lớp Base
                        
                        // Repeat nudge periodically: move to offset, move back, wait 1.5s
                        Sequence seq = DOTween.Sequence()
                            .Append(tileView.transform.DOLocalMove(normalPos + directionOffset, duration).SetEase(Ease.OutQuad))
                            .Append(tileView.transform.DOLocalMove(normalPos, duration).SetEase(Ease.InQuad))
                            .AppendInterval(1.5f)
                            .SetLoops(-1)
                            .SetId("HintSequence")
                            .SetLink(tileView.gameObject);
                    }
                }
            }
            else if (hint.HasSource)
            {
                CellModel cell = _board.GetCell(hint.SourceX, hint.SourceY);
                if (cell?.Tile != null &&
                    _tileViews.TryGetValue(cell.Tile.InstanceId, out Match3TileView tileView) &&
                    tileView != null)
                {
                    // Repeat wobble periodically: punch scale, wait 1.5s
                    Sequence seq = DOTween.Sequence()
                        .Append(tileView.transform.DOPunchScale(Vector3.one * 0.20f, 0.45f, 10, 1f))
                        .AppendInterval(1.5f)
                        .SetLoops(-1)
                        .SetId("HintSequence")
                        .SetLink(tileView.gameObject);
                }
            }
        }

        public void ShowPlacementCandidates(IReadOnlyList<CellModel> cells)
        {
            ClearPreview();
            HighlightCellTiles(cells, TileShadowState.Preview);
        }

        public void ShowChargedComboSelection(CellModel sourceCell, IReadOnlyList<CellModel> partnerCells)
        {
            ClearPreview();
            HighlightCellTiles(partnerCells, TileShadowState.Preview);

            if (sourceCell?.Tile != null &&
                _tileViews.TryGetValue(sourceCell.Tile.InstanceId, out Match3TileView tileView) &&
                tileView != null)
            {
                tileView.SetShadowState(TileShadowState.Active);
                tileView.SetSelectedScale(true);
                _previewedTileIds.Add(sourceCell.Tile.InstanceId);
            }
        }

        public IEnumerator PlayMoveExecution(BoardMoveExecutionResult executionResult, float speedMultiplier = 1f)
        {
            if (executionResult == null || executionResult.PresentationTrace == null)
            {
                yield break;
            }

            // Play Booster Intro Visuals
            if (executionResult.Kind == BoardExecutionKind.Booster && executionResult.BoosterType != BoosterType.None)
            {
                yield return StartCoroutine(PlayBoosterIntroVisuals(executionResult));
            }

            BoardPresentationTrace trace = executionResult.PresentationTrace;

            if (trace.MoveAttempt != null && trace.MoveAttempt.TravelOps.Count > 0)
            {
                yield return StartCoroutine(PlayLineTravel(trace.MoveAttempt.TravelOps, trace.MoveAttempt.Axis, trace.MoveAttempt.Direction, moveDuration / speedMultiplier));
            }

            if (!executionResult.IsAccepted)
            {
                if (trace.Rollback != null && trace.Rollback.TravelOps.Count > 0)
                {
                    yield return StartCoroutine(PlayLineTravel(trace.Rollback.TravelOps, trace.Rollback.Axis, trace.Rollback.Direction, rollbackDuration / speedMultiplier));
                }

                yield break;
            }

            for (int i = 0; i < trace.Cascades.Count; i++)
            {
                CascadeTrace cascadeTrace = trace.Cascades[i];
                yield return StartCoroutine(PlayClearPhase(cascadeTrace.ClearPhase, speedMultiplier));
                yield return StartCoroutine(PlayGravityPhase(cascadeTrace.GravityPhase, speedMultiplier));
                yield return StartCoroutine(PlayRefillPhase(cascadeTrace.RefillPhase, speedMultiplier));
            }
        }

        private IEnumerator PlayBoosterIntroVisuals(BoardMoveExecutionResult executionResult)
        {
            switch (executionResult.BoosterType)
            {
                case BoosterType.Hammer:
                    yield return StartCoroutine(PlayHammerBoosterVisual(executionResult));
                    break;
                case BoosterType.LineClear:
                    yield return StartCoroutine(PlayLineClearBoosterVisual(executionResult));
                    break;
                case BoosterType.RainbowPlacement:
                    yield return StartCoroutine(PlayRainbowPlacementBoosterVisual(executionResult));
                    break;
                case BoosterType.Shuffle:
                    yield return StartCoroutine(PlayShuffleBoosterVisual(executionResult));
                    break;
            }
        }

        private IEnumerator PlayHammerBoosterVisual(BoardMoveExecutionResult executionResult)
        {
            BoardPresentationTrace trace = executionResult.PresentationTrace;
            BoardCellPosition targetCellPos = default;
            bool hasTargetCell = false;

            if (trace.Cascades.Count > 0 && trace.Cascades[0].ClearPhase != null)
            {
                var clearPhase = trace.Cascades[0].ClearPhase;
                if (clearPhase.ClearOps.Count > 0)
                {
                    targetCellPos = clearPhase.ClearOps[0].Cell;
                    hasTargetCell = true;
                }
                else if (clearPhase.DamageOps.Count > 0)
                {
                    targetCellPos = clearPhase.DamageOps[0].Cell;
                    hasTargetCell = true;
                }
            }

            if (!hasTargetCell)
            {
                yield break;
            }

            Vector3 targetWorldPos = GetCellWorldPosition(targetCellPos.X, targetCellPos.Y);

            // Punch scale target tile directly
            CellModel cell = _board != null ? _board.GetCell(targetCellPos.X, targetCellPos.Y) : null;
            if (cell != null)
            {
                TileModel tile = cell.TopTile;
                if (tile != null && _tileViews.TryGetValue(tile.InstanceId, out Match3TileView tileView) && tileView != null)
                {
                    tileView.transform.DOPunchScale(Vector3.one * 0.22f, 0.2f, 10, 1f).SetLink(tileView.gameObject);
                }
            }

            if (Camera.main != null)
            {
                Camera.main.transform.DOComplete();
                Camera.main.transform.DOShakePosition(0.15f, 0.15f, 20, 90f, false, true).SetLink(Camera.main.gameObject);
            }

            if (VibrationManager.Instance != null)
            {
                VibrationManager.Instance.PlayMediumImpact(true);
            }

            if (AudioController.Instance != null)
            {
                AudioController.Instance.PlayTileDrop();
            }

            SpawnShockwaveRing(1.5f, 0.25f, new Color(1f, 1f, 1.0f, 0.6f), targetWorldPos);
            SpawnDebrisParticles(8, 0.3f, new Color(1f, 0.9f, 0.4f, 1f), null, targetWorldPos, 1f);

            yield return new WaitForSeconds(0.1f);
        }

        private IEnumerator PlayLineClearBoosterVisual(BoardMoveExecutionResult executionResult)
        {
            BoardPresentationTrace trace = executionResult.PresentationTrace;
            if (trace.MoveAttempt == null)
            {
                yield break;
            }

            MoveAxis axis = trace.MoveAttempt.Axis;
            int lineIndex = trace.MoveAttempt.LineIndex;

            if (_board == null)
            {
                yield break;
            }

            Vector3 centerPos = Vector3.zero;
            Vector3 lineScale = Vector3.one;

            if (axis == MoveAxis.Row)
            {
                Vector3 leftCellPos = GetCellWorldPosition(0, lineIndex);
                Vector3 rightCellPos = GetCellWorldPosition(_board.Width - 1, lineIndex);
                centerPos = Vector3.Lerp(leftCellPos, rightCellPos, 0.5f);
                float lineLength = Vector3.Distance(leftCellPos, rightCellPos) + cellStepX;
                lineScale = new Vector3(lineLength, 0.15f, 1f);
            }
            else
            {
                Vector3 bottomCellPos = GetCellWorldPosition(lineIndex, _board.Height - 1);
                Vector3 topCellPos = GetCellWorldPosition(lineIndex, 0);
                centerPos = Vector3.Lerp(bottomCellPos, topCellPos, 0.5f);
                float lineLength = Vector3.Distance(bottomCellPos, topCellPos) + cellStepY;
                lineScale = new Vector3(0.15f, lineLength, 1f);
            }

            GameObject laserObj = new GameObject("LineClearLaserVisual");
            laserObj.transform.position = centerPos;
            laserObj.transform.localScale = new Vector3(lineScale.x * 0.1f, lineScale.y * 0.1f, 1f);

            SpriteRenderer sr = laserObj.AddComponent<SpriteRenderer>();
            sr.sprite = Match3TileView.GetSquareSprite();
            sr.color = new Color(0.2f, 0.8f, 1f, 0.9f);
            sr.sortingOrder = 120;

            if (Camera.main != null)
            {
                Camera.main.transform.DOComplete();
                Camera.main.transform.DOShakePosition(0.15f, 0.15f, 20, 90f, false, true).SetLink(Camera.main.gameObject);
            }

            if (VibrationManager.Instance != null)
            {
                VibrationManager.Instance.PlayMediumImpact(true);
            }

            if (AudioController.Instance != null)
            {
                AudioController.Instance.PlayTileSlide();
            }

            bool animationDone = false;
            Sequence seq = DOTween.Sequence().SetLink(laserObj);
            seq.Append(laserObj.transform.DOScale(lineScale, 0.08f).SetEase(Ease.OutQuad))
               .Append(laserObj.transform.DOScale(new Vector3(lineScale.x, 0f, 1f), 0.15f).SetEase(Ease.InQuad))
               .Join(sr.DOFade(0f, 0.15f))
               .OnComplete(() => animationDone = true);

            int numCells = (axis == MoveAxis.Row) ? _board.Width : _board.Height;
            for (int i = 0; i < numCells; i++)
            {
                int x = (axis == MoveAxis.Row) ? i : lineIndex;
                int y = (axis == MoveAxis.Row) ? lineIndex : i;
                Vector3 cellWorldPos = GetCellWorldPosition(x, y);
                SpawnDebrisParticles(2, 0.22f, new Color(0.3f, 0.8f, 1.0f, 1f), null, cellWorldPos, 0.7f);
            }

            while (!animationDone)
            {
                yield return null;
            }

            Destroy(laserObj);
        }

        private IEnumerator PlayRainbowPlacementBoosterVisual(BoardMoveExecutionResult executionResult)
        {
            BoardPresentationTrace trace = executionResult.PresentationTrace;
            BoardCellPosition targetCellPos = default;
            bool hasTargetCell = false;

            if (trace.Cascades.Count > 0 && trace.Cascades[0].ClearPhase != null)
            {
                var clearPhase = trace.Cascades[0].ClearPhase;
                if (clearPhase.SpecialCreateOps.Count > 0)
                {
                    targetCellPos = clearPhase.SpecialCreateOps[0].Cell;
                    hasTargetCell = true;
                }
            }

            if (!hasTargetCell)
            {
                yield break;
            }

            Vector3 targetWorldPos = GetCellWorldPosition(targetCellPos.X, targetCellPos.Y);

            GameObject circleObj = new GameObject("RainbowCircleVisual");
            circleObj.transform.position = targetWorldPos;
            circleObj.transform.localScale = Vector3.zero;

            SpriteRenderer sr = circleObj.AddComponent<SpriteRenderer>();
            sr.sprite = Match3TileView.GetRingSprite();
            sr.color = new Color(0.9f, 0.2f, 0.9f, 0.9f);
            sr.sortingOrder = 120;

            if (Camera.main != null)
            {
                Camera.main.transform.DOComplete();
                Camera.main.transform.DOShakePosition(0.12f, 0.12f, 20, 90f, false, true).SetLink(Camera.main.gameObject);
            }

            if (VibrationManager.Instance != null)
            {
                VibrationManager.Instance.PlayLightImpact(true);
            }

            if (AudioController.Instance != null)
            {
                AudioController.Instance.PlayTileDrop();
            }

            bool animationDone = false;
            Sequence seq = DOTween.Sequence().SetLink(circleObj);
            seq.Append(circleObj.transform.DOScale(Vector3.one * 1.8f, 0.18f).SetEase(Ease.OutQuad))
               .Join(sr.DOFade(0f, 0.18f).SetEase(Ease.InQuad))
               .OnComplete(() => animationDone = true);

            Color[] colors = new Color[] { Color.red, Color.yellow, Color.green, Color.cyan, Color.magenta };
            for (int i = 0; i < 10; i++)
            {
                SpawnDebrisParticles(1, 0.35f, colors[i % colors.Length], null, targetWorldPos, 1.2f);
            }

            while (!animationDone)
            {
                yield return null;
            }

            Destroy(circleObj);
        }

        private IEnumerator PlayShuffleBoosterVisual(BoardMoveExecutionResult executionResult)
        {
            if (Camera.main != null)
            {
                Camera.main.transform.DOComplete();
                Camera.main.transform.DOShakePosition(0.3f, 0.18f, 25, 90f, false, true).SetLink(Camera.main.gameObject);
            }
            if (VibrationManager.Instance != null)
            {
                VibrationManager.Instance.PlayLightImpact(true);
            }

            yield return new WaitForSeconds(0.1f);
        }

        private void SpawnDebrisParticles(int count, float duration, Color color, Sprite customSprite = null, Vector3? customWorldPos = null, float spreadMultiplier = 1f)
        {
            Vector3 spawnPos = customWorldPos.HasValue ? customWorldPos.Value : transform.position;
            Sprite spriteToUse = customSprite != null ? customSprite : Match3TileView.GetCircleSprite();

            for (int i = 0; i < count; i++)
            {
                GameObject p = new GameObject("DebrisParticle");
                p.transform.position = spawnPos;
                SpriteRenderer sr = p.AddComponent<SpriteRenderer>();

                if (i % 2 == 0)
                {
                    sr.sprite = spriteToUse;
                    sr.color = Color.white;
                }
                else
                {
                    sr.sprite = Match3TileView.GetCircleSprite();
                    sr.color = new Color(color.r * 1.1f, color.g * 1.1f, color.b * 1.1f, color.a * 0.9f);
                }
                sr.sortingOrder = 105;

                float targetShardScale = UnityEngine.Random.Range(0.18f, 0.26f);
                p.transform.localScale = Vector3.one * targetShardScale;

                float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                float dist = UnityEngine.Random.Range(0.6f, 1.4f) * spreadMultiplier;
                Vector3 targetPos = spawnPos + new Vector3(Mathf.Cos(angle) * dist, Mathf.Sin(angle) * dist, 0);

                p.transform.DOMove(targetPos, duration).SetEase(Ease.OutQuad);
                p.transform.DORotate(new Vector3(0, 0, UnityEngine.Random.Range(-270f, 270f)), duration);
                p.transform.DOScale(Vector3.zero, duration).SetEase(Ease.InQuad);
                sr.DOFade(0f, duration).SetEase(Ease.InQuad).OnComplete(() => Destroy(p));
            }
        }

        private void SpawnShockwaveRing(float maxScale, float duration, Color color, Vector3? customWorldPos = null)
        {
            Vector3 spawnPos = customWorldPos.HasValue ? customWorldPos.Value : transform.position;
            GameObject ring = new GameObject("ShockwaveRing");
            ring.transform.position = spawnPos;
            ring.transform.localScale = Vector3.one * 0.1f;
            
            SpriteRenderer sr = ring.AddComponent<SpriteRenderer>();
            sr.sprite = Match3TileView.GetRingSprite();
            sr.color = color;
            sr.sortingOrder = 100;

            ring.transform.DOScale(Vector3.one * maxScale, duration).SetEase(Ease.OutQuad);
            sr.DOFade(0f, duration).SetEase(Ease.OutQuad).OnComplete(() => Destroy(ring));
        }

        public void SyncToBoardState()
        {
            if (_board == null)
            {
                return;
            }

            HashSet<int> aliveTileIds = new HashSet<int>();

            foreach (CellModel cell in _board.GetAllCells())
            {
                if (cell == null || !cell.IsPlayable)
                {
                    continue;
                }

                SyncTileView(cell, cell.Tile, TileStackLayer.Base, aliveTileIds);
                SyncTileView(cell, cell.Overlay, TileStackLayer.Overlay, aliveTileIds);
            }

            List<int> staleTileIds = new List<int>();
            foreach (KeyValuePair<int, Match3TileView> pair in _tileViews)
            {
                if (!aliveTileIds.Contains(pair.Key))
                {
                    staleTileIds.Add(pair.Key);
                }
            }

            for (int i = 0; i < staleTileIds.Count; i++)
            {
                DestroyTileView(staleTileIds[i]);
            }
        }

        private void Rebuild()
        {
            if (_board == null)
            {
                Debug.LogError("[Match3BoardView] Board is null.");
                return;
            }

            if (cellPrefab == null)
            {
                Debug.LogError("[Match3BoardView] Missing cell prefab.");
                return;
            }

            ClearCells();
            ClearTileViews();
            PrewarmBoardPools();

            foreach (CellModel cell in _board.GetAllCells())
            {
                if (!cell.IsPlayable)
                {
                    continue;
                }

                Match3CellView cellView = CreateCellView(cell);
                if (cellView != null)
                {
                    _cellViews[cell] = cellView;
                }
            }

            SyncToBoardState();
            PrepareNormalTilesForIntro();
        }

        private void ClearCells()
        {
            if (Application.isPlaying)
            {
                List<Match3CellView> cellViews = new List<Match3CellView>(_cellViews.Values);
                for (int i = 0; i < cellViews.Count; i++)
                {
                    _cellViewFactory.ReturnVisual(cellViews[i]);
                }

                _cellViews.Clear();
                return;
            }

            Transform parent = cellRoot != null ? cellRoot : transform;
            List<Transform> children = new List<Transform>();

            for (int i = 0; i < parent.childCount; i++)
            {
                children.Add(parent.GetChild(i));
            }

            for (int i = 0; i < children.Count; i++)
            {
                if (Application.isPlaying)
                {
                    Destroy(children[i].gameObject);
                }
                else
                {
                    DestroyImmediate(children[i].gameObject);
                }
            }

            _cellViews.Clear();
        }

        private void ClearTileViews()
        {
            List<int> tileIds = new List<int>(_tileViews.Keys);
            for (int i = 0; i < tileIds.Count; i++)
            {
                DestroyTileView(tileIds[i]);
            }
        }

        private Match3TileView CreateTileView(TileModel tile, _PawSlidePopGame._Scripts.Feature.Match3.Data.BoardContentDefinitionSO definition, Vector3 localPosition)
        {
            if (tile == null || definition == null || definition.TileViewPrefab == null)
            {
                return null;
            }

            Transform parent = tileRoot != null ? tileRoot : transform;
            Match3TileView tileView = Application.isPlaying
                ? _tileViewFactory.CreateVisual(
                    new TileViewSpawnData(
                        definition.TileViewPrefab,
                        tile,
                        definition,
                        localPosition,
                        _isIdleEnabled,
                        $"Tile_{tile.InstanceId}_{tile.TileId}"),
                    parent)
                : Instantiate(definition.TileViewPrefab, parent);

            if (!Application.isPlaying && tileView != null)
            {
                tileView.name = $"Tile_{tile.InstanceId}_{tile.TileId}";
                tileView.Bind(tile, definition);
                tileView.SnapToLocalPosition(localPosition);
                tileView.SetIdleEnabled(_isIdleEnabled);
            }

            if (tileView == null)
            {
                return null;
            }

            _tileViews[tile.InstanceId] = tileView;
            return tileView;
        }

        private void DestroyTileView(int tileInstanceId)
        {
            if (!_tileViews.TryGetValue(tileInstanceId, out Match3TileView tileView))
            {
                return;
            }

            _tileViews.Remove(tileInstanceId);
            _previewedTileIds.Remove(tileInstanceId);

            if (tileView == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                _tileViewFactory.ReturnVisual(tileView);
                return;
            }

            DestroyImmediate(tileView.gameObject);
        }

        private Match3CellView CreateCellView(CellModel cell)
        {
            Transform parent = cellRoot != null ? cellRoot : transform;
            Vector3 localPosition = GetLocalPosition(cell.X, cell.Y);

            Match3CellView cellView;
            if (Application.isPlaying)
            {
                cellView = _cellViewFactory.CreateVisual(
                    new CellViewSpawnData(cellPrefab, cell, localPosition, $"Cell_{cell.X}_{cell.Y}"),
                    parent);
            }
            else
            {
                cellView = Instantiate(cellPrefab, parent);
                cellView.name = $"Cell_{cell.X}_{cell.Y}";
                cellView.transform.localPosition = localPosition;
                cellView.Initialize(cell);
            }

            if (cellView != null)
            {
                int cellArtId = 0;
                if (_levelData != null && _levelData.cellArtLayout != null)
                {
                    int index = (cell.Y * _levelData.width) + cell.X;
                    if (index >= 0 && index < _levelData.cellArtLayout.Length)
                    {
                        cellArtId = _levelData.cellArtLayout[index];
                    }
                }

                Sprite cellArtSprite = ResolveCellArt(cellArtId);
                if (cellView.FallbackRenderer != null && cellArtSprite != null)
                {
                    cellView.FallbackRenderer.sprite = cellArtSprite;
                }
            }

            return cellView;
        }

        private Sprite ResolveCellArt(int cellArtId)
        {
            if (_cellArtCatalog == null || cellArtId <= 0)
            {
                return null;
            }

            var entry = _cellArtCatalog.GetEntry(cellArtId);
            return entry != null ? entry.Sprite : null;
        }

        private void PrewarmBoardPools()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (cellPrefab != null)
            {
                PoolingManager.Instance.Prewarm(cellPrefab.gameObject, CountPlayableCells());
            }

            foreach (KeyValuePair<GameObject, int> pair in BuildTilePrewarmCounts())
            {
                if (pair.Key == null || pair.Value <= 0)
                {
                    continue;
                }

                PoolingManager.Instance.Prewarm(pair.Key, pair.Value);
            }
        }

        private int CountPlayableCells()
        {
            if (_board == null)
            {
                return 0;
            }

            int count = 0;
            foreach (CellModel cell in _board.GetAllCells())
            {
                if (cell != null && cell.IsPlayable)
                {
                    count++;
                }
            }

            return count;
        }

        private Dictionary<GameObject, int> BuildTilePrewarmCounts()
        {
            Dictionary<GameObject, int> counts = new Dictionary<GameObject, int>();

            if (_board != null)
            {
                foreach (CellModel cell in _board.GetAllCells())
                {
                    AccumulatePrewarmCount(counts, cell?.Tile?.Definition);
                    AccumulatePrewarmCount(counts, cell?.Overlay?.Definition);
                }
            }

            if (_levelData == null || _tileDatabase == null || _levelData.spawnableTileIds == null || spawnableTilePrewarmReserve <= 0)
            {
                return counts;
            }

            HashSet<GameObject> reservedPrefabs = new HashSet<GameObject>();
            for (int i = 0; i < _levelData.spawnableTileIds.Count; i++)
            {
                BoardContentDefinitionSO definition = _tileDatabase.GetContentDefinition(_levelData.spawnableTileIds[i]);
                Match3TileView prefab = definition?.TileViewPrefab;
                if (prefab == null)
                {
                    continue;
                }

                GameObject prefabObject = prefab.gameObject;
                if (!reservedPrefabs.Add(prefabObject))
                {
                    continue;
                }

                counts[prefabObject] = counts.TryGetValue(prefabObject, out int currentCount)
                    ? currentCount + spawnableTilePrewarmReserve
                    : spawnableTilePrewarmReserve;
            }

            return counts;
        }

        private IEnumerator PlayClearPhase(ClearPhaseTrace clearPhase, float speedMultiplier = 1f)
        {
            if (clearPhase == null)
            {
                yield break;
            }

            List<IEnumerator> routines = new List<IEnumerator>();
            List<ScoreGainOp> pendingScoreGains = clearPhase.ScoreGainOps != null
                ? new List<ScoreGainOp>(clearPhase.ScoreGainOps)
                : new List<ScoreGainOp>();

            for (int i = 0; i < clearPhase.ActivateOps.Count; i++)
            {
                TileActivateOp op = clearPhase.ActivateOps[i];
                OnTileActivatePlaybackStarted?.Invoke(op);
                routines.Add(PlayActivateOp(op, clearPhase));
            }

            yield return StartCoroutine(RunParallel(routines));
            routines.Clear();

            for (int i = 0; i < clearPhase.TargetSelectionOps.Count; i++)
            {
                TargetSelectionOp op = clearPhase.TargetSelectionOps[i];
                if (_tileViews.TryGetValue(op.TargetTileInstanceId, out Match3TileView tileView) && tileView != null)
                {
                    routines.Add(tileView.PlayTargetSelectionAsync());
                }
            }

            yield return StartCoroutine(RunParallel(routines));
            routines.Clear();

            for (int i = 0; i < clearPhase.DamageOps.Count; i++)
            {
                TileDamageOp op = clearPhase.DamageOps[i];
                if (_tileViews.TryGetValue(op.TileInstanceId, out Match3TileView tileView) && tileView != null)
                {
                    routines.Add(tileView.PlayDamageAsync(op.CurrentHP, op.PreviousHP));
                }
            }

            yield return StartCoroutine(RunParallel(routines));
            routines.Clear();

            for (int i = 0; i < clearPhase.ClearOps.Count; i++)
            {
                TileClearOp op = clearPhase.ClearOps[i];
                OnTileClearPlaybackStarted?.Invoke(op, ResolveWorldPosition(op));
                DispatchScoreGainsForClearOp(op, pendingScoreGains);

                if (_tileViews.TryGetValue(op.TileInstanceId, out Match3TileView tileView) && tileView != null)
                {
                    routines.Add(tileView.PlayClearAsync(!op.WasMatched));
                }
            }

            yield return StartCoroutine(RunParallel(routines));
            routines.Clear();

            for (int i = 0; i < clearPhase.ClearOps.Count; i++)
            {
                DestroyTileView(clearPhase.ClearOps[i].TileInstanceId);
            }

            for (int i = 0; i < clearPhase.SpecialCreateOps.Count; i++)
            {
                SpecialCreateOp op = clearPhase.SpecialCreateOps[i];
                OnSpecialCreatePlaybackStarted?.Invoke(op);
                DispatchScoreGainsForSpecialCreateOp(op, pendingScoreGains);
                routines.Add(PlaySpecialCreateOp(op, speedMultiplier));
            }

            DispatchRemainingScoreGains(pendingScoreGains);
            yield return StartCoroutine(RunParallel(routines));
            routines.Clear();

            if (phaseGap > 0f)
            {
                yield return new WaitForSeconds(phaseGap / speedMultiplier);
            }
        }

        private IEnumerator PlayGravityPhase(GravityPhaseTrace gravityPhase, float speedMultiplier = 1f)
        {
            if (gravityPhase == null || gravityPhase.TravelOps.Count == 0)
            {
                yield break;
            }

            List<IEnumerator> moveRoutines = new List<IEnumerator>();
            List<Match3TileView> landingViews = new List<Match3TileView>();

            for (int i = 0; i < gravityPhase.TravelOps.Count; i++)
            {
                TileTravelOp op = gravityPhase.TravelOps[i];
                if (!_tileViews.TryGetValue(op.TileInstanceId, out Match3TileView tileView) || tileView == null)
                {
                    continue;
                }

                float duration = Mathf.Max(0.05f / speedMultiplier, (gravityDurationPerCell / speedMultiplier) * Mathf.Max(1, op.Distance));
                moveRoutines.Add(tileView.PlayMoveAsync(GetTileLocalPosition(op.ToCell.X, op.ToCell.Y, op.Layer), duration));
                landingViews.Add(tileView);
            }

            yield return StartCoroutine(RunParallel(moveRoutines));
            yield return StartCoroutine(PlayLandings(landingViews, gravityPhase.TravelOps, speedMultiplier));

            if (phaseGap > 0f)
            {
                yield return new WaitForSeconds(phaseGap / speedMultiplier);
            }
        }

        private IEnumerator PlayRefillPhase(RefillPhaseTrace refillPhase, float speedMultiplier = 1f)
        {
            if (refillPhase == null || refillPhase.SpawnOps.Count == 0)
            {
                yield break;
            }

            List<IEnumerator> spawnRoutines = new List<IEnumerator>();
            List<Match3TileView> landingViews = new List<Match3TileView>();
            List<int> landingDistances = new List<int>();

            for (int i = 0; i < refillPhase.SpawnOps.Count; i++)
            {
                TileSpawnOp op = refillPhase.SpawnOps[i];
                if (_tileViews.TryGetValue(op.TileInstanceId, out Match3TileView existingTileView) && existingTileView != null)
                {
                    continue;
                }

                Match3TileView tileView = CreateTileView(
                    new TileModel(op.TileInstanceId, op.Definition),
                    op.Definition,
                    GetTileLocalPosition(op.ToCell.X, op.SpawnFromRowAboveBoard, op.Layer));
                if (tileView == null)
                {
                    continue;
                }

                float distance = Mathf.Abs(op.ToCell.Y - op.SpawnFromRowAboveBoard);
                float duration = Mathf.Max(0.08f / speedMultiplier, (refillDurationPerCell / speedMultiplier) * Mathf.Max(1f, distance));
                spawnRoutines.Add(tileView.PlaySpawnFallAsync(
                    GetTileLocalPosition(op.ToCell.X, op.SpawnFromRowAboveBoard, op.Layer),
                    GetTileLocalPosition(op.ToCell.X, op.ToCell.Y, op.Layer),
                    duration));
                landingViews.Add(tileView);
                landingDistances.Add(Mathf.Max(1, Mathf.RoundToInt(distance)));
            }

            yield return StartCoroutine(RunParallel(spawnRoutines));

            List<TileTravelOp> landingOps = new List<TileTravelOp>(landingViews.Count);
            for (int i = 0; i < landingViews.Count; i++)
            {
                landingOps.Add(new TileTravelOp
                {
                    TileInstanceId = landingViews[i].TileInstanceId,
                    Distance = landingDistances[i]
                });
            }

            yield return StartCoroutine(PlayLandings(landingViews, landingOps, speedMultiplier));

            if (phaseGap > 0f)
            {
                yield return new WaitForSeconds(phaseGap / speedMultiplier);
            }
        }

        private IEnumerator PlayLineTravel(IReadOnlyList<TileTravelOp> travelOps, MoveAxis axis, LineSlideDirection direction, float duration)
        {
            if (travelOps == null || travelOps.Count == 0)
            {
                yield break;
            }

            List<IEnumerator> routines = new List<IEnumerator>();
            List<WrapSnapData> wrapSnaps = new List<WrapSnapData>();

            for (int i = 0; i < travelOps.Count; i++)
            {
                TileTravelOp op = travelOps[i];
                if (!_tileViews.TryGetValue(op.TileInstanceId, out Match3TileView tileView) || tileView == null)
                {
                    continue;
                }

                Vector3 finalTarget = GetTileLocalPosition(op.ToCell.X, op.ToCell.Y, op.Layer);

                if (op.IsWrapAround)
                {
                    // Ẩn ngay lập tức, không tween ra ngoài board
                    tileView.HideForWrapExit();
                    wrapSnaps.Add(new WrapSnapData(tileView, finalTarget));
                }
                else
                {
                    routines.Add(tileView.PlayMoveAsync(finalTarget, duration));
                }
            }

            yield return StartCoroutine(RunParallel(routines));

            for (int i = 0; i < travelOps.Count; i++)
            {
                if (_tileViews.TryGetValue(travelOps[i].TileInstanceId, out Match3TileView tileView) && tileView != null)
                {
                    tileView.SetShadowState(TileShadowState.Off);
                }
            }

            // Snap về biên đối diện rồi hiện lại ngay
            for (int i = 0; i < wrapSnaps.Count; i++)
            {
                wrapSnaps[i].TileView.SnapToLocalPosition(wrapSnaps[i].TargetLocalPosition);
                wrapSnaps[i].TileView.SetShadowState(TileShadowState.Off);
                wrapSnaps[i].TileView.ShowAfterWrapEntry();
            }

            if (phaseGap > 0f)
            {
                yield return new WaitForSeconds(phaseGap);
            }
        }


        private IEnumerator PlayLandings(IReadOnlyList<Match3TileView> tileViews, IReadOnlyList<TileTravelOp> ops, float speedMultiplier = 1f)
        {
            if (tileViews == null || tileViews.Count == 0)
            {
                yield break;
            }

            List<IEnumerator> routines = new List<IEnumerator>();
            for (int i = 0; i < tileViews.Count; i++)
            {
                Match3TileView tileView = tileViews[i];
                int distance = ops != null && i < ops.Count ? Mathf.Max(1, ops[i].Distance) : 1;
                routines.Add(tileView.PlayLandAsync(Mathf.Clamp(distance * 0.35f, 0.7f, 1.5f), speedMultiplier));
            }

            yield return StartCoroutine(RunParallel(routines));
        }

        private IEnumerator RunParallel(IReadOnlyList<IEnumerator> routines)
        {
            if (routines == null || routines.Count == 0)
            {
                yield break;
            }

            int remaining = 0;
            for (int i = 0; i < routines.Count; i++)
            {
                if (routines[i] == null)
                {
                    continue;
                }

                remaining++;
                StartCoroutine(WrapRoutine(routines[i], () => remaining--));
            }

            while (remaining > 0)
            {
                yield return null;
            }
        }

        private TargetSelectionOp FindTargetSelectionForSource(ClearPhaseTrace clearPhase, int sourceTileInstanceId)
        {
            if (clearPhase?.TargetSelectionOps == null) return null;
            for (int i = 0; i < clearPhase.TargetSelectionOps.Count; i++)
            {
                if (clearPhase.TargetSelectionOps[i].SourceTileInstanceId == sourceTileInstanceId)
                {
                    return clearPhase.TargetSelectionOps[i];
                }
            }
            return null;
        }

        private IEnumerator PlayActivateOp(TileActivateOp activateOp, ClearPhaseTrace clearPhase)
        {
            if (activateOp.LogicType == TileLogicType.CrossBomb)
            {
                yield return StartCoroutine(PlayCrossHighlight(activateOp.Cell));
            }

            if (_tileViews.TryGetValue(activateOp.TileInstanceId, out Match3TileView tileView) && tileView != null)
            {
                if (activateOp.LogicType == TileLogicType.SquareBomb && tileView is SquareBombTileView squareTile)
                {
                    TargetSelectionOp targetOp = FindTargetSelectionForSource(clearPhase, activateOp.TileInstanceId);
                    if (targetOp != null)
                    {
                        Vector3 targetLocalPos = GetTileLocalPosition(targetOp.TargetCell.X, targetOp.TargetCell.Y, targetOp.TargetLayer);
                        yield return StartCoroutine(squareTile.PlaySquareFlyAndExplodeAsync(targetLocalPos));
                        yield break;
                    }
                }

                yield return StartCoroutine(tileView.PlayActivateAsync());
            }
        }

        private IEnumerator PlaySpecialCreateOp(SpecialCreateOp createOp, float speedMultiplier = 1f)
        {
            Match3TileView sourceTileView = null;
            if (_tileViews.TryGetValue(createOp.SourceTileInstanceId, out Match3TileView existingView))
            {
                sourceTileView = existingView;
            }

            if (sourceTileView != null)
            {
                yield return StartCoroutine(sourceTileView.PlaySpecialCreateAsync(speedMultiplier));
            }

            if (createOp.ReplaceSourceTileView)
            {
                DestroyTileView(createOp.SourceTileInstanceId);
            }

            CellModel boardCell = _board != null ? _board.GetCell(createOp.Cell.X, createOp.Cell.Y) : null;
            TileModel boardTile = boardCell?.GetTile(createOp.Layer);
            if (boardTile == null || boardTile.InstanceId != createOp.NewTileInstanceId)
            {
                boardTile = new TileModel(createOp.NewTileInstanceId, createOp.Definition);
            }

            Match3TileView newTileView = CreateTileView(boardTile, createOp.Definition, GetTileLocalPosition(createOp.Cell.X, createOp.Cell.Y, createOp.Layer));
            if (newTileView != null)
            {
                yield return StartCoroutine(newTileView.PlaySpecialCreateAsync(speedMultiplier));
            }
        }

        private IEnumerator PlayCrossHighlight(BoardCellPosition center)
        {
            if (_board == null)
            {
                yield break;
            }

            List<Match3TileView> highlighted = new List<Match3TileView>();
            HighlightCells(_board.GetPlayableCellsInRow(center.Y), highlighted);
            HighlightCells(_board.GetPlayableCellsInColumn(center.X), highlighted);
            yield return new WaitForSeconds(Mathf.Max(0.04f, phaseGap + 0.04f));

            for (int i = 0; i < highlighted.Count; i++)
            {
                if (highlighted[i] != null)
                {
                    highlighted[i].SetShadowState(TileShadowState.Off);
                }
            }
        }

        private void HighlightCells(IReadOnlyList<CellModel> cells, List<Match3TileView> highlighted)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                TileModel tile = cells[i].TopTile;
                if (tile == null || !_tileViews.TryGetValue(tile.InstanceId, out Match3TileView tileView) || tileView == null)
                {
                    continue;
                }

                tileView.SetShadowState(TileShadowState.Active);
                if (!highlighted.Contains(tileView))
                {
                    highlighted.Add(tileView);
                }
            }
        }

        private void HighlightCellTiles(IReadOnlyList<CellModel> cells, TileShadowState state)
        {
            if (cells == null)
            {
                return;
            }

            for (int i = 0; i < cells.Count; i++)
            {
                CellModel cell = cells[i];
                TileModel tile = cell?.Tile;
                if (tile == null || !_tileViews.TryGetValue(tile.InstanceId, out Match3TileView tileView) || tileView == null)
                {
                    continue;
                }

                tileView.SetShadowState(state);
                tileView.SetSelectedScale(state == TileShadowState.Active);
                _previewedTileIds.Add(tile.InstanceId);
            }
        }

        private IEnumerator WrapRoutine(IEnumerator routine, System.Action onComplete)
        {
            yield return StartCoroutine(routine);
            onComplete?.Invoke();
        }

        private void DispatchScoreGainsForClearOp(TileClearOp clearOp, List<ScoreGainOp> pendingScoreGains)
        {
            if (clearOp == null || pendingScoreGains == null || pendingScoreGains.Count == 0)
            {
                return;
            }

            for (int i = pendingScoreGains.Count - 1; i >= 0; i--)
            {
                ScoreGainOp scoreGainOp = pendingScoreGains[i];
                if (!MatchesClearOp(scoreGainOp, clearOp))
                {
                    continue;
                }

                OnScoreGainPlaybackStarted?.Invoke(scoreGainOp);
                pendingScoreGains.RemoveAt(i);
            }
        }

        private void DispatchScoreGainsForSpecialCreateOp(SpecialCreateOp specialCreateOp, List<ScoreGainOp> pendingScoreGains)
        {
            if (specialCreateOp == null || pendingScoreGains == null || pendingScoreGains.Count == 0)
            {
                return;
            }

            for (int i = pendingScoreGains.Count - 1; i >= 0; i--)
            {
                ScoreGainOp scoreGainOp = pendingScoreGains[i];
                if (!MatchesSpecialCreateOp(scoreGainOp, specialCreateOp))
                {
                    continue;
                }

                OnScoreGainPlaybackStarted?.Invoke(scoreGainOp);
                pendingScoreGains.RemoveAt(i);
            }
        }

        private void DispatchRemainingScoreGains(List<ScoreGainOp> pendingScoreGains)
        {
            if (pendingScoreGains == null || pendingScoreGains.Count == 0)
            {
                return;
            }

            for (int i = 0; i < pendingScoreGains.Count; i++)
            {
                OnScoreGainPlaybackStarted?.Invoke(pendingScoreGains[i]);
            }

            pendingScoreGains.Clear();
        }

        private Vector3 GetTileLocalPosition(CellModel cell)
        {
            return GetTileLocalPosition(cell, TileStackLayer.Base);
        }

        private Vector3 GetTileLocalPosition(CellModel cell, TileStackLayer layer)
        {
            if (cell != null && _cellViews.TryGetValue(cell, out Match3CellView cellView) && cellView != null)
            {
                return cellView.GetTileAnchorLocalPosition(layer, transform);
            }

            if (cell == null)
            {
                return Vector3.zero;
            }

            return GetTileLocalPosition(cell.X, cell.Y, layer);
        }

        public Vector3 GetCellWorldPosition(int x, int y)
        {
            return transform.TransformPoint(GetTileLocalPosition(x, y, TileStackLayer.Base));
        }

        private Vector3 ResolveWorldPosition(TileClearOp clearOp)
        {
            if (clearOp != null &&
                _tileViews.TryGetValue(clearOp.TileInstanceId, out Match3TileView tileView) &&
                tileView != null)
            {
                return tileView.transform.position;
            }

            return transform.TransformPoint(GetTileLocalPosition(clearOp.Cell.X, clearOp.Cell.Y, clearOp.Layer));
        }

        private Vector3 GetWrapExitPosition(BoardCellPosition fromCell, MoveAxis axis, LineSlideDirection direction)
        {
            if (_board == null)
            {
                return GetLocalPosition(fromCell.X, fromCell.Y);
            }

            if (axis == MoveAxis.Row)
            {
                int exitX = direction == LineSlideDirection.Right ? _board.Width : -1;
                return GetTileLocalPosition(exitX, fromCell.Y, TileStackLayer.Base);
            }

            int exitY = direction == LineSlideDirection.Down ? _board.Height : -1;
            return GetTileLocalPosition(fromCell.X, exitY, TileStackLayer.Base);
        }

        private Vector3 GetLocalPosition(int x, int y)
        {
            float offsetX = _board != null ? -((_board.Width - 1) * cellStepX) * 0.5f : 0f;
            float offsetY = _board != null ? ((_board.Height - 1) * cellStepY) * 0.5f : 0f;
            return new Vector3(boardOffset.x + offsetX + (x * cellStepX), boardOffset.y + offsetY - (y * cellStepY), 0f);
        }

        private Vector3 GetTileLocalPosition(int x, int y, TileStackLayer layer)
        {
            CellModel cell = _board != null ? _board.GetCell(x, y) : null;
            if (cell != null && _cellViews.TryGetValue(cell, out Match3CellView cellView) && cellView != null)
            {
                return cellView.GetTileAnchorLocalPosition(layer, transform);
            }

            Vector3 position = GetLocalPosition(x, y);
            if (layer == TileStackLayer.Overlay)
            {
                position.z -= 0.05f;
            }

            return position;
        }

        private void SyncTileView(CellModel cell, TileModel tile, TileStackLayer layer, HashSet<int> aliveTileIds)
        {
            if (cell == null || tile == null)
            {
                return;
            }

            aliveTileIds.Add(tile.InstanceId);
            Vector3 targetLocalPosition = GetTileLocalPosition(cell, layer);
            if (!_tileViews.TryGetValue(tile.InstanceId, out Match3TileView tileView) || tileView == null)
            {
                tileView = CreateTileView(tile, tile.Definition, targetLocalPosition);
            }
            else
            {
                tileView.Bind(tile, tile.Definition);
                tileView.SnapToLocalPosition(targetLocalPosition);
            }

            if (tileView != null)
            {
                tileView.SetShadowState(TileShadowState.Off);
                tileView.SetIdleEnabled(_isIdleEnabled);
            }
        }

        private static void AccumulatePrewarmCount(Dictionary<GameObject, int> counts, BoardContentDefinitionSO definition)
        {
            Match3TileView prefab = definition?.TileViewPrefab;
            if (prefab == null)
            {
                return;
            }

            GameObject prefabObject = prefab.gameObject;
            counts[prefabObject] = counts.TryGetValue(prefabObject, out int currentCount)
                ? currentCount + 1
                : 1;
        }

        private static bool MatchesClearOp(ScoreGainOp scoreGainOp, TileClearOp clearOp)
        {
            return scoreGainOp != null &&
                   clearOp != null &&
                   scoreGainOp.TileInstanceId == clearOp.TileInstanceId &&
                   scoreGainOp.Cell.Equals(clearOp.Cell);
        }

        private static bool MatchesSpecialCreateOp(ScoreGainOp scoreGainOp, SpecialCreateOp specialCreateOp)
        {
            return scoreGainOp != null &&
                   specialCreateOp != null &&
                   scoreGainOp.TileInstanceId == specialCreateOp.SourceTileInstanceId &&
                   scoreGainOp.Cell.Equals(specialCreateOp.Cell);
        }

        public void PrepareNormalTilesForIntro()
        {
            if (_board == null)
            {
                return;
            }

            foreach (CellModel cell in _board.GetAllCells())
            {
                if (cell == null || !cell.IsPlayable || cell.Tile == null || cell.Tile.TileKind != TileKind.Normal)
                {
                    continue;
                }

                if (_tileViews.TryGetValue(cell.Tile.InstanceId, out Match3TileView tileView) && tileView != null)
                {
                    Vector3 startPos = GetTileLocalPosition(cell.X, -3, TileStackLayer.Base);
                    tileView.SnapToLocalPosition(startPos);
                    tileView.transform.localScale = Vector3.zero;
                    tileView.SetShadowState(TileShadowState.Off);
                }
            }
        }

        public IEnumerator PlayIntroAnimation()
        {
            if (_board == null)
            {
                yield break;
            }

            List<IEnumerator> routines = new List<IEnumerator>();
            float maxStagger = introDuration * 0.4f;
            float rowDelay = _board.Height > 1 ? maxStagger / (_board.Height - 1) : 0f;
            float singleTileDuration = introDuration * 0.6f;

            foreach (CellModel cell in _board.GetAllCells())
            {
                if (cell == null || !cell.IsPlayable || cell.Tile == null || cell.Tile.TileKind != TileKind.Normal)
                {
                    continue;
                }

                if (_tileViews.TryGetValue(cell.Tile.InstanceId, out Match3TileView tileView) && tileView != null)
                {
                    Vector3 targetPos = GetTileLocalPosition(cell, TileStackLayer.Base);
                    Vector3 startPos = GetTileLocalPosition(cell.X, -3, TileStackLayer.Base);

                    float delay = (_board.Height - 1 - cell.Y) * rowDelay;

                    routines.Add(PlayIntroTileFall(tileView, startPos, targetPos, delay, singleTileDuration));
                }
            }

            yield return StartCoroutine(RunParallel(routines));
            SyncToBoardState();
        }

        private IEnumerator PlayIntroTileFall(Match3TileView tileView, Vector3 startPos, Vector3 targetPos, float delay, float duration)
        {
            tileView.SnapToLocalPosition(startPos);
            tileView.transform.localScale = Vector3.zero;
            tileView.SetShadowState(TileShadowState.Off);
            tileView.SetIdleEnabled(false);

            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            yield return StartCoroutine(tileView.PlaySpawnFallAsync(startPos, targetPos, duration * 0.7f));
            yield return StartCoroutine(tileView.PlayLandAsync(0.6f));

            tileView.ResetVisualState();
            tileView.SetIdleEnabled(_isIdleEnabled);
        }

        public IEnumerator PlayLoseOutro()
        {
            if (_board == null)
            {
                yield break;
            }

            List<IEnumerator> routines = new List<IEnumerator>();
            float centerX = _board.Width / 2f;
            float centerY = _board.Height / 2f;

            foreach (CellModel cell in _board.GetAllCells())
            {
                if (cell == null || !cell.IsPlayable)
                {
                    continue;
                }

                float distance = Mathf.Sqrt((cell.X - centerX) * (cell.X - centerX) + (cell.Y - centerY) * (cell.Y - centerY));
                float delay = distance * 0.08f;

                if (cell.Tile != null && _tileViews.TryGetValue(cell.Tile.InstanceId, out Match3TileView tileView) && tileView != null)
                {
                    routines.Add(PlaySadTileOutro(tileView, delay, loseOutroDuration));
                }

                if (cell.Overlay != null && _tileViews.TryGetValue(cell.Overlay.InstanceId, out Match3TileView overlayView) && overlayView != null)
                {
                    routines.Add(PlaySadTileOutro(overlayView, delay, loseOutroDuration));
                }
            }

            yield return StartCoroutine(RunParallel(routines));
            ClearTileViews();
        }

        private IEnumerator PlaySadTileOutro(Match3TileView tileView, float delay, float duration)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            tileView.SetShadowState(TileShadowState.Off);
            tileView.SetIdleEnabled(false);
            tileView.ForceEyesClosed(true);

            float shiverDuration = duration * 0.5f;
            float fadeDuration = duration * 0.5f;

            tileView.transform.DOShakePosition(shiverDuration, 0.12f, 20, 90f, false, true);
            tileView.ApplyDimmedState(true);

            yield return new WaitForSeconds(shiverDuration);

            Sequence seq = DOTween.Sequence().SetLink(tileView.gameObject);
            seq.Join(tileView.transform.DOScale(Vector3.zero, fadeDuration).SetEase(Ease.InBack));

            yield return seq.WaitForCompletion();
        }

        private void OnValidate()
        {
            if (spawnableTilePrewarmReserve < 0)
            {
                spawnableTilePrewarmReserve = 0;
            }
        }

        private readonly struct WrapSnapData
        {
            public Match3TileView TileView { get; }
            public Vector3 TargetLocalPosition { get; }

            public WrapSnapData(Match3TileView tileView, Vector3 targetLocalPosition)
            {
                TileView = tileView;
                TargetLocalPosition = targetLocalPosition;
            }
        }
    }
}
