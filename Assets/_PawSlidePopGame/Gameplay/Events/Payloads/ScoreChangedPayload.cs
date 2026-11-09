namespace _PawSlidePopGame._Scripts.Data.Events.Payloads
{
    public readonly struct ScoreChangedPayload
    {
        public int PreviousScore { get; }
        public int CurrentScore { get; }
        public int Delta { get; }

        public ScoreChangedPayload(int previousScore, int currentScore, int delta)
        {
            PreviousScore = previousScore;
            CurrentScore = currentScore;
            Delta = delta;
        }
    }
}
