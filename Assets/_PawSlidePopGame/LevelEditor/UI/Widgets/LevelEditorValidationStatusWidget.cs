using System.Text;
using TMPro;
using UnityEngine;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Validation;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.UI
{
    public sealed class LevelEditorValidationStatusWidget : MonoBehaviour
    {
        [SerializeField] private TMP_Text validationStatusText;

        public void Refresh(Match3LevelDataValidationResult validationResult)
        {
            if (validationStatusText == null)
            {
                return;
            }

            validationStatusText.text = BuildSummary(validationResult);
        }

        private static string BuildSummary(Match3LevelDataValidationResult validationResult)
        {
            if (validationResult == null)
            {
                return "Validation: not run";
            }

            StringBuilder builder = new StringBuilder();
            builder.Append("Validation: ");
            builder.Append(validationResult.IsValid ? "Valid" : "Invalid");
            builder.Append($" | Errors: {validationResult.ErrorCount}");
            builder.Append($" | Warnings: {validationResult.WarningCount}");
            return builder.ToString();
        }
    }
}
