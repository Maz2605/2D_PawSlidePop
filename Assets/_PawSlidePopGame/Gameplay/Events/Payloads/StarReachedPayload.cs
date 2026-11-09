namespace _PawSlidePopGame._Scripts.Data.Events.Payloads
{
    public readonly struct StarReachedPayload
    {
        public int StarIndex { get; }
        public int CurrentScore { get; }

        public StarReachedPayload(int starIndex, int currentScore)
        {
            StarIndex = starIndex;
            CurrentScore = currentScore;
        }
    }
}
