using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Contracts;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Model;
using UnityEngine;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.UI
{
    public sealed class LevelEditorGoalsPresenter : MonoBehaviour
    {
        [SerializeField] private Transform goalListContent;
        [SerializeField] private LevelEditorGoalRowView goalRowTemplate;
        [SerializeField] private Button addGoalButton;

        private LevelEditorUIController _service;

        public void Bind(LevelEditorUIController service)
        {
            _service = service;
            if (addGoalButton != null)
            {
                addGoalButton.onClick.RemoveAllListeners();
                addGoalButton.onClick.AddListener(() => _service?.AddGoal());
            }
        }

        public void Refresh()
        {
            if (_service?.Session == null || goalListContent == null || goalRowTemplate == null)
            {
                return;
            }

            ClearRows();

            List<LevelEditorPaletteEntryData> goalOptions = _service.GetGoalOptions();
            List<(int id, string label)> options = new List<(int id, string label)>(goalOptions.Count);
            for (int i = 0; i < goalOptions.Count; i++)
            {
                options.Add((goalOptions[i].Id, goalOptions[i].Label));
            }

            IReadOnlyList<LevelTargetRequirement> goals = _service.Targets;
            for (int i = 0; i < goals.Count; i++)
            {
                int index = i;
                LevelEditorGoalRowView row = Instantiate(goalRowTemplate, goalListContent);
                row.gameObject.SetActive(true);
                row.Bind(
                    options,
                    goals[i].tileId,
                    goals[i].requiredCount,
                    (tileId, requiredCount) => _service.UpdateGoal(index, tileId, requiredCount),
                    () => _service.RemoveGoal(index));
            }

            goalRowTemplate.gameObject.SetActive(false);
        }

        private void ClearRows()
        {
            for (int i = goalListContent.childCount - 1; i >= 0; i--)
            {
                Transform child = goalListContent.GetChild(i);
                if (child == goalRowTemplate.transform)
                {
                    continue;
                }

                DestroyViewObject(child.gameObject);
            }
        }

        private static void DestroyViewObject(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
}
