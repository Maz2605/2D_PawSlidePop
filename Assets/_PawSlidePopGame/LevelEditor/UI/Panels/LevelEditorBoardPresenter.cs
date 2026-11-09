using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Model;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;
using UnityEngine;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.UI
{
    public sealed class LevelEditorBoardPresenter : MonoBehaviour
    {
        [SerializeField] private Transform boardContentRoot;
        [SerializeField] private LevelEditorBoardCellView cellTemplate;
        [SerializeField] private float cellSpacingX = 1.1f;
        [SerializeField] private float cellSpacingY = 1.1f;
        [SerializeField] private Vector3 boardOffset;
        [SerializeField] private Sprite cellBackgroundSprite;

        private LevelEditorUIController _service;
        private readonly List<LevelEditorBoardCellView> _cells = new List<LevelEditorBoardCellView>();
        private int _renderedWidth = -1;
        private int _renderedHeight = -1;
        private string _renderedLevelId;

        public void Bind(LevelEditorUIController service)
        {
            _service = service;
        }

        public Vector3 GetLocalPosition(int x, int y, int width, int height)
        {
            float offsetX = -((width - 1) * cellSpacingX) * 0.5f + boardOffset.x;
            float offsetY = ((height - 1) * cellSpacingY) * 0.5f + boardOffset.y;
            return new Vector3(offsetX + (x * cellSpacingX), offsetY - (y * cellSpacingY), 0f);
        }

        public void Refresh()
        {
            if (_service?.Session?.board == null || boardContentRoot == null || cellTemplate == null)
            {
                return;
            }

            LevelEditorBoardState board = _service.Session.board;
            bool levelChanged = _service.Session != null && _renderedLevelId != _service.Session.levelId;
            bool sizeChanged = _renderedWidth != board.width || _renderedHeight != board.height;

            EnsureCells(board);

            if (levelChanged)
            {
                _renderedLevelId = _service.Session.levelId;
            }

            for (int y = 0; y < board.height; y++)
            {
                for (int x = 0; x < board.width; x++)
                {
                    LevelEditorCellState cell = board.GetCell(x, y);
                    int index = board.ToIndex(x, y);
                    LevelEditorBoardCellView view = _cells[index];
                    view.transform.localPosition = GetLocalPosition(x, y, board.width, board.height);

                    Sprite cellArt = ResolveCellArt(cell);
                    Sprite underlay = ResolveIcon(cell.UnderlayId);
                    Sprite tile = ResolveIcon(cell.TileId);
                    Sprite overlay = ResolveIcon(cell.OverlayId);

                    if (_service != null)
                    {
                        switch (_service.ViewMode)
                        {
                            case LevelEditorViewMode.Normal:
                                underlay = null;
                                overlay = null;
                                break;
                            case LevelEditorViewMode.Overlay:
                                tile = null;
                                underlay = null;
                                break;
                            case LevelEditorViewMode.Underlay:
                                tile = null;
                                overlay = null;
                                break;
                            case LevelEditorViewMode.ShowAll:
                            default:
                                break;
                        }
                    }

                    view.Bind(
                        x,
                        y,
                        cellArt,
                        underlay,
                        tile,
                        overlay,
                        cellBackgroundSprite,
                        _service.Selection != null && _service.Selection.HasSelectedCell && _service.Selection.SelectedX == x && _service.Selection.SelectedY == y,
                        cell.Playable,
                        HandleCellAction,
                        _service);
                }
            }

            cellTemplate.gameObject.SetActive(false);

            if (levelChanged || sizeChanged)
            {
                var camCtrl = Camera.main != null ? Camera.main.GetComponent<_PawSlidePopGame._Scripts.Feature.LevelEditor.Scene.LevelEditorCameraController>() : null;
                if (camCtrl != null)
                {
                    camCtrl.AutoFit(board.width, board.height, boardOffset);
                }
            }
        }

        private void HandleCellAction(int x, int y, LevelEditorBoardCellAction action)
        {
            switch (action)
            {
                case LevelEditorBoardCellAction.Erase:
                    _service?.EraseCell(x, y);
                    break;
                case LevelEditorBoardCellAction.Paint:
                default:
                    _service?.PaintCell(x, y);
                    break;
            }
        }

        private void EnsureCells(LevelEditorBoardState board)
        {
            if (_renderedWidth == board.width && _renderedHeight == board.height && _cells.Count == board.CellCount)
            {
                return;
            }

            ClearCells();
            _renderedWidth = board.width;
            _renderedHeight = board.height;

            for (int i = 0; i < board.CellCount; i++)
            {
                LevelEditorBoardCellView view = Instantiate(cellTemplate, boardContentRoot);
                view.gameObject.SetActive(true);
                _cells.Add(view);
            }
        }

        private void ClearCells()
        {
            for (int i = _cells.Count - 1; i >= 0; i--)
            {
                if (_cells[i] != null)
                {
                    DestroyViewObject(_cells[i].gameObject);
                }
            }

            _cells.Clear();
        }

        private static void DestroyViewObject(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        private Sprite ResolveIcon(int contentId)
        {
            if (contentId <= 0 || _service?.TileDatabase == null)
            {
                return null;
            }

            BoardContentDefinitionSO definition = _service.TileDatabase.GetContentDefinition(contentId);
            return definition != null ? definition.Icon : null;
        }

        private Sprite ResolveCellArt(LevelEditorCellState cell)
        {
            if (_service?.CellArtCatalog == null)
            {
                return null;
            }

            int cellArtId = _service.GetCellArtId(cell.Coordinate.X, cell.Coordinate.Y);
            LevelEditorCellArtDefinition definition = _service.CellArtCatalog.GetEntry(cellArtId);
            return definition != null ? definition.Sprite : null;
        }
    }
}
