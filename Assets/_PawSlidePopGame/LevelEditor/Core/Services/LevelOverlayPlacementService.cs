using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Contracts;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Model;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Services
{
    public sealed class LevelOverlayPlacementService
    {
        public bool TryPlace(LevelEditorBoardState board, LevelGenerationRequest request, out string error, out int placedIceCount)
        {
            error = null;
            placedIceCount = 0;

            if (request.requiredOverlayPlacements == null || request.requiredOverlayPlacements.Count == 0)
            {
                return true;
            }

            int seedStep = 11;
            for (int requirementIndex = 0; requirementIndex < request.requiredOverlayPlacements.Count; requirementIndex++)
            {
                LevelOverlayRequirement requirement = request.requiredOverlayPlacements[requirementIndex];
                int placedCount = 0;
                for (int attempt = 0; attempt < board.CellCount && placedCount < requirement.count; attempt++)
                {
                    int index = (attempt * seedStep + requirementIndex * 5) % board.CellCount;
                    if (board.tileLayout[index] <= 0 || board.overlayLayout[index] != 0)
                    {
                        continue;
                    }

                    board.overlayLayout[index] = requirement.tileId;
                    placedCount++;
                }

                if (placedCount < requirement.count)
                {
                    error = $"Unable to place overlay {requirement.tileId} x{requirement.count}.";
                    return false;
                }

                if (requirement.tileId == 301)
                {
                    placedIceCount += placedCount;
                }
            }

            return true;
        }
    }
}
