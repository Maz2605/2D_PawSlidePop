using TMPro;
using UnityEngine;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Model;
using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.UI
{
    public sealed class LevelEditorSelectionPresenter : MonoBehaviour
    {
        [SerializeField] private TMP_Text selectedCoordText;
        [SerializeField] private TMP_Text selectedUnderText;
        [SerializeField] private TMP_Text selectedItemText;
        [SerializeField] private TMP_Text selectedOverText;
        [SerializeField] private Color underHighlightColor = new Color(0.52f, 0.88f, 0.62f, 1f);
        [SerializeField] private Color itemHighlightColor = new Color(1f, 0.86f, 0.4f, 1f);
        [SerializeField] private Color overlayHighlightColor = new Color(0.98f, 0.58f, 0.58f, 1f);
        [SerializeField] private Color emptyValueColor = new Color(0.86f, 0.92f, 1f, 0.72f);
        [SerializeField] private Color selectedCoordColor = Color.white;
        [SerializeField] private Color idleCoordColor = new Color(0.86f, 0.92f, 1f, 0.72f);

        private LevelEditorUIController _service;

        public void Bind(LevelEditorUIController service)
        {
            _service = service;
        }

        public void Refresh()
        {
            if (_service?.Selection == null)
            {
                return;
            }
            
            bool hasSelectedCell = _service.Selection.HasSelectedCell;
            LevelEditorCellState? selectedCell = GetSelectedCellState(hasSelectedCell);

            if (selectedCoordText != null)
            {
                selectedCoordText.text = hasSelectedCell
                    ? $"Selected Cell ({_service.Selection.SelectedX}, {_service.Selection.SelectedY})"
                    : "Selected Cell (None)";
                selectedCoordText.color = hasSelectedCell ? selectedCoordColor : idleCoordColor;
            }

            if (selectedUnderText != null)
            {
                int underlayId = selectedCell.HasValue ? selectedCell.Value.UnderlayId : 0;
                bool hasValue = underlayId > 0;
                selectedUnderText.text = _service.GetContentDisplayName(BoardLayer.Underlay, underlayId);
                selectedUnderText.color = hasValue ? underHighlightColor : emptyValueColor;
            }

            if (selectedItemText != null)
            {
                int tileId = selectedCell.HasValue ? selectedCell.Value.TileId : 0;
                bool hasValue = tileId > 0;
                selectedItemText.text = _service.GetContentDisplayName(BoardLayer.Tile, tileId);
                selectedItemText.color = hasValue ? itemHighlightColor : emptyValueColor;
            }

            if (selectedOverText != null)
            {
                int overlayId = selectedCell.HasValue ? selectedCell.Value.OverlayId : 0;
                bool hasValue = overlayId > 0;
                selectedOverText.text = _service.GetContentDisplayName(BoardLayer.Overlay, overlayId);
                selectedOverText.color = hasValue ? overlayHighlightColor : emptyValueColor;
            }
        }

        private LevelEditorCellState? GetSelectedCellState(bool hasSelectedCell)
        {
            if (!hasSelectedCell || _service?.Session?.board == null)
            {
                return null;
            }

            int x = _service.Selection.SelectedX;
            int y = _service.Selection.SelectedY;
            if (!_service.Session.board.IsInBounds(x, y))
            {
                return null;
            }

            return _service.Session.board.GetCell(x, y);
        }
    }
}
