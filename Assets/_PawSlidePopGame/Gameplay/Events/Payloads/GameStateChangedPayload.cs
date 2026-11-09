using _PawSlidePopGame._Scripts.Core.System.GameFlow;

namespace _PawSlidePopGame._Scripts.Data.Events.Payloads
{
    public readonly struct GameStateChangedPayload
    {
        public GameState Previous { get; }
        public GameState Current { get; }

        public GameStateChangedPayload(GameState previous, GameState current)
        {
            Previous = previous;
            Current = current;
        }
    }
}
