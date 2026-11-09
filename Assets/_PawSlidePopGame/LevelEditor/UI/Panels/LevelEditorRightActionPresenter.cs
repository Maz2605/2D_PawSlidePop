using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.UI
{
    public sealed class LevelEditorRightActionPresenter : MonoBehaviour
    {
        [SerializeField] private Button clearCellButton;
        [SerializeField] private Button clearSelectionButton;
        [SerializeField] private Button clearItemButton;
        [SerializeField] private Button clearUnderButton;
        [SerializeField] private Button clearOverButton;

        [SerializeField] private TMP_Text clearCellHotkeyText;
        [SerializeField] private TMP_Text clearSelectionHotkeyText;
        [SerializeField] private TMP_Text clearItemHotkeyText;
        [SerializeField] private TMP_Text clearUnderHotkeyText;
        [SerializeField] private TMP_Text clearOverHotkeyText;

        private LevelEditorUIController _service;

        public void Bind(LevelEditorUIController service)
        {
            _service = service;
            BindButton(clearCellButton, HandleClearCell);
            BindButton(clearSelectionButton, HandleClearSelection);
            BindButton(clearItemButton, HandleClearItem);
            BindButton(clearUnderButton, HandleClearUnder);
            BindButton(clearOverButton, HandleClearOver);
            ApplyHotkeyLabels();
        }

        public void Refresh()
        {
            ApplyHotkeyLabels();

            bool hasSelectedCell = _service?.Selection != null && _service.Selection.HasSelectedCell;
            bool hasAnyPayload = _service?.Selection != null && _service.Selection.HasAnySelectedLayer();

            SetInteractable(clearCellButton, hasSelectedCell);
            SetInteractable(clearItemButton, hasSelectedCell);
            SetInteractable(clearUnderButton, hasSelectedCell);
            SetInteractable(clearOverButton, hasSelectedCell);
            SetInteractable(clearSelectionButton, hasAnyPayload);
        }

        private void Update()
        {
            if (_service == null || IsTypingIntoInputField())
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.qKey.wasPressedThisFrame)
            {
                InvokeIfInteractable(clearCellButton, HandleClearCell);
            }
            else if (keyboard.wKey.wasPressedThisFrame)
            {
                InvokeIfInteractable(clearItemButton, HandleClearItem);
            }
            else if (keyboard.eKey.wasPressedThisFrame)
            {
                InvokeIfInteractable(clearUnderButton, HandleClearUnder);
            }
            else if (keyboard.rKey.wasPressedThisFrame)
            {
                InvokeIfInteractable(clearOverButton, HandleClearOver);
            }
            else if (keyboard.tKey.wasPressedThisFrame)
            {
                InvokeIfInteractable(clearSelectionButton, HandleClearSelection);
            }
        }

        private void HandleClearCell()
        {
            _service?.ClearSelectedCell();
        }

        private void HandleClearSelection()
        {
            _service?.ClearSelectionPayload();
        }

        private void HandleClearItem()
        {
            _service?.ClearSelectedCellTile();
        }

        private void HandleClearUnder()
        {
            _service?.ClearSelectedCellUnderlay();
        }

        private void HandleClearOver()
        {
            _service?.ClearSelectedCellOverlay();
        }

        private void ApplyHotkeyLabels()
        {
            SetHotkeyText(clearCellHotkeyText, "Q");
            SetHotkeyText(clearItemHotkeyText, "W");
            SetHotkeyText(clearUnderHotkeyText, "E");
            SetHotkeyText(clearOverHotkeyText, "R");
            SetHotkeyText(clearSelectionHotkeyText, "T");
        }

        private static void BindButton(Button button, UnityEngine.Events.UnityAction callback)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(callback);
        }

        private static void SetInteractable(Selectable selectable, bool interactable)
        {
            if (selectable != null)
            {
                selectable.interactable = interactable;
            }
        }

        private static void SetHotkeyText(TMP_Text label, string keyText)
        {
            if (label != null)
            {
                label.text = keyText;
            }
        }

        private static void InvokeIfInteractable(Button button, System.Action callback)
        {
            if (button == null || !button.IsActive() || !button.interactable)
            {
                return;
            }

            callback?.Invoke();
        }

        private static bool IsTypingIntoInputField()
        {
            GameObject current = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (current == null)
            {
                return false;
            }

            return current.GetComponent<TMP_InputField>() != null || current.GetComponent<InputField>() != null;
        }
    }
}
