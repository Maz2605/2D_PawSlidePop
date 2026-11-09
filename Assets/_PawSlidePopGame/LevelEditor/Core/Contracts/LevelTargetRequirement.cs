namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Contracts
{
    [System.Serializable]
    public struct LevelTargetRequirement
    {
        public int tileId;
        public int requiredCount;

        public LevelTargetRequirement(int tileId, int requiredCount)
        {
            this.tileId = tileId;
            this.requiredCount = requiredCount;
        }
    }
}
