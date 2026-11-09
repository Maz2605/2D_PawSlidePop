using System;
using _PawSlidePopGame._Scripts.Services.Auth;
using _PawSlidePopGame._Scripts.UI.Base;
using _PawSlidePopGame._Scripts.UI.Manager;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.UI.Popups
{
    public sealed class EmailAuthPopup : BasePopup
    {
        public enum AuthPopupMode
        {
            Login,
            Register
        }

        [Header("--- UI Content Area ---")]
        [SerializeField] private RectTransform animatedContent;
        [SerializeField] private RectTransform inputsLayoutGroup;

        [Header("--- Input Fields ---")]
        [SerializeField] private TMP_InputField inputEmail;
        [SerializeField] private TMP_InputField inputPassword;
        [SerializeField] private TMP_InputField inputConfirmPassword;

        [Header("--- Action Buttons ---")]
        [SerializeField] private Button btnSubmitLogin;
        [SerializeField] private Button btnSubmitRegister;
        [SerializeField] private Button btnClose;

        [Header("--- Mode Toggle Elements ---")]
        [SerializeField] private TextMeshProUGUI txtTitle;
        [SerializeField] private Button btnToggleMode;
        [SerializeField] private TextMeshProUGUI txtToggleModeLabel;
        [SerializeField] private AuthPopupMode defaultMode = AuthPopupMode.Login;

        [Header("--- Password Visibility ---")]
        [SerializeField] private Button btnTogglePasswordVisibility;
        [SerializeField] private Image imgPasswordVisibilityEye;
        [SerializeField] private Sprite spriteEyeOpen;
        [SerializeField] private Sprite spriteEyeClosed;

        [Header("--- Log Display ---")]
        [SerializeField] private TextMeshProUGUI txtStatusLog;

        [Header("--- Animation Settings ---")]
        [SerializeField] private float moveOffsetY = 200f;
        [SerializeField] private float moveDuration = 0.3f;
        [SerializeField] private Ease showEase = Ease.OutBack;
        [SerializeField] private Ease hideEase = Ease.InCubic;

        private Sequence _showSequence;
        private Tween _idleTween;
        private Vector2 _contentOriginalPos;
        private AuthPopupMode _currentMode;
        private bool _isPasswordVisible;
        private Tween _confirmFieldTween;
        private CanvasGroup _confirmFieldCanvasGroup;

        private Vector3 _statusLogOriginalLocalPos;

        protected override void Awake()
        {
            base.Awake();
            if (animatedContent != null)
                _contentOriginalPos = animatedContent.anchoredPosition;
            if (txtStatusLog != null)
                _statusLogOriginalLocalPos = txtStatusLog.transform.localPosition;
        }

        protected override void OnBeforeShow()
        {
            KillActiveTweens();
            ResetLayoutState();

            // Clear inputs and logs on open

            if (inputEmail != null) inputEmail.text = string.Empty;
            if (inputPassword != null)
            {
                inputPassword.text = string.Empty;
                inputPassword.contentType = TMP_InputField.ContentType.Password;
                inputPassword.ForceLabelUpdate();
            }
            if (inputConfirmPassword != null)
            {
                inputConfirmPassword.text = string.Empty;
                inputConfirmPassword.contentType = TMP_InputField.ContentType.Password;
                inputConfirmPassword.ForceLabelUpdate();
            }
            if (txtStatusLog != null)
            {
                txtStatusLog.text = string.Empty;
            }

            _isPasswordVisible = false;
            UpdatePasswordEyeIcon();

            SetMode(defaultMode, false);
            BindButtons();
        }

        private void SetMode(AuthPopupMode mode, bool animate = false)
        {
            _currentMode = mode;
            if (txtStatusLog != null) txtStatusLog.text = string.Empty;

            // Kill any active confirm field transition
            if (_confirmFieldTween != null && _confirmFieldTween.IsActive())
            {
                _confirmFieldTween.Kill();
            }

            var confirmParent = inputConfirmPassword != null ? (RectTransform)inputConfirmPassword.transform.parent : null;
            if (confirmParent != null)
            {
                if (_confirmFieldCanvasGroup == null)
                {
                    _confirmFieldCanvasGroup = confirmParent.GetComponent<CanvasGroup>();
                    if (_confirmFieldCanvasGroup == null)
                    {
                        _confirmFieldCanvasGroup = confirmParent.gameObject.AddComponent<CanvasGroup>();
                    }
                }
            }

            string targetTitle = _currentMode == AuthPopupMode.Login ? "Login" : "Register";
            string targetToggleLabel = _currentMode == AuthPopupMode.Login ? "Don't have an account? Register" : "Already have an account? Login";

            if (txtTitle != null) txtTitle.text = targetTitle;
            if (btnSubmitLogin != null) btnSubmitLogin.gameObject.SetActive(_currentMode == AuthPopupMode.Login);
            if (btnSubmitRegister != null) btnSubmitRegister.gameObject.SetActive(_currentMode == AuthPopupMode.Register);
            if (txtToggleModeLabel != null) txtToggleModeLabel.text = targetToggleLabel;

            if (confirmParent != null)
            {
                float targetHeight = _currentMode == AuthPopupMode.Register ? 80f : 0f;
                float targetAlpha = _currentMode == AuthPopupMode.Register ? 1f : 0f;

                if (animate)
                {
                    if (_currentMode == AuthPopupMode.Register)
                    {
                        confirmParent.gameObject.SetActive(true);
                    }

                    _confirmFieldCanvasGroup.alpha = _currentMode == AuthPopupMode.Register ? 0f : 1f;

                    var seq = DOTween.Sequence()
                        .SetUpdate(true)
                        .SetLink(gameObject, LinkBehaviour.KillOnDisable);

                    seq.Append(DOTween.To(() => confirmParent.sizeDelta.y, y =>
                    {
                        confirmParent.sizeDelta = new Vector2(confirmParent.sizeDelta.x, y);
                        if (inputsLayoutGroup != null)
                        {
                            LayoutRebuilder.ForceRebuildLayoutImmediate(inputsLayoutGroup);
                        }
                    }, targetHeight, 0.25f).SetEase(Ease.OutQuad));

                    seq.Join(_confirmFieldCanvasGroup.DOFade(targetAlpha, 0.20f));

                    seq.OnComplete(() =>
                    {
                        if (_currentMode == AuthPopupMode.Login)
                        {
                            confirmParent.gameObject.SetActive(false);
                        }
                        if (inputsLayoutGroup != null)
                        {
                            LayoutRebuilder.ForceRebuildLayoutImmediate(inputsLayoutGroup);
                        }
                    });

                    _confirmFieldTween = seq;
                }
                else
                {
                    // Instant setup
                    confirmParent.sizeDelta = new Vector2(confirmParent.sizeDelta.x, targetHeight);
                    _confirmFieldCanvasGroup.alpha = targetAlpha;
                    confirmParent.gameObject.SetActive(_currentMode == AuthPopupMode.Register);
                    if (inputsLayoutGroup != null)
                    {
                        LayoutRebuilder.ForceRebuildLayoutImmediate(inputsLayoutGroup);
                    }
                }
            }
        }

        private void ToggleMode()
        {
            AuthPopupMode nextMode = _currentMode == AuthPopupMode.Login

                ? AuthPopupMode.Register

                : AuthPopupMode.Login;
            SetMode(nextMode, true);
        }

        private void TogglePasswordVisibility()
        {
            if (inputPassword == null) return;

            _isPasswordVisible = !_isPasswordVisible;


            inputPassword.contentType = _isPasswordVisible

                ? TMP_InputField.ContentType.Standard

                : TMP_InputField.ContentType.Password;
            inputPassword.ForceLabelUpdate();

            if (inputConfirmPassword != null)
            {
                inputConfirmPassword.contentType = _isPasswordVisible

                    ? TMP_InputField.ContentType.Standard

                    : TMP_InputField.ContentType.Password;
                inputConfirmPassword.ForceLabelUpdate();
            }

            UpdatePasswordEyeIcon();
        }

        private void UpdatePasswordEyeIcon()
        {
            if (imgPasswordVisibilityEye != null)
            {
                if (_isPasswordVisible && spriteEyeOpen != null)
                {
                    imgPasswordVisibilityEye.sprite = spriteEyeOpen;
                }
                else if (!_isPasswordVisible && spriteEyeClosed != null)
                {
                    imgPasswordVisibilityEye.sprite = spriteEyeClosed;
                }
            }
        }

        protected override void PlayShowAnimation()
        {
            _showSequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);

            if (animatedContent != null)
            {
                animatedContent.localScale = Vector3.zero;
                animatedContent.anchoredPosition = _contentOriginalPos + new Vector2(0f, -moveOffsetY);

                _showSequence.Append(
                    animatedContent
                        .DOAnchorPos(_contentOriginalPos, moveDuration)
                        .SetEase(showEase));
                _showSequence.Join(
                    animatedContent
                        .DOScale(Vector3.one, moveDuration)
                        .SetEase(showEase));
                _showSequence.OnComplete(PlayIdleAnimation);
            }
        }

        private void PlayIdleAnimation()
        {
            if (animatedContent == null) return;
            _idleTween = animatedContent.DOAnchorPos(_contentOriginalPos + new Vector2(0f, 15f), 2f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
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
                hideSeq.Join(
                    animatedContent
                        .DOScale(Vector3.zero, moveDuration)
                        .SetEase(hideEase));
            }

            hideSeq.Join(canvasGroup.DOFade(0f, moveDuration * 0.8f).SetEase(Ease.InQuad));
            hideSeq.OnComplete(() => onComplete?.Invoke());
        }

        private void BindButtons()
        {
            BindButton(btnClose, Hide);
            BindButton(btnSubmitLogin, HandleSubmitLoginPressed);
            BindButton(btnSubmitRegister, HandleSubmitRegisterPressed);
            BindButton(btnToggleMode, ToggleMode);
            BindButton(btnTogglePasswordVisibility, TogglePasswordVisibility);
        }

        private void ShowStatusLog(string message, bool isError = true)
        {
            if (txtStatusLog == null) return;

            txtStatusLog.text = message;
            txtStatusLog.color = isError ? Color.red : new Color(0.12f, 0.73f, 0.22f); // Đỏ cho lỗi, Xanh lá cho thành công

            // Hiệu ứng rung chữ dựa trên vị trí ban đầu được cache
            txtStatusLog.transform.DOKill();
            txtStatusLog.transform.localPosition = _statusLogOriginalLocalPos;
            txtStatusLog.transform.DOShakePosition(0.4f, new Vector3(10f, 0f, 0f), 10, 90f, false, true)
                .SetUpdate(true)
                .SetLink(txtStatusLog.gameObject, LinkBehaviour.KillOnDisable);

            // Rung lắc cả khung nội dung của popup khi có lỗi
            if (isError && animatedContent != null)
            {
                _idleTween?.Kill();
                animatedContent.DOKill();
                animatedContent.localScale = Vector3.one;
                animatedContent.localRotation = Quaternion.identity;
                
                animatedContent.DOShakePosition(0.5f, new Vector3(25f, 0f, 0f), 12, 90f, false, true)
                    .SetUpdate(true)
                    .SetLink(animatedContent.gameObject, LinkBehaviour.KillOnDisable)
                    .OnComplete(PlayIdleAnimation); // Tiếp tục chạy idle sau khi shake xong
            }
        }

        private void HandleSubmitRegisterPressed()
        {
            if (inputEmail == null || inputPassword == null) return;
            string email = inputEmail.text;
            string password = inputPassword.text;
            string confirm = inputConfirmPassword != null ? inputConfirmPassword.text : string.Empty;

            if (txtStatusLog != null) txtStatusLog.text = string.Empty;

            if (string.IsNullOrWhiteSpace(email))
            {
                ShowStatusLog("Email cannot be empty.");
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowStatusLog("Password cannot be empty.");
                return;
            }

            if (password.Length < 6)
            {
                ShowStatusLog("Password must be at least 6 characters.");
                return;
            }

            if (password != confirm)
            {
                ShowStatusLog("Passwords do not match.");
                return;
            }

            UIManager.Instance.ShowLoading();
#if FIREBASE_AUTH_ENABLED
            FirebaseAuthService.Instance.RegisterWithEmail(
                email,
                password,
                user =>
                {
                    UIManager.Instance.HideLoading();
                    UIManager.Instance.ShowToast("Account created successfully!");
                    Hide();
                },
                err =>
                {
                    UIManager.Instance.HideLoading();
                    ShowStatusLog($"Error: {err}");
                }
            );
#else
            FirebaseAuthService.Instance.RegisterWithEmail(
                email,
                password,
                uid => {
                    UIManager.Instance.HideLoading();
                    UIManager.Instance.ShowToast("Account created successfully (Mock)!");
                    Hide();
                },
                err => {
                    UIManager.Instance.HideLoading();
                    ShowStatusLog($"Error: {err}");
                }
            );
#endif
        }

        private void HandleSubmitLoginPressed()
        {
            if (inputEmail == null || inputPassword == null) return;
            string email = inputEmail.text;
            string password = inputPassword.text;

            if (txtStatusLog != null) txtStatusLog.text = string.Empty;

            if (string.IsNullOrWhiteSpace(email))
            {
                ShowStatusLog("Email cannot be empty.");
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowStatusLog("Password cannot be empty.");
                return;
            }

            UIManager.Instance.ShowLoading();
#if FIREBASE_AUTH_ENABLED
            FirebaseAuthService.Instance.SignInWithEmail(
                email,
                password,
                user =>
                {
                    UIManager.Instance.HideLoading();
                    UIManager.Instance.ShowToast("Logged in successfully!");
                    Hide();
                },
                err =>
                {
                    UIManager.Instance.HideLoading();
                    ShowStatusLog($"Error: {err}");
                }
            );
#else
            FirebaseAuthService.Instance.SignInWithEmail(
                email,
                password,
                uid => {
                    UIManager.Instance.HideLoading();
                    UIManager.Instance.ShowToast("Logged in successfully (Mock)!");
                    Hide();
                },
                err => {
                    UIManager.Instance.HideLoading();
                    ShowStatusLog($"Error: {err}");
                }
            );
#endif
        }

        private void ResetLayoutState()
        {
            if (animatedContent == null) return;
            animatedContent.anchoredPosition = _contentOriginalPos + new Vector2(0f, -moveOffsetY);
            animatedContent.localScale = Vector3.zero;
            animatedContent.localRotation = Quaternion.identity;
        }

        private void KillActiveTweens()
        {
            _showSequence?.Kill();
            _idleTween?.Kill();
            _confirmFieldTween?.Kill();
            animatedContent?.DOKill();
            transform.DOKill();
        }
    }
}
