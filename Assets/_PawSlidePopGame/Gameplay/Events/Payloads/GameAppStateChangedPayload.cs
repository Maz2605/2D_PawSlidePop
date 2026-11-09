using _PawSlidePopGame._Scripts.Core.System.GameFlow;

namespace _PawSlidePopGame._Scripts.Data.Events.Payloads
{
    public readonly struct GameAppStateChangedPayload
    {
        public GameAppState Previous { get; }
        public GameAppState Current { get; }

        public GameAppStateChangedPayload(GameAppState previous, GameAppState current)
        {
            Previous = previous;
            Current = current;
        }
    }
}
