using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Entities;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Logic.Match
{
    public class BoardMatchFinder : IBoardMatchRule
    {
        public BoardMatchAnalysis Analyze(BoardModel board)
        {
            if (board == null)
            {
                return BoardMatchAnalysis.Empty;
            }

            List<MatchRun> horizontalRuns = FindRuns(board, true);
            List<MatchRun> verticalRuns = FindRuns(board, false);
            HashSet<CellModel> matchedCells = BuildMatchedCells(board, horizontalRuns, verticalRuns);
            if (matchedCells.Count == 0)
            {
                return BoardMatchAnalysis.Empty;
            }

            Dictionary<CellModel, List<MatchRun>> runsByCell = BuildRunsByCell(horizontalRuns, verticalRuns);
            Dictionary<CellModel, int> groupIds = new Dictionary<CellModel, int>();
            List<MatchGroup> groups = new List<MatchGroup>();
            int nextGroupId = 1;
            List<CellModel> orderedMatchedCells = new List<CellModel>(matchedCells);
            orderedMatchedCells.Sort(CompareCells);

            foreach (CellModel startCell in orderedMatchedCells)
            {
                if (groupIds.ContainsKey(startCell))
                {
                    continue;
                }

                MatchGroup group = BuildGroup(board, startCell, matchedCells, runsByCell, groupIds, nextGroupId++);
                if (group != null)
                {
                    groups.Add(group);
                }
            }

            groups.Sort((left, right) => CompareCells(left.Cells[0], right.Cells[0]));
            return groups.Count > 0 ? new BoardMatchAnalysis(groups) : BoardMatchAnalysis.Empty;
        }

        private static List<MatchRun> FindRuns(BoardModel board, bool isHorizontal)
        {
            List<MatchRun> runs = new List<MatchRun>();
            int primary = isHorizontal ? board.Height : board.Width;
            int secondary = isHorizontal ? board.Width : board.Height;

            for (int line = 0; line < primary; line++)
            {
                int runStart = 0;
                while (runStart < secondary)
                {
                    int runLength = GetRunLength(board, isHorizontal, line, runStart);
                    if (runLength >= 3)
                    {
                        List<CellModel> runCells = new List<CellModel>(runLength);
                        for (int offset = 0; offset < runLength; offset++)
                        {
                            int x = isHorizontal ? runStart + offset : line;
                            int y = isHorizontal ? line : runStart + offset;
                            runCells.Add(board.GetCell(x, y));
                        }

                        runs.Add(new MatchRun(isHorizontal, runCells));
                    }

                    runStart += runLength > 0 ? runLength : 1;
                }
            }

            return runs;
        }

        private static HashSet<CellModel> BuildMatchedCells(
            BoardModel board,
            IEnumerable<MatchRun> horizontalRuns,
            IEnumerable<MatchRun> verticalRuns)
        {
            HashSet<CellModel> matchedCells = new HashSet<CellModel>();
            AddRunCells(matchedCells, horizontalRuns);
            AddRunCells(matchedCells, verticalRuns);
            AddSquareCells(board, matchedCells);
            return matchedCells;
        }

        private static void AddRunCells(HashSet<CellModel> matchedCells, IEnumerable<MatchRun> runs)
        {
            foreach (MatchRun run in runs)
            {
                for (int i = 0; i < run.Cells.Count; i++)
                {
                    if (run.Cells[i] != null)
                    {
                        matchedCells.Add(run.Cells[i]);
                    }
                }
            }
        }

        private static void AddSquareCells(BoardModel board, HashSet<CellModel> matchedCells)
        {
            if (board == null)
            {
                return;
            }

            for (int y = 0; y < board.Height - 1; y++)
            {
                for (int x = 0; x < board.Width - 1; x++)
                {
                    CellModel topLeft = board.GetCell(x, y);
                    CellModel topRight = board.GetCell(x + 1, y);
                    CellModel bottomLeft = board.GetCell(x, y + 1);
                    CellModel bottomRight = board.GetCell(x + 1, y + 1);

                    if (!FormsSquare(topLeft, topRight, bottomLeft, bottomRight))
                    {
                        continue;
                    }

                    matchedCells.Add(topLeft);
                    matchedCells.Add(topRight);
                    matchedCells.Add(bottomLeft);
                    matchedCells.Add(bottomRight);
                }
            }
        }

        private static int GetRunLength(BoardModel board, bool isHorizontal, int line, int start)
        {
            int x = isHorizontal ? start : line;
            int y = isHorizontal ? line : start;
            CellModel seedCell = board.GetCell(x, y);
            TileModel seed = seedCell != null && seedCell.CanTileMatch() ? seedCell.Tile : null;
            if (!IsMatchable(seed))
            {
                return 0;
            }

            int limit = isHorizontal ? board.Width : board.Height;
            int length = 1;
            for (int index = start + 1; index < limit; index++)
            {
                int candidateX = isHorizontal ? index : line;
                int candidateY = isHorizontal ? line : index;
                CellModel candidateCell = board.GetCell(candidateX, candidateY);
                TileModel candidate = candidateCell != null && candidateCell.CanTileMatch() ? candidateCell.Tile : null;
                if (!seed.IsMatchableWith(candidate))
                {
                    break;
                }

                length++;
            }

            return length;
        }

        private static Dictionary<CellModel, List<MatchRun>> BuildRunsByCell(IEnumerable<MatchRun> horizontalRuns, IEnumerable<MatchRun> verticalRuns)
        {
            Dictionary<CellModel, List<MatchRun>> runsByCell = new Dictionary<CellModel, List<MatchRun>>();
            AddRuns(runsByCell, horizontalRuns);
            AddRuns(runsByCell, verticalRuns);
            return runsByCell;
        }

        private static void AddRuns(Dictionary<CellModel, List<MatchRun>> runsByCell, IEnumerable<MatchRun> runs)
        {
            foreach (MatchRun run in runs)
            {
                for (int i = 0; i < run.Cells.Count; i++)
                {
                    CellModel cell = run.Cells[i];
                    if (!runsByCell.TryGetValue(cell, out List<MatchRun> cellRuns))
                    {
                        cellRuns = new List<MatchRun>();
                        runsByCell[cell] = cellRuns;
                    }

                    cellRuns.Add(run);
                }
            }
        }

        private static MatchGroup BuildGroup(
            BoardModel board,
            CellModel startCell,
            HashSet<CellModel> matchedCells,
            Dictionary<CellModel, List<MatchRun>> runsByCell,
            Dictionary<CellModel, int> groupIds,
            int groupId)
        {
            if (startCell?.Tile == null || !startCell.CanTileMatch() || !(startCell.Tile.Definition is NormalAnimalTileDefinitionSO definition))
            {
                return null;
            }

            Queue<CellModel> queue = new Queue<CellModel>();
            List<CellModel> cells = new List<CellModel>();
            HashSet<MatchRun> horizontalRuns = new HashSet<MatchRun>();
            HashSet<MatchRun> verticalRuns = new HashSet<MatchRun>();
            AnimalTileId animalId = definition.AnimalId;

            queue.Enqueue(startCell);
            groupIds[startCell] = groupId;

            while (queue.Count > 0)
            {
                CellModel cell = queue.Dequeue();
                cells.Add(cell);

                if (runsByCell.TryGetValue(cell, out List<MatchRun> cellRuns))
                {
                    for (int i = 0; i < cellRuns.Count; i++)
                    {
                        MatchRun run = cellRuns[i];
                        if (run.IsHorizontal)
                        {
                            horizontalRuns.Add(run);
                        }
                        else
                        {
                            verticalRuns.Add(run);
                        }
                    }
                }

                TryEnqueueNeighbor(board, cell.X + 1, cell.Y, animalId, matchedCells, groupIds, groupId, queue);
                TryEnqueueNeighbor(board, cell.X - 1, cell.Y, animalId, matchedCells, groupIds, groupId, queue);
                TryEnqueueNeighbor(board, cell.X, cell.Y + 1, animalId, matchedCells, groupIds, groupId, queue);
                TryEnqueueNeighbor(board, cell.X, cell.Y - 1, animalId, matchedCells, groupIds, groupId, queue);
            }

            cells.Sort(CompareCells);

            List<MatchRun> orderedHorizontalRuns = new List<MatchRun>(horizontalRuns);
            orderedHorizontalRuns.Sort(CompareRuns);
            List<MatchRun> orderedVerticalRuns = new List<MatchRun>(verticalRuns);
            orderedVerticalRuns.Sort(CompareRuns);

            return new MatchGroup(
                groupId,
                animalId,
                cells,
                orderedHorizontalRuns,
                orderedVerticalRuns,
                DetectSquare2X2(cells, animalId));
        }

        private static void TryEnqueueNeighbor(
            BoardModel board,
            int x,
            int y,
            AnimalTileId animalId,
            HashSet<CellModel> matchedCells,
            Dictionary<CellModel, int> groupIds,
            int groupId,
            Queue<CellModel> queue)
        {
            CellModel neighbor = board.GetCell(x, y);
            if (neighbor?.Tile == null || !neighbor.CanTileMatch() || groupIds.ContainsKey(neighbor) || !matchedCells.Contains(neighbor))
            {
                return;
            }

            if (!(neighbor.Tile.Definition is NormalAnimalTileDefinitionSO definition) || definition.AnimalId != animalId)
            {
                return;
            }

            groupIds[neighbor] = groupId;
            queue.Enqueue(neighbor);
        }

        private static bool DetectSquare2X2(IReadOnlyList<CellModel> cells, AnimalTileId animalId)
        {
            HashSet<CellModel> lookup = new HashSet<CellModel>(cells);
            for (int i = 0; i < cells.Count; i++)
            {
                CellModel topLeft = cells[i];
                CellModel topRight = FindCell(lookup, topLeft.X + 1, topLeft.Y);
                CellModel bottomLeft = FindCell(lookup, topLeft.X, topLeft.Y + 1);
                CellModel bottomRight = FindCell(lookup, topLeft.X + 1, topLeft.Y + 1);

                if (topRight == null || bottomLeft == null || bottomRight == null)
                {
                    continue;
                }

                if (IsAnimal(topLeft, animalId) && IsAnimal(topRight, animalId) && IsAnimal(bottomLeft, animalId) && IsAnimal(bottomRight, animalId))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool FormsSquare(CellModel topLeft, CellModel topRight, CellModel bottomLeft, CellModel bottomRight)
        {
            if (topLeft == null || topRight == null || bottomLeft == null || bottomRight == null ||
                !topLeft.CanTileMatch() ||
                !topRight.CanTileMatch() ||
                !bottomLeft.CanTileMatch() ||
                !bottomRight.CanTileMatch())
            {
                return false;
            }

            return topLeft.Tile.IsMatchableWith(topRight.Tile) &&
                   topLeft.Tile.IsMatchableWith(bottomLeft.Tile) &&
                   topLeft.Tile.IsMatchableWith(bottomRight.Tile);
        }

        private static CellModel FindCell(IEnumerable<CellModel> cells, int x, int y)
        {
            foreach (CellModel cell in cells)
            {
                if (cell.X == x && cell.Y == y)
                {
                    return cell;
                }
            }

            return null;
        }

        private static bool IsAnimal(CellModel cell, AnimalTileId animalId)
        {
            return cell?.Tile?.Definition is NormalAnimalTileDefinitionSO definition && definition.AnimalId == animalId;
        }

        private static int CompareCells(CellModel left, CellModel right)
        {
            int byY = left.Y.CompareTo(right.Y);
            return byY != 0 ? byY : left.X.CompareTo(right.X);
        }

        private static int CompareRuns(MatchRun left, MatchRun right)
        {
            if (left.Cells.Count == 0 || right.Cells.Count == 0)
            {
                return left.Cells.Count.CompareTo(right.Cells.Count);
            }

            return CompareCells(left.Cells[0], right.Cells[0]);
        }

        private static bool IsMatchable(TileModel tile)
        {
            return tile != null && tile.CanMatch();
        }
    }
}

