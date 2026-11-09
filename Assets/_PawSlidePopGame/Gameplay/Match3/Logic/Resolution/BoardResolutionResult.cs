namespace _PawSlidePopGame._Scripts.Feature.Match3.Logic.Resolution
{
    public class BoardResolutionResult
    {
        public bool IsMoveAccepted { get; set; }
        public int CascadesResolved { get; set; }
        public int ClearedTiles { get; set; }
        public int SpawnedTiles { get; set; }
        public int ScoreDelta { get; set; }
        public bool HasTriggeredPostMoveRule { get; set; }
    }
}

