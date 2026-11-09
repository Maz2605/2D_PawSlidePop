using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Contracts;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Model;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Validation;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.Scene
{
    public sealed class LevelEditorSessionStateHolder : MonoBehaviour
    {
        [SerializeField] private string currentLevelId = "Level_001";
        [SerializeField] private int displayLevelNumber = 1;
        [SerializeField] private int width = 8;
        [SerializeField] private int height = 8;
        [SerializeField] private int movesLimit = 26;
        [SerializeField] private LevelEditorSessionContext context = new LevelEditorSessionContext();

        public LevelEditorSessionContext Context => context;
        public string CurrentLevelId => currentLevelId;
        public int DisplayLevelNumber => displayLevelNumber;
        public int Width => width;
        public int Height => height;
        public int MovesLimit => movesLimit;

        public void Apply(LevelGenerationRequest request)
        {
            if (request == null)
            {
                return;
            }

            currentLevelId = request.levelId;
            displayLevelNumber = request.displayLevelNumber > 0 ? request.displayLevelNumber : 1;
            width = request.width;
            height = request.height;
            movesLimit = request.movesLimit;

            context.levelId = currentLevelId;
            context.displayLevelNumber = displayLevelNumber;
            context.movesLimit = movesLimit;
            if (context.board == null || context.board.width != width || context.board.height != height)
            {
                context.board = new LevelEditorBoardState();
                context.board.Initialize(width, height);
            }

            EnsureCellArtLayout();
        }

        public void LoadContext(LevelEditorSessionContext sessionContext)
        {
            context = sessionContext ?? new LevelEditorSessionContext();
            if (context.board == null)
            {
                context.board = new LevelEditorBoardState();
                context.board.Initialize(width, height);
            }

            EnsureCellArtLayout();

            currentLevelId = string.IsNullOrWhiteSpace(context.levelId) ? currentLevelId : context.levelId;
            displayLevelNumber = context.displayLevelNumber > 0 ? context.displayLevelNumber : 1;
            width = context.board.width > 0 ? context.board.width : width;
            height = context.board.height > 0 ? context.board.height : height;
            movesLimit = context.movesLimit > 0 ? context.movesLimit : movesLimit;
        }

        public void SetMetadata(string levelId, int levelNumber, int moveLimit)
        {
            currentLevelId = string.IsNullOrWhiteSpace(levelId) ? currentLevelId : levelId.Trim();
            displayLevelNumber = levelNumber > 0 ? levelNumber : 1;
            movesLimit = moveLimit > 0 ? moveLimit : 1;

            context.levelId = currentLevelId;
            context.displayLevelNumber = displayLevelNumber;
            context.movesLimit = movesLimit;
            context.isDirty = true;
        }

        public void MarkDirty(string statusMessage)
        {
            context.isDirty = true;
            SetStatus(statusMessage, string.Empty);
        }

        public void MarkSaved(string savedLevelId, Match3LevelDataValidationResult validationResult)
        {
            currentLevelId = savedLevelId;
            context.levelId = savedLevelId;
            context.isDirty = false;
            context.lastValidationResult = validationResult;
            SetStatus($"Saved level '{savedLevelId}'.", string.Empty);
        }

        public void SetValidation(Match3LevelDataValidationResult validationResult)
        {
            context.lastValidationResult = validationResult;
        }

        public void SetStatus(string statusMessage, string errorMessage)
        {
            context.statusMessage = statusMessage ?? string.Empty;
            context.errorMessage = errorMessage ?? string.Empty;
        }

        private void EnsureCellArtLayout()
        {
            int cellCount = context.board != null ? context.board.CellCount : 0;
            if (cellCount <= 0)
            {
                context.cellArtLayout = System.Array.Empty<int>();
                return;
            }

            if (context.cellArtLayout == null || context.cellArtLayout.Length != cellCount)
            {
                context.cellArtLayout = new int[cellCount];
            }
        }
    }
}
