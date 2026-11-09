namespace _PawSlidePopGame._Scripts.Core.System.GameFlow
{
    public enum InGameSubState
    {
        None = 0,
        Bootstrapping = 1,
        PreparingBoard = 2,
        PlayerTurn = 3,
        ResolvingBoard = 4,
        CheckingResult = 5,
        Victory = 6,
        Defeat = 7,
        Paused = 8,
        TargetingChargedPlacement = 9,
        TargetingChargedCombo = 10
    }
}
