using _PawSlidePopGame._Scripts.Feature.Match3.Presentation;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Data.Events.Payloads
{
    public readonly struct TargetCollectedFxPayload
    {
        public int TileId { get; }
        public int TileInstanceId { get; }
        public BoardCellPosition Cell { get; }
        public Vector3 WorldPosition { get; }
        public int PreviousCount { get; }
        public int CurrentCount { get; }
        public int RequiredCount { get; }
        public bool JustCompleted { get; }

        public TargetCollectedFxPayload(
            int tileId,
            int tileInstanceId,
            BoardCellPosition cell,
            Vector3 worldPosition,
            int previousCount,
            int currentCount,
            int requiredCount,
            bool justCompleted)
        {
            TileId = tileId;
            TileInstanceId = tileInstanceId;
            Cell = cell;
            WorldPosition = worldPosition;
            PreviousCount = previousCount;
            CurrentCount = currentCount;
            RequiredCount = requiredCount;
            JustCompleted = justCompleted;
        }
    }
}
