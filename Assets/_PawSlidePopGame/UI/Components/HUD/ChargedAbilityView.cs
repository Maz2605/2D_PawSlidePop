using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static _PawSlidePopGame._Scripts.Data.Events.Payloads.GameplayHudSnapshot;

namespace _PawSlidePopGame._Scripts.UI.Components.HUD
{
    [DisallowMultipleComponent]
    public class ChargedAbilityView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Image fillImage;
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private TMP_Text stateText;
        [SerializeField] private Button useButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private CanvasGroup canvasGroup;

        public event Action OnUseRequested;
        public event Action OnCancelRequested;

        private ChargedAbilityHudData _currentData;
        private bool _isInputEnabled = true;

        private void Awake()
        {
            EnsureRuntimeView();
        }

        private void OnEnable()
        {
            if (useButton != null)
            {
                useButton.onClick.AddListener(HandleUseClicked);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(HandleCancelClicked);
            }
        }

        private void OnDisable()
        {
            if (useButton != null)
            {
                useButton.onClick.RemoveListener(HandleUseClicked);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveListener(HandleCancelClicked);
            }
        }

        public void SetData(ChargedAbilityHudData data)
        {
            _currentData = data?.Clone();
            EnsureRuntimeView();

            bool isVisible = _currentData != null && _currentData.isEnabled;
            gameObject.SetActive(isVisible);
            if (!isVisible)
            {
                return;
            }

            if (iconImage != null)
            {
                iconImage.sprite = _currentData.icon;
                iconImage.enabled = _currentData.icon != null;
            }

            if (fillImage != null)
            {
                float fillAmount = _currentData.requiredEnergy > 0
                    ? Mathf.Clamp01((float)_currentData.currentEnergy / _currentData.requiredEnergy)
                    : 0f;
                fillImage.fillAmount = fillAmount;
            }

            if (valueText != null)
            {
                valueText.SetText("{0}/{1}", _currentData.currentEnergy, Mathf.Max(1, _currentData.requiredEnergy));
            }

            if (stateText != null)
            {
                if (_currentData.isPlacementMode)
                {
                    stateText.text = "Pick a normal tile";
                }
                else if (_currentData.isComboSelectionMode)
                {
                    stateText.text = "Pick linked booster";
                }
                else if (_currentData.availableCharges > 0)
                {
                    stateText.text = "Charged";
                }
                else
                {
                    stateText.text = "Charging";
                }
            }

            if (useButton != null)
            {
                bool canUse = _currentData.availableCharges > 0 && !_currentData.isPlacementMode && !_currentData.isComboSelectionMode;
                useButton.gameObject.SetActive(!_currentData.isPlacementMode && !_currentData.isComboSelectionMode);
                useButton.interactable = _isInputEnabled && canUse;
            }

            if (cancelButton != null)
            {
                cancelButton.gameObject.SetActive(_currentData.isPlacementMode || _currentData.isComboSelectionMode);
                cancelButton.interactable = _isInputEnabled &&
                                            (_currentData.isPlacementMode || _currentData.isComboSelectionMode);
            }

            if (canvasGroup != null)
            {
                canvasGroup.interactable = _isInputEnabled;
                canvasGroup.blocksRaycasts = _isInputEnabled;
                canvasGroup.DOKill();
                canvasGroup.alpha = 0.85f;
                canvasGroup.DOFade(1f, 0.15f).SetLink(gameObject, LinkBehaviour.KillOnDisable);
            }
        }

        public void SetInputEnabled(bool enabled)
        {
            _isInputEnabled = enabled;
            RefreshInteractableState();
        }

        public void ResetView()
        {
            _currentData = null;
            _isInputEnabled = true;

            if (canvasGroup != null)
            {
                canvasGroup.DOKill();
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }

            if (fillImage != null)
            {
                fillImage.fillAmount = 0f;
            }

            if (valueText != null)
            {
                valueText.text = string.Empty;
            }

            if (stateText != null)
            {
                stateText.text = string.Empty;
            }

            if (useButton != null)
            {
                useButton.gameObject.SetActive(true);
                useButton.interactable = false;
            }

            if (cancelButton != null)
            {
                cancelButton.gameObject.SetActive(false);
                cancelButton.interactable = false;
            }

            gameObject.SetActive(false);
        }

        private void HandleUseClicked()
        {
            OnUseRequested?.Invoke();
        }

        private void HandleCancelClicked()
        {
            OnCancelRequested?.Invoke();
        }

        private void RefreshInteractableState()
        {
            if (_currentData == null)
            {
                if (canvasGroup != null)
                {
                    canvasGroup.interactable = _isInputEnabled;
                    canvasGroup.blocksRaycasts = _isInputEnabled;
                }

                return;
            }

            if (useButton != null)
            {
                bool canUse = _currentData.availableCharges > 0 && !_currentData.isPlacementMode && !_currentData.isComboSelectionMode;
                useButton.interactable = _isInputEnabled && canUse;
            }

            if (cancelButton != null)
            {
                bool canCancel = _currentData.isPlacementMode || _currentData.isComboSelectionMode;
                cancelButton.interactable = _isInputEnabled && canCancel;
            }

            if (canvasGroup != null)
            {
                canvasGroup.interactable = _isInputEnabled;
                canvasGroup.blocksRaycasts = _isInputEnabled;
            }
        }

