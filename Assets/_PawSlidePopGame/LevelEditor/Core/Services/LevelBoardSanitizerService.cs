using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Contracts;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Model;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Services
{
    public sealed class LevelBoardSanitizerService
    {
        public bool Sanitize(LevelEditorBoardState board, LevelGenerationRequest request, LevelGenerationRuleSet ruleSet)
        {
            if (!request.requireNoInitialMatches)
            {
                return true;
            }

            List<int> allowed = new List<int>();
            for (int i = 0; i < request.allowedSpawnableTileIds.Count; i++)
            {
                int tileId = request.allowedSpawnableTileIds[i];
                if (tileId > 0 && !request.disallowedInitialTileIds.Contains(tileId) && !allowed.Contains(tileId))
                {
                    allowed.Add(tileId);
                }
            }

            for (int pass = 0; pass < ruleSet.MaxSanitizerPasses; pass++)
            {
                bool changed = false;
                for (int y = 0; y < board.height; y++)
                {
                    for (int x = 0; x < board.width; x++)
                    {
                        if (!IsPartOfMatch(board, x, y))
                        {
                            continue;
                        }

                        int index = board.ToIndex(x, y);
                        int replacement = FindReplacement(board, x, y, allowed, board.tileLayout[index]);
                        if (replacement > 0 && replacement != board.tileLayout[index])
                        {
                            board.tileLayout[index] = replacement;
                            changed = true;
                        }
                    }
                }

                if (!changed)
                {
                    return !HasAnyMatch(board);
                }
            }

            return !HasAnyMatch(board);
        }

        public bool HasAnyMatch(LevelEditorBoardState board)
        {
            for (int y = 0; y < board.height; y++)
            {
                for (int x = 0; x < board.width; x++)
                {
                    if (IsPartOfMatch(board, x, y))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsPartOfMatch(LevelEditorBoardState board, int x, int y)
        {
            int tileId = board.tileLayout[board.ToIndex(x, y)];
            if (tileId <= 0)
            {
                return false;
            }

            int horizontal = 1;
            int vertical = 1;

            horizontal += CountDirection(board, x, y, -1, 0, tileId);
            horizontal += CountDirection(board, x, y, 1, 0, tileId);
            vertical += CountDirection(board, x, y, 0, -1, tileId);
            vertical += CountDirection(board, x, y, 0, 1, tileId);

            return horizontal >= 3 || vertical >= 3;
        }

        private static int CountDirection(LevelEditorBoardState board, int x, int y, int stepX, int stepY, int tileId)
        {
            int count = 0;
            int currentX = x + stepX;
            int currentY = y + stepY;
            while (board.IsInBounds(currentX, currentY) && board.tileLayout[board.ToIndex(currentX, currentY)] == tileId)
            {
                count++;
                currentX += stepX;
                currentY += stepY;
            }

            return count;
        }

        private static int FindReplacement(LevelEditorBoardState board, int x, int y, IReadOnlyList<int> allowed, int currentTileId)
        {
            for (int i = 0; i < allowed.Count; i++)
            {
                int candidate = allowed[i];
                if (candidate == currentTileId)
                {
                    continue;
                }

                int index = board.ToIndex(x, y);
                int previous = board.tileLayout[index];
                board.tileLayout[index] = candidate;
                bool valid = !IsPartOfMatch(board, x, y);
                board.tileLayout[index] = previous;
                if (valid)
                {
                    return candidate;
                }
            }

            return currentTileId;
        }
    }
}
