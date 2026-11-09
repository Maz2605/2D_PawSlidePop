using System;
using _PawSlidePopGame._Scripts.Core.Audio;
using _PawSlidePopGame._Scripts.Core.System;
using _PawSlidePopGame._Scripts.UI.Base;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace _PawSlidePopGame._Scripts.UI.Popups
{
    public sealed class SettingsPopup : BasePopup
    {
        [Header("--- Elements ---")]
        [SerializeField] private RectTransform animatedContent;

        // ── Music ────────────────────────────────────────────────
        [Header("--- Music Settings ---")]
        [SerializeField] private Slider musicSlider;
        /// <summary>GameObject Icon hien thi khi Music > 0 (chua tat)</summary>
        [SerializeField] private GameObject musicIconOn;
        /// <summary>GameObject Icon hien thi khi Music = 0 (da tat)</summary>
        [SerializeField] private GameObject musicIconOff;

        // ── SFX ──────────────────────────────────────────────────
        [Header("--- SFX Settings ---")]
        [SerializeField] private Slider sfxSlider;
        /// <summary>GameObject Icon hien thi khi SFX > 0 (chua tat)</summary>
        [SerializeField] private GameObject sfxIconOn;
        /// <summary>GameObject Icon hien thi khi SFX = 0 (da tat)</summary>
        [SerializeField] private GameObject sfxIconOff;

        // ── Vibration ────────────────────────────────────────────
        [Header("--- Vibration (Slider as Toggle) ---")]
        [SerializeField] private Slider vibrationSlider;
        [SerializeField] private GameObject vibrationIconOn;
        [SerializeField] private GameObject vibrationIconOff;

        [Header("--- Buttons ---")]
        [SerializeField] private Button btnClose;
        [SerializeField] private Button btnCancel;
        [SerializeField] private Button btnSave;

        [Header("--- Sequence Settings ---")]
        [SerializeField] private float moveOffsetY = 200f;
        [SerializeField] private float moveDuration = 0.3f;
        [SerializeField] private Ease showEase = Ease.OutBack;
        [SerializeField] private Ease hideEase = Ease.InCubic;

        private Sequence _showSequence;
        private Vector2 _contentOriginalPos;
        private float _initialMusicVolume;
        private float _initialSfxVolume;
        private bool _initialVibrationEnabled;
        private float _currentMusicVolume;
        private float _currentSfxVolume;
        private bool _currentVibrationEnabled;
        private bool _lastPreviewedVibrationState;
        private bool _shouldRestoreSettingsOnHide;
        private bool _hasActiveSession;

        // ──────────────────────────────────────────────────────────
        // Lifecycle
        // ──────────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            if (animatedContent != null)
                _contentOriginalPos = animatedContent.anchoredPosition;

            // Vibration hoat dong nhu Toggle (chi 0 hoac 1)
            if (vibrationSlider != null)
            {
                vibrationSlider.minValue = 0f;
                vibrationSlider.maxValue = 1f;
                vibrationSlider.wholeNumbers = true;
            }

            SetupSliderDragEffects();
        }

        // ──────────────────────────────────────────────────────────
        // BasePopup Overrides
        // ──────────────────────────────────────────────────────────

        protected override void OnBeforeShow()
        {
            KillActiveTweens();
            ResetLayoutState();
            _shouldRestoreSettingsOnHide = true;
            _hasActiveSession = true;
            LoadRuntimeValues();
            BindButtons();
            BindSliderIcons();
            RefreshMusicIcon(animate: false);
            RefreshSfxIcon(animate: false);
            RefreshVibrationIcon(animate: false);
        }

        protected override void PlayShowAnimation()
        {
            _showSequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);

            if (animatedContent != null)
            {
                _showSequence.Append(
                    animatedContent
                        .DOAnchorPos(_contentOriginalPos, moveDuration)
                        .SetEase(showEase));
            }

            _showSequence.OnComplete(() => SetButtonsInteractable(true));
        }

        protected override void PlayHideAnimation(Action onComplete)
        {
            KillActiveTweens();

            Sequence hideSeq = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);

            if (animatedContent != null)
            {
                hideSeq.Join(
                    animatedContent
                        .DOAnchorPos(_contentOriginalPos + new Vector2(0f, -moveOffsetY), moveDuration)
                        .SetEase(hideEase));
            }

            hideSeq.Join(canvasGroup.DOFade(0f, moveDuration * 0.8f).SetEase(Ease.InQuad));
            hideSeq.OnComplete(() => onComplete?.Invoke());
        }

        // ──────────────────────────────────────────────────────────
        // Binding
        // ──────────────────────────────────────────────────────────

        private void BindButtons()
        {
            SetButtonsInteractable(false);
            BindButton(btnClose,  HandleClosePressed);
            BindButton(btnCancel, HandleCancelPressed);
            BindButton(btnSave,   HandleSavePressed);
        }

        private void BindSliderIcons()
        {
            if (musicSlider != null)
            {
                musicSlider.onValueChanged.RemoveAllListeners();
                musicSlider.onValueChanged.AddListener(HandleMusicSliderChanged);
            }

            if (sfxSlider != null)
            {
                sfxSlider.onValueChanged.RemoveAllListeners();
                sfxSlider.onValueChanged.AddListener(HandleSfxSliderChanged);
            }

            if (vibrationSlider != null)
            {
                vibrationSlider.onValueChanged.RemoveAllListeners();
                vibrationSlider.onValueChanged.AddListener(HandleVibrationSliderChanged);
            }
        }

        // ──────────────────────────────────────────────────────────
        // Icon Refresh - dung GameObject On/Off cho ca 3
        // ──────────────────────────────────────────────────────────

        private void RefreshMusicIcon(bool animate = true)
        {
            bool isMuted = musicSlider == null || musicSlider.value <= 0.001f;
            TransitionIcon(musicSlider, musicIconOn, musicIconOff, !isMuted, animate);
        }

        private void RefreshSfxIcon(bool animate = true)
        {
            bool isMuted = sfxSlider == null || sfxSlider.value <= 0.001f;
            TransitionIcon(sfxSlider, sfxIconOn, sfxIconOff, !isMuted, animate);
        }

        private void RefreshVibrationIcon(bool animate = true)
        {
            bool isOn = vibrationSlider != null && vibrationSlider.value >= 0.5f;
            TransitionIcon(vibrationSlider, vibrationIconOn, vibrationIconOff, isOn, animate);
        }

        private void TransitionIcon(Slider slider, GameObject iconOn, GameObject iconOff, bool showOn, bool animate)
        {
            if (iconOn == null || iconOff == null) return;

            GameObject targetActive = showOn ? iconOn : iconOff;
            GameObject targetInactive = showOn ? iconOff : iconOn;

            if (!animate)
            {
                targetActive.SetActive(true);
                targetActive.transform.localScale = Vector3.one;
                targetInactive.SetActive(false);
                targetInactive.transform.localScale = Vector3.zero;
                return;
            }

            if (!targetActive.activeSelf)
            {
                targetInactive.SetActive(false);
                targetInactive.transform.localScale = Vector3.zero;

                targetActive.SetActive(true);
                targetActive.transform.localScale = Vector3.one;

                // Tac dong hieu ung nay (Punch Scale) len Knob (Handle cua Slider)
                if (slider != null && slider.handleRect != null)
                {
                    slider.handleRect.DOKill();
                    slider.handleRect.DOPunchScale(new Vector3(0.12f, 0.12f, 0f), 0.25f, 4, 0.5f).SetUpdate(true);
                }
            }
            else
            {
                targetActive.SetActive(true);
                targetActive.transform.localScale = Vector3.one;
                targetInactive.SetActive(false);
                targetInactive.transform.localScale = Vector3.zero;
            }
        }

        // ──────────────────────────────────────────────────────────
        // Handlers
        // ──────────────────────────────────────────────────────────

        private void HandleClosePressed()
        {
            Hide();
        }

        private void HandleCancelPressed()
        {
            Hide();
        }

        private void HandleSavePressed()
        {
            CommitSettingsState();
            _shouldRestoreSettingsOnHide = false;
            Hide();
        }

        // ──────────────────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────────────────

        private void SetButtonsInteractable(bool enable)
        {
            if (btnClose)  btnClose.interactable  = enable;
            if (btnCancel) btnCancel.interactable  = enable;
            if (btnSave)   btnSave.interactable    = enable;
        }

        private void PrepareForShow()
        {
            if (animatedContent == null) return;
            animatedContent.anchoredPosition = _contentOriginalPos + new Vector2(0f, -moveOffsetY);
        }

        private void ResetLayoutState()
        {
            if (animatedContent == null) return;
            PrepareForShow();
            animatedContent.localScale = Vector3.one;
            animatedContent.localRotation = Quaternion.identity;
        }

        private void LoadRuntimeValues()
        {
            SettingsManager settingsManager = SettingsManager.Instance;
            if (settingsManager != null)
            {
                settingsManager.RestoreSavedSettings();
                _initialMusicVolume = settingsManager.MusicVolume;
                _initialSfxVolume = settingsManager.SfxVolume;
                _initialVibrationEnabled = settingsManager.IsVibrationEnabled;
            }
            else
            {
                _initialMusicVolume = 1f;
                _initialSfxVolume = 1f;
                _initialVibrationEnabled = true;
            }

            _currentMusicVolume = _initialMusicVolume;
            _currentSfxVolume = _initialSfxVolume;
            _currentVibrationEnabled = _initialVibrationEnabled;

            ApplyAudioState(_currentMusicVolume, _currentSfxVolume, animate: false);
            ApplyVibrationState(_currentVibrationEnabled, animate: false);
        }

        private void ApplyVibrationState(bool enabled, bool animate)
        {
            SettingsManager.Instance?.PreviewVibration(enabled);

            if (vibrationSlider == null)
            {
                _lastPreviewedVibrationState = enabled;
                return;
            }

            vibrationSlider.SetValueWithoutNotify(enabled ? 1f : 0f);
            _lastPreviewedVibrationState = enabled;
            RefreshVibrationIcon(animate);
        }

        private void HandleVibrationSliderChanged(float _)
        {
            bool isOn = vibrationSlider != null && vibrationSlider.value >= 0.5f;
            _currentVibrationEnabled = isOn;
            RefreshVibrationIcon(animate: true);
            SettingsManager.Instance?.PreviewVibration(_currentVibrationEnabled, playFeedback: _currentVibrationEnabled && !_lastPreviewedVibrationState);

            _lastPreviewedVibrationState = _currentVibrationEnabled;
        }

        private void ApplyAudioState(float musicValue, float sfxValue, bool animate)
        {
            if (musicSlider != null)
            {
                musicSlider.SetValueWithoutNotify(Mathf.Clamp01(musicValue));
                RefreshMusicIcon(animate);
            }

            if (sfxSlider != null)
            {
                sfxSlider.SetValueWithoutNotify(Mathf.Clamp01(sfxValue));
                RefreshSfxIcon(animate);
            }

            ApplyAudioRuntime(musicValue, sfxValue);
        }

        private void RestoreInitialSettingsState()
        {
            SettingsManager settingsManager = SettingsManager.Instance;
            settingsManager?.RestoreSavedSettings();
            _currentMusicVolume = _initialMusicVolume;
            _currentSfxVolume = _initialSfxVolume;
            _currentVibrationEnabled = _initialVibrationEnabled;
            ApplyAudioState(_currentMusicVolume, _currentSfxVolume, animate: false);
            ApplyVibrationState(_currentVibrationEnabled, animate: false);
        }

        private void CommitSettingsState()
        {
            SettingsManager settingsManager = SettingsManager.Instance;
            if (settingsManager == null)
            {
                return;
            }

            settingsManager.Save(_currentMusicVolume, _currentSfxVolume, _currentVibrationEnabled);
            _initialMusicVolume = settingsManager.MusicVolume;
            _initialSfxVolume = settingsManager.SfxVolume;
            _initialVibrationEnabled = settingsManager.IsVibrationEnabled;
            _currentMusicVolume = _initialMusicVolume;
            _currentSfxVolume = _initialSfxVolume;
            _currentVibrationEnabled = _initialVibrationEnabled;
        }

        private void ApplyAudioRuntime(float musicValue, float sfxValue)
        {
            SettingsManager settingsManager = SettingsManager.Instance;
            if (settingsManager == null)
            {
                return;
            }

            float clampedMusic = Mathf.Clamp01(musicValue);
            float clampedSfx = Mathf.Clamp01(sfxValue);
            settingsManager.PreviewAudio(clampedMusic, clampedSfx);
        }

        private void HandleMusicSliderChanged(float value)
        {
            _currentMusicVolume = Mathf.Clamp01(value);
            RefreshMusicIcon(animate: true);
            SettingsManager settingsManager = SettingsManager.Instance;
            if (settingsManager == null)
            {
                return;
            }

            settingsManager.PreviewAudio(_currentMusicVolume, _currentSfxVolume);
        }

        private void HandleSfxSliderChanged(float value)
        {
            _currentSfxVolume = Mathf.Clamp01(value);
            RefreshSfxIcon(animate: true);
            SettingsManager settingsManager = SettingsManager.Instance;
            if (settingsManager == null)
            {
                return;
            }

            settingsManager.PreviewAudio(_currentMusicVolume, _currentSfxVolume);
        }

        private void KillActiveTweens()
        {
            _showSequence?.Kill();
            animatedContent?.DOKill();
            transform.DOKill();

            if (musicIconOn != null) musicIconOn.transform.DOKill();
            if (musicIconOff != null) musicIconOff.transform.DOKill();
            if (sfxIconOn != null) sfxIconOn.transform.DOKill();
            if (sfxIconOff != null) sfxIconOff.transform.DOKill();
            if (vibrationIconOn != null) vibrationIconOn.transform.DOKill();
            if (vibrationIconOff != null) vibrationIconOff.transform.DOKill();

            if (musicSlider != null && musicSlider.handleRect != null) musicSlider.handleRect.DOKill();
            if (sfxSlider != null && sfxSlider.handleRect != null) sfxSlider.handleRect.DOKill();
            if (vibrationSlider != null && vibrationSlider.handleRect != null) vibrationSlider.handleRect.DOKill();
        }

        private void OnDisable()
        {
            if (!_hasActiveSession)
            {
                return;
            }

            if (_shouldRestoreSettingsOnHide)
            {
                RestoreInitialSettingsState();
            }

            _hasActiveSession = false;
            _shouldRestoreSettingsOnHide = false;
        }

        // ──────────────────────────────────────────────────────────
        // Private Interaction Handlers (Tu dong dang ky EventTrigger qua code)
        // ──────────────────────────────────────────────────────────

        private readonly System.Collections.Generic.Dictionary<Slider, bool> _isSliderPressed = new System.Collections.Generic.Dictionary<Slider, bool>();

        private void SetupSliderDragEffects()
        {
            SetupDragEffectForSlider(musicSlider);
            SetupDragEffectForSlider(sfxSlider);
            SetupDragEffectForSlider(vibrationSlider);
        }

        private void SetupDragEffectForSlider(Slider slider)
        {
            if (slider == null) return;

            RectTransform handle = slider.handleRect;
            if (handle == null) return;

            EventTrigger trigger = slider.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = slider.gameObject.AddComponent<EventTrigger>();
            }

            // Xoa cac event cu de tranh trung lap
            trigger.triggers.Clear();

            // Pointer Enter (Hover vao)
            EventTrigger.Entry pointerEnter = new EventTrigger.Entry();
            pointerEnter.eventID = EventTriggerType.PointerEnter;
            pointerEnter.callback.AddListener((data) =>
            {
                if (_isSliderPressed.TryGetValue(slider, out bool pressed) && pressed) return;
                handle.DOKill();
                handle.DOScale(1.08f, 0.15f).SetEase(Ease.OutCubic).SetUpdate(true);
            });
            trigger.triggers.Add(pointerEnter);

            // Pointer Exit (Hover ra)
            EventTrigger.Entry pointerExit = new EventTrigger.Entry();
            pointerExit.eventID = EventTriggerType.PointerExit;
            pointerExit.callback.AddListener((data) =>
            {
                if (_isSliderPressed.TryGetValue(slider, out bool pressed) && pressed) return;
                handle.DOKill();
                handle.DOScale(1.0f, 0.15f).SetEase(Ease.OutCubic).SetUpdate(true);
            });
            trigger.triggers.Add(pointerExit);

            // Pointer Down (Nhan giu/Keo)
            EventTrigger.Entry pointerDown = new EventTrigger.Entry();
            pointerDown.eventID = EventTriggerType.PointerDown;
            pointerDown.callback.AddListener((data) =>
            {
                _isSliderPressed[slider] = true;
                handle.DOKill();
                handle.DOScale(1.16f, 0.15f).SetEase(Ease.OutBack).SetUpdate(true);
            });
            trigger.triggers.Add(pointerDown);

            // Pointer Up (Tha ra)
            EventTrigger.Entry pointerUp = new EventTrigger.Entry();
            pointerUp.eventID = EventTriggerType.PointerUp;
            pointerUp.callback.AddListener((data) =>
            {
                _isSliderPressed[slider] = false;
                handle.DOKill();
                handle.DOScale(1.0f, 0.25f).SetEase(Ease.OutBack).SetUpdate(true);

                if (slider == sfxSlider && AudioController.Instance != null)
                {
                    AudioController.Instance.PlayUISound(UISoundType.ClickNormal);
                }
            });
            trigger.triggers.Add(pointerUp);
        }
    }
}
