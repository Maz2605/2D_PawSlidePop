using System;
using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Logic.Match
{
    [Serializable]
    public sealed class BoardMatchAnalysis
    {
        public static BoardMatchAnalysis Empty { get; } = new BoardMatchAnalysis(Array.Empty<MatchGroup>());

        public IReadOnlyList<MatchGroup> Groups { get; }
        public bool HasMatches => Groups.Count > 0;

        public BoardMatchAnalysis(IReadOnlyList<MatchGroup> groups)
        {
            Groups = groups ?? Array.Empty<MatchGroup>();
        }
    }

    [Serializable]
    public sealed class MatchGroup
    {
        public int Id { get; }
        public AnimalTileId AnimalId { get; }
        public IReadOnlyList<CellModel> Cells { get; }
        public IReadOnlyList<MatchRun> HorizontalRuns { get; }
        public IReadOnlyList<MatchRun> VerticalRuns { get; }
        public bool ContainsSquare2X2 { get; }
        public int ClusterSize { get; }
        public int MaxStraightRunLength { get; }

        public MatchGroup(
            int id,
            AnimalTileId animalId,
            IReadOnlyList<CellModel> cells,
            IReadOnlyList<MatchRun> horizontalRuns,
            IReadOnlyList<MatchRun> verticalRuns,
            bool containsSquare2X2)
        {
            Id = id;
            AnimalId = animalId;
            Cells = cells ?? Array.Empty<CellModel>();
            HorizontalRuns = horizontalRuns ?? Array.Empty<MatchRun>();
            VerticalRuns = verticalRuns ?? Array.Empty<MatchRun>();
            ContainsSquare2X2 = containsSquare2X2;
            ClusterSize = Cells.Count;

            int maxStraightRunLength = 0;
            for (int i = 0; i < HorizontalRuns.Count; i++)
            {
                maxStraightRunLength = Math.Max(maxStraightRunLength, HorizontalRuns[i].Length);
            }

            for (int i = 0; i < VerticalRuns.Count; i++)
            {
                maxStraightRunLength = Math.Max(maxStraightRunLength, VerticalRuns[i].Length);
            }

            MaxStraightRunLength = maxStraightRunLength;
        }
    }

    [Serializable]
    public sealed class MatchRun
    {
        public bool IsHorizontal { get; }
        public IReadOnlyList<CellModel> Cells { get; }
        public int Length => Cells.Count;

        public MatchRun(bool isHorizontal, IReadOnlyList<CellModel> cells)
        {
            IsHorizontal = isHorizontal;
            Cells = cells ?? Array.Empty<CellModel>();
        }
    }
}

