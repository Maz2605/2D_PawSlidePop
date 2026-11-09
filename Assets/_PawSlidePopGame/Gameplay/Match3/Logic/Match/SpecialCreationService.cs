using System;
using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Move;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Logic.Match
{
    public static class SpecialCreationService
    {
        public static List<SpecialSpawnDecision> CreateDecisions(
            BoardMatchAnalysis analysis,
            BoardMoveRequest moveRequest,
            Match3TileDatabaseSO tileDatabase)
        {
            List<SpecialSpawnDecision> decisions = new List<SpecialSpawnDecision>();
            if (analysis == null || !analysis.HasMatches || tileDatabase == null)
            {
                return decisions;
            }

            for (int i = 0; i < analysis.Groups.Count; i++)
            {
                MatchGroup group = analysis.Groups[i];
                TileLogicType? specialType = ResolveSpecialType(group);
                if (!specialType.HasValue)
                {
                    continue;
                }

                if (tileDatabase.GetBoosterDefinition(specialType.Value) == null)
                {
                    continue;
                }

                CellModel spawnCell = ChooseSpawnCell(group, moveRequest);
                if (spawnCell == null || spawnCell.Tile == null || !spawnCell.CanTileMatch())
                {
                    continue;
                }

                decisions.Add(new SpecialSpawnDecision(group.Id, specialType.Value, spawnCell));
            }

            return decisions;
        }

        private static TileLogicType? ResolveSpecialType(MatchGroup group)
        {
            if (group == null)
            {
                return null;
            }
            if (group.ClusterSize >= 6)
            {
                return TileLogicType.AreaBombLarge;
            }
            if (group.ClusterSize == 5)
            {
                return TileLogicType.AreaBombMedium;
            }
            if (group.ContainsSquare2X2)
            {
                return TileLogicType.SquareBomb;
            }
            if (group.MaxStraightRunLength == 4)
            {
                return TileLogicType.CrossBomb;
            }
            return null;
        }

        private static CellModel ChooseSpawnCell(MatchGroup group, BoardMoveRequest moveRequest)
        {
            if (group == null || group.Cells.Count == 0)
            {
                return null;
            }

            CellModel preferredCell = null;
            for (int i = 0; i < group.Cells.Count; i++)
            {
                CellModel cell = group.Cells[i];
                if (cell.X == moveRequest.SourceX && cell.Y == moveRequest.SourceY)
                {
                    preferredCell = cell;
                    break;
                }
            }

            if (preferredCell != null && preferredCell.Tile != null && preferredCell.CanTileMatch())
            {
                return preferredCell;
            }

            CellModel bestCell = null;
            int bestDistance = int.MaxValue;

            for (int i = 0; i < group.Cells.Count; i++)
            {
                CellModel cell = group.Cells[i];
                if (cell.Tile == null || !cell.CanTileMatch())
                {
                    continue;
                }

                int distance = moveRequest.HasSource
                    ? Math.Abs(cell.X - moveRequest.SourceX) + Math.Abs(cell.Y - moveRequest.SourceY)
                    : int.MaxValue - 1;

                if (bestCell == null || distance < bestDistance || (distance == bestDistance && IsPreferredTieBreaker(cell, bestCell)))
                {
                    bestCell = cell;
                    bestDistance = distance;
                }
            }

            return bestCell;
        }

        private static bool IsPreferredTieBreaker(CellModel candidate, CellModel current)
        {
            if (candidate.Y != current.Y)
            {
                return candidate.Y < current.Y;
            }

            return candidate.X < current.X;
        }
    }
}