        private void EnsureRuntimeView()
        {
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            RectTransform rootRect = transform as RectTransform;
            if (rootRect != null && rootRect.sizeDelta == Vector2.zero)
            {
                rootRect.anchorMin = new Vector2(0.5f, 0f);
                rootRect.anchorMax = new Vector2(0.5f, 0f);
                rootRect.pivot = new Vector2(0.5f, 0f);
                rootRect.anchoredPosition = new Vector2(0f, 18f);
                rootRect.sizeDelta = new Vector2(360f, 104f);
            }

            Image background = GetComponent<Image>();
            if (background == null)
            {
                background = gameObject.AddComponent<Image>();
            }

            background.color = new Color(0.11f, 0.14f, 0.18f, 0.92f);

            if (iconImage == null)
            {
                iconImage = CreateImage("Icon", new Vector2(48f, 48f), new Vector2(16f, 24f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
            }

            if (fillImage == null)
            {
                Image fillBackground = CreateImage("FillBackground", new Vector2(140f, 16f), new Vector2(84f, 26f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
                fillBackground.color = new Color(0.23f, 0.28f, 0.34f, 1f);

                fillImage = CreateImage("Fill", new Vector2(140f, 16f), new Vector2(84f, 26f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
                fillImage.type = Image.Type.Filled;
                fillImage.fillMethod = Image.FillMethod.Horizontal;
                fillImage.color = new Color(1f, 0.76f, 0.18f, 1f);
            }

            if (valueText == null)
            {
                valueText = CreateText("ValueText", new Vector2(140f, 24f), new Vector2(84f, 8f), 22, TextAlignmentOptions.Center);
            }

            if (stateText == null)
            {
                stateText = CreateText("StateText", new Vector2(140f, 22f), new Vector2(84f, -18f), 18, TextAlignmentOptions.Center);
            }

            if (useButton == null)
            {
                useButton = CreateButton("UseButton", "Use", new Vector2(92f, 36f), new Vector2(-16f, 18f));
            }

            if (cancelButton == null)
            {
                cancelButton = CreateButton("CancelButton", "Cancel", new Vector2(92f, 36f), new Vector2(-16f, -18f));
            }
        }

        private Image CreateImage(string name, Vector2 size, Vector2 anchoredPosition, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject child = FindOrCreateChild(name);
            RectTransform rectTransform = child.GetComponent<RectTransform>();
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = anchorMin;
            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = anchoredPosition;

            Image image = child.GetComponent<Image>();
            if (image == null)
            {
                image = child.AddComponent<Image>();
            }

            return image;
        }

        private TMP_Text CreateText(string name, Vector2 size, Vector2 anchoredPosition, float fontSize, TextAlignmentOptions alignment)
        {
            GameObject child = FindOrCreateChild(name);
            RectTransform rectTransform = child.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 0.5f);
            rectTransform.anchorMax = new Vector2(0f, 0.5f);
            rectTransform.pivot = new Vector2(0f, 0.5f);
            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = anchoredPosition;

            TMP_Text text = child.GetComponent<TMP_Text>();
            if (text == null)
            {
                text = child.AddComponent<TextMeshProUGUI>();
            }

            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            return text;
        }

        private Button CreateButton(string name, string label, Vector2 size, Vector2 anchoredPosition)
        {
            GameObject child = FindOrCreateChild(name);
            RectTransform rectTransform = child.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(1f, 0.5f);
            rectTransform.anchorMax = new Vector2(1f, 0.5f);
            rectTransform.pivot = new Vector2(1f, 0.5f);
            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = anchoredPosition;

            Image image = child.GetComponent<Image>();
            if (image == null)
            {
                image = child.AddComponent<Image>();
            }

            image.color = new Color(0.22f, 0.47f, 0.94f, 1f);

            Button button = child.GetComponent<Button>();
            if (button == null)
            {
                button = child.AddComponent<Button>();
            }

            TMP_Text buttonText = child.GetComponentInChildren<TMP_Text>();
            if (buttonText == null)
            {
                buttonText = CreateText(name + "Label", size, Vector2.zero, 20, TextAlignmentOptions.Center);
                buttonText.transform.SetParent(child.transform, false);
                RectTransform textRect = buttonText.transform as RectTransform;
                if (textRect != null)
                {
                    textRect.anchorMin = Vector2.zero;
                    textRect.anchorMax = Vector2.one;
                    textRect.offsetMin = Vector2.zero;
                    textRect.offsetMax = Vector2.zero;
                    textRect.pivot = new Vector2(0.5f, 0.5f);
                    textRect.anchoredPosition = Vector2.zero;
                }
            }

            buttonText.text = label;
            return button;
        }

        private GameObject FindOrCreateChild(string childName)
        {
            Transform child = transform.Find(childName);
            if (child != null)
            {
                return child.gameObject;
            }

            GameObject gameObjectChild = new GameObject(childName, typeof(RectTransform));
            gameObjectChild.transform.SetParent(transform, false);
            return gameObjectChild;
        }
    }
}
