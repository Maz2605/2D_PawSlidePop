using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Model;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.Validation
{
    public enum Match3LevelDataValidationSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2
    }

    public sealed class Match3LevelDataValidationIssue
    {
        public Match3LevelDataValidationIssue(Match3LevelDataValidationSeverity severity, string code, string message, string fieldPath = null, int? cellIndex = null, LevelEditorCoordinate? coordinate = null)
        {
            Severity = severity;
            Code = code;
            Message = message;
            FieldPath = fieldPath ?? string.Empty;
            CellIndex = cellIndex;
            Coordinate = coordinate;
        }

        public Match3LevelDataValidationSeverity Severity { get; }
        public string Code { get; }
        public string Message { get; }
        public string FieldPath { get; }
        public int? CellIndex { get; }
        public LevelEditorCoordinate? Coordinate { get; }
    }
}
