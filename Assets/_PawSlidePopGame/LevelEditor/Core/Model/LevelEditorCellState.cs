namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Model
{
    public readonly struct LevelEditorCellState
    {
        public LevelEditorCellState(LevelEditorCoordinate coordinate, int tileId, int overlayId, int underlayId, bool playable)
        {
            Coordinate = coordinate;
            TileId = tileId;
            OverlayId = overlayId;
            UnderlayId = underlayId;
            Playable = playable;
        }

        public LevelEditorCoordinate Coordinate { get; }
        public int TileId { get; }
        public int OverlayId { get; }
        public int UnderlayId { get; }
        public bool Playable { get; }
    }
}
