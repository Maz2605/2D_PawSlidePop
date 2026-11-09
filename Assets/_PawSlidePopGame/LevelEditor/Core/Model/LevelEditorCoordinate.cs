namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Model
{
    [System.Serializable]
    public readonly struct LevelEditorCoordinate
    {
        public LevelEditorCoordinate(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }
    }
}
