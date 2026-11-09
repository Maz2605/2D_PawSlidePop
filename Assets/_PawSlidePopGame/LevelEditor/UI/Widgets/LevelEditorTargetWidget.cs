using System;
using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Contracts;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Model;
using UnityEngine;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.UI
{
    public sealed class LevelEditorTargetWidget : MonoBehaviour
    {
        [SerializeField] private Button btnAddTargets;
        [SerializeField] private LevelEditorItemTargetView[] targetSlots;
        [SerializeField] private EditorUIManager editorUIManager;
        [SerializeField] private LevelEditorTargetPickerPopup targetPickerPopupPrefab;

        private LevelEditorUIController _service;
        private readonly Dictionary<int, LevelEditorPaletteEntryData> _targetEntryLookup = new Dictionary<int, LevelEditorPaletteEntryData>();

        public void Bind(LevelEditorUIController service)
        {
            _service = service;

            if (btnAddTargets != null)
            {
                btnAddTargets.onClick.RemoveAllListeners();
                btnAddTargets.onClick.AddListener(HandleAddTargetClicked);
            }
        }

        public void Refresh()
        {
            if (_service?.Session == null)
            {
                return;
            }

            RebuildTargetLookup(_service.GetTargetOptions());

            IReadOnlyList<LevelTargetRequirement> targets = _service.Targets;
            int slotCount = targetSlots != null ? targetSlots.Length : 0;

            for (int i = 0; i < slotCount; i++)
            {
                LevelEditorItemTargetView slot = targetSlots[i];
                if (slot == null)
                {
                    continue;
                }

                if (i < targets.Count && _targetEntryLookup.TryGetValue(targets[i].tileId, out LevelEditorPaletteEntryData entry))
                {
                    slot.Bind(
                        entry,
                        targets[i].requiredCount,
                        i,
                        HandleEditTargetRequested,
                        HandleTargetCountChanged,
                        HandleRemoveTargetRequested);
                }
                else
                {
                    slot.ShowEmpty();
                }
            }

            if (btnAddTargets != null)
            {
                btnAddTargets.gameObject.SetActive(targets.Count < slotCount);
            }
        }

        private void HandleAddTargetClicked()
        {
            if (_service?.Session == null || targetSlots == null || _service.Targets.Count >= targetSlots.Length)
            {
                Debug.LogWarning("[LevelEditor] Add Target button ignored. No active session or target slots are full.");
                return;
            }

            Debug.Log($"[LevelEditor] Add Target button clicked. Current targets={_service.Targets.Count}/{targetSlots.Length}.");
            LevelEditorTargetPickerPopup popup = ShowPickerPopup();
            if (popup == null)
            {
                Debug.LogWarning("[LevelEditor] Add Target popup could not open. Check EditorUIManager and targetPickerPopupPrefab references.");
                return;
            }

            popup.ShowForAdd(
                _service.GetTargetOptions(),
                tileId => _service.AddTargetWithTile(tileId, 1),
                () => editorUIManager?.ClosePopup(popup));
        }

        private void HandleEditTargetRequested(int slotIndex)
        {
            if (_service?.Session == null || slotIndex < 0 || slotIndex >= _service.Targets.Count)
            {
                Debug.LogWarning($"[LevelEditor] Edit Target ignored. Invalid slot index {slotIndex}.");
                return;
            }

            Debug.Log($"[LevelEditor] Edit Target requested for slot #{slotIndex + 1}, tileId={_service.Targets[slotIndex].tileId}.");
            LevelEditorTargetPickerPopup popup = ShowPickerPopup();
            if (popup == null)
            {
                Debug.LogWarning("[LevelEditor] Edit Target popup could not open. Check EditorUIManager and targetPickerPopupPrefab references.");
                return;
            }

            popup.ShowForEdit(
                slotIndex,
                _service.Targets[slotIndex].tileId,
                _service.GetTargetOptions(),
                (index, tileId) => _service.UpdateTargetTile(index, tileId),
                () => editorUIManager?.ClosePopup(popup));
        }

        private void HandleTargetCountChanged(int slotIndex, int requiredCount)
        {
            Debug.Log($"[LevelEditor] Target count changed for slot #{slotIndex + 1}. Requested count={requiredCount}.");
            _service?.UpdateTargetCount(slotIndex, requiredCount);
        }

        private void HandleRemoveTargetRequested(int slotIndex)
        {
            Debug.Log($"[LevelEditor] Remove Target requested for slot #{slotIndex + 1}.");
            _service?.RemoveTarget(slotIndex);
        }

        private LevelEditorTargetPickerPopup ShowPickerPopup()
        {
            if (editorUIManager == null || targetPickerPopupPrefab == null)
            {
                return null;
            }

            return editorUIManager.ShowPopup(targetPickerPopupPrefab);
        }

        private void RebuildTargetLookup(IReadOnlyList<LevelEditorPaletteEntryData> options)
        {
            _targetEntryLookup.Clear();
            if (options == null)
            {
                return;
            }

            for (int i = 0; i < options.Count; i++)
            {
                _targetEntryLookup[options[i].Id] = options[i];
            }
        }
    }
}
