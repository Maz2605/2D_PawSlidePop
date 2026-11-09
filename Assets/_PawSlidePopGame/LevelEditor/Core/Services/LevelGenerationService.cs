using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Contracts;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Model;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Persistence;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Validation;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Services
{
    public sealed class LevelGenerationService
    {
        private readonly LevelBoardFillService _fillService = new LevelBoardFillService();
        private readonly LevelOverlayPlacementService _overlayPlacementService = new LevelOverlayPlacementService();
        private readonly LevelBoardSanitizerService _sanitizerService = new LevelBoardSanitizerService();
        private readonly LevelAssemblyService _assemblyService = new LevelAssemblyService();
        private readonly LevelGenerationRuleSet _ruleSet = new LevelGenerationRuleSet();

        public LevelGenerationResult Generate(LevelGenerationRequest request, Match3TileDatabaseSO database)
        {
            LevelGenerationResult result = new LevelGenerationResult();
            LevelEditorBoardState board = new LevelEditorBoardState();
            board.Initialize(request.width, request.height);

            if (!_fillService.TryFill(board, request, out string fillError))
            {
                result.success = false;
                result.message = fillError;
                return result;
            }

            if (!_overlayPlacementService.TryPlace(board, request, out string overlayError, out int placedIceCount))
            {
                result.success = false;
                result.message = overlayError;
                return result;
            }

            if (!_sanitizerService.Sanitize(board, request, _ruleSet))
            {
                result.success = false;
                result.message = "Failed to sanitize generated board and remove initial matches.";
                return result;
            }

            if (!_fillService.EnsureRequiredSpawnablesRemainPresent(board, request))
            {
                result.success = false;
                result.message = "Failed to preserve required spawnable coverage after sanitization.";
                return result;
            }

            if (!_sanitizerService.Sanitize(board, request, _ruleSet))
            {
                result.success = false;
                result.message = "Failed to sanitize generated board after restoring required spawnables.";
                return result;
            }

            Match3LevelData levelData = _assemblyService.Build(board, request);
            Match3LevelDataValidationResult validation = Match3LevelDataValidator.Validate(levelData, database);

            result.levelData = levelData;
            result.document = LevelEditorDocumentFactory.Create(levelData);
            result.validationResult = validation;
            result.placedIceCount = placedIceCount;
            result.hasInitialMatches = _sanitizerService.HasAnyMatch(board);
            result.success = validation.IsValid && !result.hasInitialMatches;
            result.message = result.success ? "Level generation succeeded." : "Generated level failed validation.";
            return result;
        }
    }
}
