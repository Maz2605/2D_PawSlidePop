using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Model;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.UI
{
    public sealed class LevelEditorSpawnSettingsPopup : EditorPopup
    {
        [SerializeField] private Toggle dynamicBalancingToggle;
        [SerializeField] private TMP_InputField targetBiasInput;
        [SerializeField] private Transform listContent;
        [SerializeField] private LevelEditorSpawnTileRowView rowTemplate;
        [SerializeField] private Button closeButton;

        private LevelEditorUIController _service;
        private Action _onDismissRequested;

        public void Show(LevelEditorUIController service, Action onDismissRequested)
        {
            _service = service;
            _onDismissRequested = onDismissRequested;

            if (_service == null || _service.Session == null)
            {
                return;
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(() => _onDismissRequested?.Invoke());
            }

            // Bind Balancing Settings
            if (dynamicBalancingToggle != null)
            {
                dynamicBalancingToggle.onValueChanged.RemoveListener(HandleBalancingToggleChanged);
                dynamicBalancingToggle.isOn = _service.Session.enableDynamicBalancing;
                dynamicBalancingToggle.onValueChanged.AddListener(HandleBalancingToggleChanged);
            }

            if (targetBiasInput != null)
            {
                targetBiasInput.onEndEdit.RemoveListener(HandleBiasInputChanged);
                targetBiasInput.text = _service.Session.targetSpawnBias.ToString("F2");
                targetBiasInput.onEndEdit.AddListener(HandleBiasInputChanged);
            }

            base.Show();
            RefreshList();
        }

        private void HandleBalancingToggleChanged(bool isOn)
        {
            UpdateBalancingSettings();
        }

        private void HandleBiasInputChanged(string text)
        {
            UpdateBalancingSettings();
        }

        private void UpdateBalancingSettings()
        {
            if (_service == null)
            {
                return;
            }

            bool enableDynamic = dynamicBalancingToggle != null ? dynamicBalancingToggle.isOn : true;
            float bias = 1.25f;
            if (targetBiasInput != null && float.TryParse(targetBiasInput.text, out float parsedBias))
            {
                bias = Mathf.Max(1f, parsedBias);
            }

            _service.UpdateBalancingSettings(enableDynamic, bias);
        }

        private void RefreshList()
        {
            if (_service == null || listContent == null || rowTemplate == null)
            {
                return;
            }

            ClearList();

            List<LevelEditorPaletteEntryData> normalTiles = _service.GetPaletteEntries(LevelEditorPaletteSectionType.ItemNormal);
            List<LevelSpawnableTileConfig> currentConfigs = _service.Session.spawnableTileConfigs;
            List<int> currentIds = _service.Session.spawnableTileIds;

            for (int i = 0; i < normalTiles.Count; i++)
            {
                LevelEditorPaletteEntryData entry = normalTiles[i];
                if (entry == null)
                {
                    continue;
                }

                // Determine enabled and weight
                bool isEnabled = false;
                int weight = 100;

                bool foundConfig = false;
                if (currentConfigs != null)
                {
                    for (int j = 0; j < currentConfigs.Count; j++)
                    {
                        if (currentConfigs[j].tileId == entry.Id)
                        {
                            isEnabled = currentConfigs[j].enabled;
                            weight = currentConfigs[j].weight;
                            foundConfig = true;
                            break;
                        }
                    }
                }

                if (!foundConfig && currentIds != null)
                {
                    // Fallback to checking if ID is in spawnableTileIds list
                    isEnabled = currentIds.Contains(entry.Id);
                    weight = 100;
                }

                LevelEditorSpawnTileRowView row = Instantiate(rowTemplate, listContent);
                row.gameObject.SetActive(true);
                row.Bind(entry, isEnabled, weight, HandleTileConfigChanged);
            }

            if (rowTemplate != null)
            {
                rowTemplate.gameObject.SetActive(false);
            }
        }

        private void HandleTileConfigChanged(int tileId, bool enabled, int weight)
        {
            _service?.UpdateSpawnTileConfig(tileId, enabled, weight);
        }

        private void ClearList()
        {
            for (int i = listContent.childCount - 1; i >= 0; i--)
            {
                Transform child = listContent.GetChild(i);
                if (child == rowTemplate.transform)
                {
                    continue;
                }

                DestroyHelper(child.gameObject);
            }
        }

        private static void DestroyHelper(GameObject target)
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
