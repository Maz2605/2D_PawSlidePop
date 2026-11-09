using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Contracts;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Model;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Services
{
    public sealed class LevelBoardFillService
    {
        public bool TryFill(LevelEditorBoardState board, LevelGenerationRequest request, out string error)
        {
            error = null;
            List<int> allowed = BuildAllowedBaseTileIds(request);
            if (allowed.Count == 0)
            {
                error = "No allowed base tile ids available for board fill.";
                return false;
            }

            for (int y = 0; y < board.height; y++)
            {
                for (int x = 0; x < board.width; x++)
                {
                    int candidate = PickTileId(board, x, y, allowed);
                    if (candidate <= 0)
                    {
                        error = $"Unable to pick a valid tile at ({x},{y}).";
                        return false;
                    }

                    board.tileLayout[board.ToIndex(x, y)] = candidate;
                }
            }

            if (request.requireAllAllowedSpawnableAppearAtLeastOnce && board.CellCount >= allowed.Count)
            {
                EnsureAllAllowedTilesAppear(board, allowed);
            }

            return true;
        }

        public bool EnsureRequiredSpawnablesRemainPresent(LevelEditorBoardState board, LevelGenerationRequest request)
        {
            if (!request.requireAllAllowedSpawnableAppearAtLeastOnce)
            {
                return true;
            }

            List<int> allowed = BuildAllowedBaseTileIds(request);
            if (allowed.Count == 0 || board.CellCount < allowed.Count)
            {
                return true;
            }

            HashSet<int> present = new HashSet<int>(board.tileLayout);
            for (int i = 0; i < allowed.Count; i++)
            {
                int requiredTileId = allowed[i];
                if (present.Contains(requiredTileId))
                {
                    continue;
                }

                if (!TryInsertMissingTile(board, requiredTileId))
                {
                    return false;
                }

                present.Add(requiredTileId);
            }

            return true;
        }

        private static List<int> BuildAllowedBaseTileIds(LevelGenerationRequest request)
        {
            HashSet<int> disallowed = new HashSet<int>(request.disallowedInitialTileIds ?? new List<int>());
            List<int> allowed = new List<int>();

            if (request.allowedSpawnableTileIds == null)
            {
                return allowed;
            }

            for (int i = 0; i < request.allowedSpawnableTileIds.Count; i++)
            {
                int tileId = request.allowedSpawnableTileIds[i];
                if (tileId > 0 && !disallowed.Contains(tileId) && !allowed.Contains(tileId))
                {
                    allowed.Add(tileId);
                }
            }

            return allowed;
        }

        private static int PickTileId(LevelEditorBoardState board, int x, int y, IReadOnlyList<int> allowed)
        {
            int count = allowed.Count;
            int startOffset = ((y * 3) + x) % count;
            for (int offset = 0; offset < count; offset++)
            {
                int candidate = allowed[(startOffset + offset) % count];
                if (!CreatesMatch(board, x, y, candidate))
                {
                    return candidate;
                }
            }

            return allowed[startOffset];
        }

        private static bool CreatesMatch(LevelEditorBoardState board, int x, int y, int candidate)
        {
            if (x >= 2)
            {
                int leftIndex = board.ToIndex(x - 1, y);
                int left2Index = board.ToIndex(x - 2, y);
                if (board.tileLayout[leftIndex] == candidate && board.tileLayout[left2Index] == candidate)
                {
                    return true;
                }
            }

            if (y >= 2)
            {
                int downIndex = board.ToIndex(x, y - 1);
                int down2Index = board.ToIndex(x, y - 2);
                if (board.tileLayout[downIndex] == candidate && board.tileLayout[down2Index] == candidate)
                {
                    return true;
                }
            }

            return false;
        }

        private static void EnsureAllAllowedTilesAppear(LevelEditorBoardState board, IReadOnlyList<int> allowed)
        {
            HashSet<int> present = new HashSet<int>(board.tileLayout);
            int replacementIndex = 0;
            for (int i = 0; i < allowed.Count; i++)
            {
                int tileId = allowed[i];
                if (present.Contains(tileId))
                {
                    continue;
                }

                int index = replacementIndex % board.tileLayout.Length;
                board.tileLayout[index] = tileId;
                present.Add(tileId);
                replacementIndex += 7;
            }
        }

        private static bool TryInsertMissingTile(LevelEditorBoardState board, int requiredTileId)
        {
            for (int y = 0; y < board.height; y++)
            {
                for (int x = 0; x < board.width; x++)
                {
                    int index = board.ToIndex(x, y);
                    int previousTileId = board.tileLayout[index];
                    if (previousTileId == requiredTileId)
                    {
                        return true;
                    }

                    board.tileLayout[index] = requiredTileId;
                    bool createsMatch = CreatesMatchAt(board, x, y, requiredTileId);
                    board.tileLayout[index] = previousTileId;

                    if (!createsMatch)
                    {
                        board.tileLayout[index] = requiredTileId;
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool CreatesMatchAt(LevelEditorBoardState board, int x, int y, int candidate)
        {
            int horizontal = 1;
            int vertical = 1;

            horizontal += CountDirection(board, x, y, -1, 0, candidate);
            horizontal += CountDirection(board, x, y, 1, 0, candidate);
            vertical += CountDirection(board, x, y, 0, -1, candidate);
            vertical += CountDirection(board, x, y, 0, 1, candidate);

            return horizontal >= 3 || vertical >= 3;
        }

        private static int CountDirection(LevelEditorBoardState board, int x, int y, int stepX, int stepY, int candidate)
        {
            int count = 0;
            int currentX = x + stepX;
            int currentY = y + stepY;
            while (board.IsInBounds(currentX, currentY) && board.tileLayout[board.ToIndex(currentX, currentY)] == candidate)
            {
                count++;
                currentX += stepX;
                currentY += stepY;
            }

            return count;
        }
    }
}
