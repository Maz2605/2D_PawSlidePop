using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Model;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.UI.Panels
{
    public sealed class LevelEditorSpawnSettingsPanel : MonoBehaviour
    {
        [Header("Balancing Settings")]
        [SerializeField] private Toggle dynamicBalancingToggle;
        [SerializeField] private TMP_InputField targetBiasInput;

        [Header("Scroll List References")]
        [SerializeField] private Transform listContent;
        [SerializeField] private LevelEditorSpawnTileRowView rowTemplate;

        private LevelEditorUIController _controller;
        private bool _isUpdatingUI;

        public void Bind(LevelEditorUIController controller)
        {
            _controller = controller;
            if (_controller != null)
            {
                _controller.Changed -= Refresh;
                _controller.Changed += Refresh;
            }
            
            // Bind Balancing Settings
            if (dynamicBalancingToggle != null)
            {
                dynamicBalancingToggle.onValueChanged.RemoveAllListeners();
                dynamicBalancingToggle.onValueChanged.AddListener(HandleBalancingSettingsChanged);
            }

            if (targetBiasInput != null)
            {
                targetBiasInput.onEndEdit.RemoveAllListeners();
                targetBiasInput.onEndEdit.AddListener(HandleBiasInputEndEdit);
            }

            Refresh();
        }

        private void Start()
        {
            if (_controller == null)
            {
                var controller = FindFirstObjectByType<LevelEditorUIController>();
                if (controller != null)
                {
                    Bind(controller);
                }
            }
        }

        private void OnDestroy()
        {
            if (_controller != null)
            {
                _controller.Changed -= Refresh;
            }
        }

        private void HandleBalancingSettingsChanged(bool isOn)
        {
            if (_isUpdatingUI) return;
            CommitBalancingSettings();
        }

        private void HandleBiasInputEndEdit(string text)
        {
            if (_isUpdatingUI) return;
            CommitBalancingSettings();
        }

        private void CommitBalancingSettings()
        {
            if (_controller == null) return;
            bool enableDynamic = dynamicBalancingToggle != null ? dynamicBalancingToggle.isOn : true;
            float bias = 1.25f;
            if (targetBiasInput != null && float.TryParse(targetBiasInput.text, out float parsedBias))
            {
                bias = Mathf.Max(1f, parsedBias);
            }
            _controller.UpdateBalancingSettings(enableDynamic, bias);
        }

        private void Refresh()
        {
            if (_controller?.Session == null || _isUpdatingUI)
            {
                return;
            }

            _isUpdatingUI = true;

            // Update Balancing Fields
            if (dynamicBalancingToggle != null)
            {
                dynamicBalancingToggle.SetIsOnWithoutNotify(_controller.Session.enableDynamicBalancing);
            }
            if (targetBiasInput != null)
            {
                targetBiasInput.text = _controller.Session.targetSpawnBias.ToString("F2");
            }

            // Refresh Weights List
            if (listContent != null && rowTemplate != null)
            {
                ClearList();

                List<LevelEditorPaletteEntryData> normalTiles = _controller.GetPaletteEntries(LevelEditorPaletteSectionType.ItemNormal);
                List<LevelSpawnableTileConfig> currentConfigs = _controller.Session.spawnableTileConfigs;
                List<int> currentIds = _controller.Session.spawnableTileIds;

                for (int i = 0; i < normalTiles.Count; i++)
                {
                    LevelEditorPaletteEntryData entry = normalTiles[i];
                    if (entry == null) continue;

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
                        isEnabled = currentIds.Contains(entry.Id);
                        weight = 100;
                    }

                    LevelEditorSpawnTileRowView row = Instantiate(rowTemplate, listContent);
                    row.gameObject.SetActive(true);
                    row.Bind(entry, isEnabled, weight, HandleTileConfigChanged);
                }

                rowTemplate.gameObject.SetActive(false);
            }

            _isUpdatingUI = false;
        }

        private void HandleTileConfigChanged(int tileId, bool enabled, int weight)
        {
            _controller?.UpdateSpawnTileConfig(tileId, enabled, weight);
        }

        private void ClearList()
        {
            if (listContent == null) return;
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
            if (target == null) return;
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
