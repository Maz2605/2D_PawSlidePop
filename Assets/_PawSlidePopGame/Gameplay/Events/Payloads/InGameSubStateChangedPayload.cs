using _PawSlidePopGame._Scripts.Core.System.GameFlow;

namespace _PawSlidePopGame._Scripts.Data.Events.Payloads
{
    public readonly struct InGameSubStateChangedPayload
    {
        public InGameSubState Previous { get; }
        public InGameSubState Current { get; }

        public InGameSubStateChangedPayload(InGameSubState previous, InGameSubState current)
        {
            Previous = previous;
            Current = current;
        }
    }
}
