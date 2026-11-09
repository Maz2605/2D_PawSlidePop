namespace _PawSlidePopGame._Scripts.Data.Events.Payloads
{
    public readonly struct RemainingMovesChangedPayload
    {
        public int PreviousMoves { get; }
        public int CurrentMoves { get; }

        public RemainingMovesChangedPayload(int previousMoves, int currentMoves)
        {
            PreviousMoves = previousMoves;
            CurrentMoves = currentMoves;
        }
    }
}
