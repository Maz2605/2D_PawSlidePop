using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using _PawSlidePopGame._Scripts.Data.LevelProvider;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.UI;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Model;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.UI.Panels
{
    public sealed class LevelEditorToolbarPresenter : MonoBehaviour
    {
        [SerializeField] private Button newButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button loadButton;
        [SerializeField] private TMP_InputField movesInput;
        [SerializeField] private TMP_InputField widthInput;
        [SerializeField] private TMP_InputField heightInput;
        [SerializeField] private LevelEditorLevelSelectorWidget levelSelectorWidget;
        [SerializeField] private LevelEditorValidationStatusWidget validationStatusWidget;
        [SerializeField] private LevelEditorTargetWidget targetWidget;
        [SerializeField] private TMP_Dropdown viewModeDropdown;
        [SerializeField] private Button spawnSettingsButton;
        [SerializeField] private LevelEditorSpawnSettingsPopup spawnSettingsPopupPrefab;
        [SerializeField] private EditorUIManager editorUIManager;

        private LevelEditorUIController _service;
        private string _lastSyncedSessionLevelId;

        public void Bind(LevelEditorUIController service)
        {
            _service = service;

            BindButton(newButton, HandleNewClicked);
            BindButton(saveButton, HandleSaveClicked);
            BindButton(loadButton, HandleLoadClicked);
            if (spawnSettingsButton != null)
            {
                spawnSettingsButton.onClick.RemoveAllListeners();
                spawnSettingsButton.onClick.AddListener(HandleSpawnSettingsClicked);
            }
            BindBoardSizeInput(widthInput);
            BindBoardSizeInput(heightInput);
            levelSelectorWidget?.Bind();
            _lastSyncedSessionLevelId = _service?.Session?.levelId;
            targetWidget?.Bind(service);

            if (viewModeDropdown != null)
            {
                viewModeDropdown.onValueChanged.RemoveListener(HandleViewModeChanged);
                viewModeDropdown.ClearOptions();
                var options = new System.Collections.Generic.List<TMP_Dropdown.OptionData>
                {
                    new TMP_Dropdown.OptionData("Show All"),
                    new TMP_Dropdown.OptionData("Normal"),
                    new TMP_Dropdown.OptionData("Overlay"),
                    new TMP_Dropdown.OptionData("Underlay")
                };
                viewModeDropdown.options = options;
                viewModeDropdown.value = (int)_service.ViewMode;
                viewModeDropdown.onValueChanged.AddListener(HandleViewModeChanged);
            }
        }

        public void Refresh()
        {
            if (_service?.Session == null)
            {
                return;
            }

            if (movesInput != null)
            {
                movesInput.SetTextWithoutNotify(_service.Session.movesLimit.ToString());
            }

            if (widthInput != null)
            {
                widthInput.SetTextWithoutNotify(_service.Session.board.width.ToString());
            }

            if (heightInput != null)
            {
                heightInput.SetTextWithoutNotify(_service.Session.board.height.ToString());
            }

            if (viewModeDropdown != null && _service != null)
            {
                viewModeDropdown.SetValueWithoutNotify((int)_service.ViewMode);
            }

            SyncLevelSelectorIfSessionChanged();
            validationStatusWidget?.Refresh(_service.Session.lastValidationResult);
            targetWidget?.Refresh();
        }

        private void HandleViewModeChanged(int val)
        {
            if (_service != null)
            {
                _service.ViewMode = (LevelEditorViewMode)val;
            }
        }

        private void ApplyBoardSizeFromInputs()
        {
            if (_service?.Session?.board == null)
            {
                return;
            }

            string levelId = ReadLevelId();
            int movesLimit = ReadInt(movesInput, 26);
            int width = ReadInt(widthInput, _service.Session.board.width);
            int height = ReadInt(heightInput, _service.Session.board.height);
            if (width == _service.Session.board.width && height == _service.Session.board.height)
            {
                _service.SetMetadata(levelId, ResolveDisplayLevelNumber(levelId), movesLimit);
                return;
            }

            if (!ConfirmResizeBoard(width, height))
            {
                Debug.LogWarning($"[LevelEditor] Board resize cancelled by user. Level='{levelId}', requested board={width}x{height}.");
                Refresh();
                return;
            }

            Debug.Log($"[LevelEditor] Board size input committed. Level='{levelId}', requested board={width}x{height}, moves={movesLimit}, dirty={_service.Session.isDirty}.");
            _service.SetMetadata(levelId, ResolveDisplayLevelNumber(levelId), movesLimit);
            _service.ResizeBoard(width, height);
        }

        private void HandleNewClicked()
        {
            string levelId = ReadLevelId();
            int width = ReadInt(widthInput, 8);
            int height = ReadInt(heightInput, 8);
            int movesLimit = ReadInt(movesInput, 26);
            Debug.Log($"[LevelEditor] New button clicked. Level='{levelId}', board={width}x{height}, moves={movesLimit}, currentDirty={(_service?.Session?.isDirty ?? false)}.");
            _service?.NewLevel(levelId, ResolveDisplayLevelNumber(levelId), width, height, movesLimit);
        }

        private void HandleSaveClicked()
        {
            string levelId = ReadLevelId();
            int movesLimit = ReadInt(movesInput, 26);
            Debug.Log($"[LevelEditor] Save button clicked. Level='{levelId}', moves={movesLimit}, dirty={(_service?.Session?.isDirty ?? false)}.");
            if (!ConfirmSaveOverwrite(levelId))
            {
                Debug.LogWarning($"[LevelEditor] Save cancelled by user. Level='{levelId}'.");
                return;
            }

            _service?.SetMetadata(levelId, ResolveDisplayLevelNumber(levelId), movesLimit);
            _service?.SaveLevel(true);
        }

        private void HandleLoadClicked()
        {
            string levelId = ReadLevelId();
            Debug.Log($"[LevelEditor] Load button clicked. Level='{levelId}', currentDirty={(_service?.Session?.isDirty ?? false)}.");
            if (!ConfirmLoadReplacingDirtySession(levelId))
            {
                Debug.LogWarning($"[LevelEditor] Load cancelled by user. Level='{levelId}'.");
                return;
            }

            _service?.LoadLevel(levelId);
        }

        private void HandleSpawnSettingsClicked()
        {
            if (_service?.Session == null || editorUIManager == null || spawnSettingsPopupPrefab == null)
            {
                Debug.LogWarning("[LevelEditor] Spawn settings button ignored or missing references.");
                return;
            }

            Debug.Log("[LevelEditor] Open Spawn Settings Popup.");
            LevelEditorSpawnSettingsPopup popup = editorUIManager.ShowPopup(spawnSettingsPopupPrefab);
            if (popup != null)
            {
                popup.Show(_service, () => editorUIManager.ClosePopup(popup));
            }
        }

        private string ReadLevelId()
        {
            return levelSelectorWidget != null ? levelSelectorWidget.CurrentLevelId : "Level_001";
        }

        private void SyncLevelSelectorIfSessionChanged()
        {
            if (levelSelectorWidget == null || _service?.Session == null)
            {
                return;
            }

            string sessionLevelId = _service.Session.levelId;
            bool sessionChanged = !string.Equals(_lastSyncedSessionLevelId, sessionLevelId, StringComparison.OrdinalIgnoreCase);
            if (!sessionChanged || levelSelectorWidget.IsEditingLevelId)
            {
                return;
            }

            levelSelectorWidget.SetLevelId(sessionLevelId);
            _lastSyncedSessionLevelId = levelSelectorWidget.CurrentLevelId;
        }

        private int ResolveDisplayLevelNumber(string levelId)
        {
            if (string.IsNullOrWhiteSpace(levelId))
            {
                return _service?.Session != null ? Mathf.Max(1, _service.Session.displayLevelNumber) : 1;
            }

            int digitStart = levelId.Length;
            for (int i = levelId.Length - 1; i >= 0; i--)
            {
                if (!char.IsDigit(levelId[i]))
                {
                    break;
                }

                digitStart = i;
            }

            if (digitStart < levelId.Length && int.TryParse(levelId.Substring(digitStart), out int parsedLevel))
            {
                return Mathf.Max(1, parsedLevel);
            }

            return _service?.Session != null ? Mathf.Max(1, _service.Session.displayLevelNumber) : 1;
        }

        private bool ConfirmLoadReplacingDirtySession(string levelId)
        {
#if UNITY_EDITOR
            if (_service?.Session == null || !_service.Session.isDirty)
            {
                return true;
            }

            string currentLevelId = string.IsNullOrWhiteSpace(_service.Session.levelId)
                ? "current level"
                : _service.Session.levelId;
            return UnityEditor.EditorUtility.DisplayDialog(
                "Unsaved Board Changes",
                $"Board '{currentLevelId}' has unsaved changes.\n\nLoading '{levelId}' will replace the current editor session. Save first if you want to keep these changes.",
                "Load Anyway",
                "Cancel");
#else
            return true;
#endif
        }

        private bool ConfirmSaveOverwrite(string levelId)
        {
#if UNITY_EDITOR
            string absolutePath = LevelPathUtility.GetAbsoluteAssetPath(levelId);
            if (string.IsNullOrWhiteSpace(absolutePath) || !File.Exists(absolutePath))
            {
                return true;
            }

            string assetPath = LevelPathUtility.GetAssetPath(levelId);
            bool isDirty = _service?.Session?.isDirty ?? false;
            string dirtyNote = isDirty
                ? "The current board has unsaved editor changes and this save will write them into the level file."
                : "The current board is not marked dirty, but this save will still overwrite the level file.";

            return UnityEditor.EditorUtility.DisplayDialog(
                "Overwrite Level File",
                $"{dirtyNote}\n\nTarget file:\n{assetPath}",
                "Save / Overwrite",
                "Cancel");
#else
            return true;
#endif
        }

        private bool ConfirmResizeBoard(int width, int height)
        {
#if UNITY_EDITOR
            if (_service?.Session?.board == null || !WouldLoseCells(width, height))
            {
                return true;
            }

            return UnityEditor.EditorUtility.DisplayDialog(
                "Resize Board",
                $"Resizing to {width}x{height} will remove cells outside the new board area.\n\nContinue?",
                "Resize",
                "Cancel");
#else
            return true;
#endif
        }

        private bool WouldLoseCells(int width, int height)
        {
            if (_service?.Session?.board == null)
            {
                return false;
            }

            for (int y = 0; y < _service.Session.board.height; y++)
            {
                for (int x = 0; x < _service.Session.board.width; x++)
                {
                    if (x < width && y < height)
                    {
                        continue;
                    }

                    var cell = _service.Session.board.GetCell(x, y);
                    if (cell.Playable || cell.TileId > 0 || cell.UnderlayId > 0 || cell.OverlayId > 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static int ReadInt(TMP_InputField inputField, int fallback)
        {
            return inputField != null && int.TryParse(inputField.text, out int value) ? Mathf.Max(1, value) : fallback;
        }

        private static void BindButton(Button button, Action callback)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => callback?.Invoke());
        }

        private void BindBoardSizeInput(TMP_InputField inputField)
        {
            if (inputField == null)
            {
                return;
            }

            inputField.onEndEdit.RemoveListener(HandleBoardSizeInputEndEdit);
            inputField.onEndEdit.AddListener(HandleBoardSizeInputEndEdit);
        }

        private void HandleBoardSizeInputEndEdit(string _)
        {
            ApplyBoardSizeFromInputs();
        }
    }
}
