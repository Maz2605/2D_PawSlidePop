using System.Collections.Generic;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.Validation
{
    public sealed class Match3LevelDataValidationResult
    {
        private readonly List<Match3LevelDataValidationIssue> _issues = new List<Match3LevelDataValidationIssue>();

        public IReadOnlyList<Match3LevelDataValidationIssue> Issues => _issues;
        public bool IsValid => ErrorCount == 0;
        public int ErrorCount { get; private set; }
        public int WarningCount { get; private set; }

        public void Add(Match3LevelDataValidationIssue issue)
        {
            if (issue == null)
            {
                return;
            }

            _issues.Add(issue);
            if (issue.Severity == Match3LevelDataValidationSeverity.Error)
            {
                ErrorCount++;
            }
            else if (issue.Severity == Match3LevelDataValidationSeverity.Warning)
            {
                WarningCount++;
            }
        }
    }
}
