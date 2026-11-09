using System.Text;
using TMPro;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.UI
{
    public sealed class LevelEditorStatusPresenter : MonoBehaviour
    {
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text errorText;
        [SerializeField] private TMP_Text validationStatusText;

        public void Refresh(LevelEditorUIController service)
        {
            if (service?.Session == null)
            {
                return;
            }

            if (statusText != null)
            {
                string baseStatus = service.Session.statusMessage ?? string.Empty;
                if (service.HoveredX >= 0 && service.HoveredY >= 0)
                {
                    statusText.text = string.IsNullOrEmpty(baseStatus) 
                        ? $"Cell: ({service.HoveredX}, {service.HoveredY})" 
                        : $"{baseStatus} | Cell: ({service.HoveredX}, {service.HoveredY})";
                }
                else
                {
                    statusText.text = baseStatus;
                }
            }

            if (errorText != null)
            {
                errorText.text = service.Session.errorMessage ?? string.Empty;
            }

            if (validationStatusText != null)
            {
                validationStatusText.text = BuildValidationSummary(service);
            }
        }

        private static string BuildValidationSummary(LevelEditorUIController service)
        {
            if (service?.Session?.lastValidationResult == null)
            {
                return "Validation: not run";
            }

            StringBuilder builder = new StringBuilder();
            builder.Append("Validation: ");
            builder.Append(service.Session.lastValidationResult.IsValid ? "Valid" : "Invalid");
            builder.Append($" | Errors: {service.Session.lastValidationResult.ErrorCount}");
            builder.Append($" | Warnings: {service.Session.lastValidationResult.WarningCount}");

            if (!service.Session.lastValidationResult.IsValid && service.Session.lastValidationResult.Issues.Count > 0)
            {
                builder.Append('\n');
                builder.Append(service.Session.lastValidationResult.Issues[0].Code);
                builder.Append(": ");
                builder.Append(service.Session.lastValidationResult.Issues[0].Message);
            }

            return builder.ToString();
        }
    }
}
