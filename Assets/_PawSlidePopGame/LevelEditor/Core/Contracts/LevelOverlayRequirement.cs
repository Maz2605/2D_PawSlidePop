namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Contracts
{
    [System.Serializable]
    public struct LevelOverlayRequirement
    {
        public int tileId;
        public int count;

        public LevelOverlayRequirement(int tileId, int count)
        {
            this.tileId = tileId;
            this.count = count;
        }
    }
}
