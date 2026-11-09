namespace _PawSlidePopGame._Scripts.Data.Events.Payloads
{
    public readonly struct TargetProgressChangedPayload
    {
        public int TileId { get; }
        public int PreviousCount { get; }
        public int CurrentCount { get; }
        public int RequiredCount { get; }
        public bool JustCompleted { get; }

        public TargetProgressChangedPayload(int tileId, int previousCount, int currentCount, int requiredCount, bool justCompleted)
        {
            TileId = tileId;
            PreviousCount = previousCount;
            CurrentCount = currentCount;
            RequiredCount = requiredCount;
            JustCompleted = justCompleted;
        }
    }
}
