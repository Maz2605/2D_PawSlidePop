using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Model;
using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.Scene
{
    public sealed class LevelEditorSelectionStateHolder : MonoBehaviour
    {
        [SerializeField] private int selectedUnderlayId;
        [SerializeField] private int selectedTileId = 101;
        [SerializeField] private int selectedOverlayId = 301;
        [SerializeField] private int selectedCellArtId = 1;
        [SerializeField] private LevelEditorPaletteSectionType activeSectionType = LevelEditorPaletteSectionType.ItemNormal;
        [SerializeField] private bool hasSelectedCell;
        [SerializeField] private int selectedX;
        [SerializeField] private int selectedY;

        public int SelectedUnderlayId
        {
            get => selectedUnderlayId;
            set => selectedUnderlayId = Mathf.Max(0, value);
        }

        public int SelectedTileId
        {
            get => selectedTileId;
            set => selectedTileId = Mathf.Max(0, value);
        }

        public int SelectedOverlayId
        {
            get => selectedOverlayId;
            set => selectedOverlayId = Mathf.Max(0, value);
        }

        public bool HasSelectedCell => hasSelectedCell;
        public int SelectedX => selectedX;
        public int SelectedY => selectedY;
        public int SelectedCellArtId
        {
            get => selectedCellArtId;
            set => selectedCellArtId = Mathf.Max(0, value);
        }
        public LevelEditorCoordinate SelectedCoordinate => new LevelEditorCoordinate(selectedX, selectedY);
        public LevelEditorPaletteSectionType ActiveSectionType => activeSectionType;
        public BoardLayer ActiveBoardLayer => ResolveBoardLayer(activeSectionType);
        public bool IsCellArtSectionActive => activeSectionType == LevelEditorPaletteSectionType.CellArt;

        public void SetSelectedCoordinate(int x, int y)
        {
            hasSelectedCell = true;
            selectedX = x;
            selectedY = y;
        }

        public void ClearSelection()
        {
            hasSelectedCell = false;
            selectedX = 0;
            selectedY = 0;
        }

        public void ClearSelectedLayers()
        {
            selectedUnderlayId = 0;
            selectedTileId = 0;
            selectedOverlayId = 0;
            selectedCellArtId = 0;
        }

        public bool HasAnySelectedLayer()
        {
            return selectedUnderlayId > 0 || selectedTileId > 0 || selectedOverlayId > 0 || selectedCellArtId > 0;
        }

        public void SetSelectedLayer(BoardLayer layer, int contentId)
        {
            switch (layer)
            {
                case BoardLayer.Underlay:
                    SelectedUnderlayId = contentId;
                    break;
                case BoardLayer.Tile:
                    SelectedTileId = contentId;
                    break;
                case BoardLayer.Overlay:
                    SelectedOverlayId = contentId;
                    break;
            }
        }

        public void SetActiveSection(LevelEditorPaletteSectionType sectionType)
        {
            activeSectionType = sectionType;
        }

        public int GetSelectedContentIdForActiveLayer()
        {
            if (IsCellArtSectionActive)
            {
                return selectedCellArtId;
            }

            switch (ActiveBoardLayer)
            {
                case BoardLayer.Underlay:
                    return selectedUnderlayId;
                case BoardLayer.Tile:
                    return selectedTileId;
                case BoardLayer.Overlay:
                    return selectedOverlayId;
                default:
                    return 0;
            }
        }

        public static BoardLayer ResolveBoardLayer(LevelEditorPaletteSectionType sectionType)
        {
            switch (sectionType)
            {
                case LevelEditorPaletteSectionType.Overlay:
                    return BoardLayer.Overlay;
                case LevelEditorPaletteSectionType.Underlay:
                case LevelEditorPaletteSectionType.CellArt:
                    return BoardLayer.Underlay;
                case LevelEditorPaletteSectionType.ItemNormal:
                case LevelEditorPaletteSectionType.ItemSpecial:
                default:
                    return BoardLayer.Tile;
            }
        }
    }
}
