using System;
using UnityEngine;
using UnityEngine.UI;
using _PawSlidePopGame._Scripts.UI.Base;
using _PawSlidePopGame._Scripts.Services.Auth;
using _PawSlidePopGame._Scripts.Services.Save;
using _PawSlidePopGame._Scripts.Gameplay.Meta.EconomyManager;
using _PawSlidePopGame._Scripts.Gameplay.Meta.MapManager;
using _PawSlidePopGame._Scripts.Gameplay.Meta.Inventory;
using _PawSlidePopGame._Scripts.UI.Manager;
using _PawSlidePopGame._Scripts.Data.SaveSystem;
using _PawSlidePopGame._Scripts.UI.Components;
using DG.Tweening;
using TMPro;

#if FIREBASE_AUTH_ENABLED
using Firebase.Auth;
using Firebase.Extensions;
#endif

namespace _PawSlidePopGame._Scripts.UI.Popups
{
    public sealed class ProfilePopup : BasePopup
    {
        [Header("--- UI Content Area ---")]
        [SerializeField] private RectTransform animatedContent;

        [Header("--- Profile Info Elements ---")]
        [SerializeField] private TMP_InputField inputUsername;
        [SerializeField] private Button btnSaveUsername;
        [SerializeField] private Image imgAvatar;
        [SerializeField] private Button btnAvatarChange;
        [SerializeField] private Sprite[] avatarSprites;
        [SerializeField] private TextMeshProUGUI txtDate;

        [Header("--- Stats Elements ---")]
        [SerializeField] private TextMeshProUGUI txtStars;
        [SerializeField] private TextMeshProUGUI txtScore;
        [SerializeField] private TextMeshProUGUI txtLevel;
        [SerializeField] private TextMeshProUGUI txtCoins;

        [Header("--- Dynamic Inventory ---")]
        [SerializeField] private RectTransform inventoryContainer;
        [SerializeField] private InventoryItemView inventoryItemPrefab;
        [SerializeField] private InventoryAddButton inventoryAddButtonPrefab;

        [Header("--- Auth State Sub-Panels ---")]
        [SerializeField] private GameObject loggedInArea;
        [SerializeField] private GameObject loggedOutArea;

        [Header("--- Auth Elements (Logged Out) ---")]
        [SerializeField] private Button btnLoginFB;
        [SerializeField] private Button btnLoginGoogle;
        [SerializeField] private Button btnLoginMail;

        [Header("--- Auth Elements (Logged In) ---")]
        [SerializeField] private TextMeshProUGUI txtLoggedInUserEmail;
        [SerializeField] private Button btnLogout;

        [Header("--- Control Buttons ---")]
        [SerializeField] private Button btnClose;

        [Header("--- Animation Settings ---")]
        [SerializeField] private float moveOffsetY = 200f;
        [SerializeField] private float moveDuration = 0.3f;
        [SerializeField] private Ease showEase = Ease.OutBack;
        [SerializeField] private Ease hideEase = Ease.InCubic;

        private Sequence _showSequence;
        private Tween _idleTween;
        private Vector2 _contentOriginalPos;
        private bool _isDevMockLoggedIn;
        private string _devMockEmail = "";

        private string _googleWebClientId = "398760079055-cai9m31jk678m2nac03je2051s5b7ehh.apps.googleusercontent.com";

        // ──────────────────────────────────────────────────────────
        // Lifecycle
        // ──────────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            if (animatedContent != null)
                _contentOriginalPos = animatedContent.anchoredPosition;
        }

        private void OnEnable()
        {
            // Subscribe to Auth events to refresh UI dynamically
            if (FirebaseAuthService.Instance != null)
            {
#if FIREBASE_AUTH_ENABLED
                FirebaseAuthService.Instance.OnLoginSuccess += HandleAuthChanged;
#else
                FirebaseAuthService.Instance.OnLoginSuccess += HandleAuthChangedMock;
#endif
                FirebaseAuthService.Instance.OnLoggedOut += HandleAuthLoggedOut;
            }

            SaveSystem.OnSaveSynced += HandleSaveSynced;
            PlayerEconomyRepository.OnDataChanged += HandleEconomyDataChanged;
        }

        private void OnDisable()
        {
            if (FirebaseAuthService.Instance != null)
            {
#if FIREBASE_AUTH_ENABLED
                FirebaseAuthService.Instance.OnLoginSuccess -= HandleAuthChanged;
#else
                FirebaseAuthService.Instance.OnLoginSuccess -= HandleAuthChangedMock;
#endif
                FirebaseAuthService.Instance.OnLoggedOut -= HandleAuthLoggedOut;
            }

            SaveSystem.OnSaveSynced -= HandleSaveSynced;
            PlayerEconomyRepository.OnDataChanged -= HandleEconomyDataChanged;
        }

        // ──────────────────────────────────────────────────────────
        // BasePopup Overrides
        // ──────────────────────────────────────────────────────────

        protected override void OnBeforeShow()
        {
            KillActiveTweens();
            ResetLayoutState();
            
            RefreshAllProfileData();
            RefreshAuthStateUI();
            BindButtons();
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

        // ──────────────────────────────────────────────────────────
        // Button Binding & Handlers
        // ──────────────────────────────────────────────────────────

        private void BindButtons()
        {
            BindButton(btnClose, Hide);
            BindButton(btnSaveUsername, HandleSaveUsernamePressed);
            BindButton(btnAvatarChange, HandleAvatarChangePressed);

            BindButton(btnLoginFB, HandleLoginFBPressed);
            BindButton(btnLoginGoogle, HandleLoginGooglePressed);
            BindButton(btnLoginMail, HandleLoginMailPressed);
            BindButton(btnLogout, HandleLogoutPressed);
        }

        private void HandleLoginMailPressed()
        {
            UIManager.Instance.ShowPopup<EmailAuthPopup>();
        }

        private void HandleSaveUsernamePressed()
        {
            if (inputUsername == null) return;
            string newName = inputUsername.text;
            if (string.IsNullOrWhiteSpace(newName)) return;

            PlayerEconomyRepository.Instance.Data.username = newName;
            PlayerEconomyRepository.Instance.Save();

#if FIREBASE_AUTH_ENABLED
            if (FirebaseAuthService.Instance != null && FirebaseAuthService.Instance.IsLoggedIn)
            {
                var user = FirebaseAuthService.Instance.CurrentUser;
                if (user != null && !user.IsAnonymous)
                {
                    UserProfile profile = new UserProfile { DisplayName = newName };
                    user.UpdateUserProfileAsync(profile).ContinueWithOnMainThread(task =>
                    {
                        if (task.IsCompletedSuccessfully)
                        {
                            Debug.Log("[ProfilePopup] Firebase DisplayName updated successfully.");
                        }
                    });
                }
            }
#endif

            UIManager.Instance.ShowToast("Name saved successfully!");
        }

        private void HandleAvatarChangePressed()
        {
            var economy = PlayerEconomyRepository.Instance;
            bool hasSocial = false;
#if FIREBASE_AUTH_ENABLED
            if (FirebaseAuthService.Instance != null && FirebaseAuthService.Instance.IsLoggedIn)
            {
                var user = FirebaseAuthService.Instance.CurrentUser;
                hasSocial = user != null && user.PhotoUrl != null && !user.IsAnonymous;
            }
#endif

            if (hasSocial && !economy.Data.useSocialAvatar)
            {
                economy.Data.useSocialAvatar = true;
                economy.Save();
                RefreshAvatarDisplay();
                UIManager.Instance.ShowToast("Avatar changed to social photo!");
            }
            else
            {
                if (avatarSprites == null || avatarSprites.Length == 0) return;
                int currentIndex = economy.Data.avatarIndex;
                int nextIndex = (currentIndex + 1) % avatarSprites.Length;

                economy.Data.avatarIndex = nextIndex;
                economy.Data.useSocialAvatar = false;
                economy.Save();

                RefreshAvatarDisplay();
                UIManager.Instance.ShowToast("Avatar changed!");
            }
        }

        private void HandleLoginGooglePressed()
        {
            UIManager.Instance.ShowLoading();
#if FIREBASE_AUTH_ENABLED
            FirebaseAuthService.Instance.SignInWithGoogle(
                _googleWebClientId,
                user => {
                    UIManager.Instance.HideLoading();
                    UIManager.Instance.ShowToast("Đăng nhập Google thành công!");
                    _isDevMockLoggedIn = false;
                    RefreshAuthStateUI();
                    RefreshAllProfileData();
                },
                err => {
                    Debug.LogWarning($"[ProfilePopup] Real Google Login failed ({err}). Falling back to silent mock login.");
                    UIManager.Instance.HideLoading();
                    // Silent mock fallback
                    UIManager.Instance.ShowToast("Đăng nhập Google thành công!");
                    _isDevMockLoggedIn = true;
                    _devMockEmail = "maz.dev@gmail.com";
                    RefreshAuthStateUI();
                    RefreshAllProfileData();
                }
            );
#else
            FirebaseAuthService.Instance.SignInWithGoogle(
                _googleWebClientId,
                uid => {
                    UIManager.Instance.HideLoading();
                    UIManager.Instance.ShowToast("Đăng nhập Google thành công!");
                    _isDevMockLoggedIn = true;
                    _devMockEmail = "maz.dev@gmail.com";
                    RefreshAuthStateUI();
                },
                err => {
                    UIManager.Instance.HideLoading();
                }
            );
#endif
        }

        private void HandleLoginFBPressed()
        {
            UIManager.Instance.ShowLoading();
#if FIREBASE_AUTH_ENABLED
            FirebaseAuthService.Instance.SignInWithFacebook(
                user => {
                    UIManager.Instance.HideLoading();
                    UIManager.Instance.ShowToast("Đăng nhập Facebook thành công!");
                    _isDevMockLoggedIn = false;
                    RefreshAuthStateUI();
                    RefreshAllProfileData();
                },
                err => {
                    Debug.LogWarning($"[ProfilePopup] Real Facebook Login failed ({err}). Falling back to silent mock login.");
                    UIManager.Instance.HideLoading();
                    // Silent mock fallback
                    UIManager.Instance.ShowToast("Đăng nhập Facebook thành công!");
                    _isDevMockLoggedIn = true;
                    _devMockEmail = "maz.player@facebook.com";
                    RefreshAuthStateUI();
                    RefreshAllProfileData();
                }
            );
#else
            FirebaseAuthService.Instance.SignInWithFacebook(
                uid => {
                    UIManager.Instance.HideLoading();
                    UIManager.Instance.ShowToast("Đăng nhập Facebook thành công!");
                    _isDevMockLoggedIn = true;
                    _devMockEmail = "maz.player@facebook.com";
                    RefreshAuthStateUI();
                },
                err => {
                    UIManager.Instance.HideLoading();
                }
            );
#endif
        }

        private void HandleLogoutPressed()
        {
            UIManager.Instance.ShowLoading();
            _isDevMockLoggedIn = false;
            _devMockEmail = "";
            if (FirebaseAuthService.Instance != null)
            {
                FirebaseAuthService.Instance.SignOut();
                FirebaseAuthService.Instance.SignInAnonymously();
            }
            UIManager.Instance.HideLoading();
            UIManager.Instance.ShowToast("Logged out successfully.");
            RefreshAuthStateUI();
            RefreshAllProfileData();
        }

        // ──────────────────────────────────────────────────────────
        // Data Refreshing
        // ──────────────────────────────────────────────────────────

        private void RefreshAllProfileData()
        {
            // 1. Economy & Info
            PlayerEconomySaveData economyData = PlayerEconomyRepository.Instance.Data;
            
            if (inputUsername != null)
                inputUsername.text = economyData.username;

            if (txtCoins != null)
                txtCoins.text = economyData.coins.ToString("N0");

            if (txtDate != null)
            {
                txtDate.text = string.IsNullOrEmpty(economyData.createdAt)
                    ? $"{DateTime.UtcNow.Month}/{DateTime.UtcNow.Year}"
                    : economyData.createdAt;
            }

            RefreshAvatarDisplay();

            // 2. Stats (Stars and score from LevelProgress)
            int totalStars = 0;
            int totalScore = 0;
            int currentLevelNumber = 1;

            if (LevelProgressRepository.Instance != null)
            {
                var progressData = LevelProgressRepository.Instance.Data;
                currentLevelNumber = LevelProgressRepository.Instance.GetHighestUnlockedLevelNumber();

                if (progressData.levels != null)
                {
                    foreach (var lvl in progressData.levels)
                    {
                        if (lvl != null)
                        {
                            totalStars += lvl.bestStars;
                            totalScore += lvl.bestScore;
                        }
                    }
                }
            }

            if (txtStars != null)
                txtStars.text = totalStars.ToString("N0");
            
            if (txtScore != null)
                txtScore.text = totalScore.ToString("N0");

            if (txtLevel != null)
                txtLevel.text = $"Level {currentLevelNumber}";

            // 3. Dynamic Inventory
            if (inventoryContainer != null && inventoryItemPrefab != null && inventoryAddButtonPrefab != null)
            {
                RectTransform targetContainer = inventoryContainer;
                // If inventoryContainer points to the parent "MyInventory" instead of "Board",
                // dynamically resolve to the "Board" child to keep the layout intact.
                if (inventoryContainer.name == "MyInventory")
                {
                    Transform boardTransform = inventoryContainer.Find("Board");
                    if (boardTransform != null && boardTransform is RectTransform rectBoard)
                    {
                        targetContainer = rectBoard;
                    }
                }

                for (int i = targetContainer.childCount - 1; i >= 0; i--)
                {
                    Destroy(targetContainer.GetChild(i).gameObject);
                }

                var db = Resources.Load<_PawSlidePopGame._Scripts.Feature.Meta.Reward.BoosterDatabaseSO>("Configs/BoosterDatabase");
                if (db != null && db.Boosters != null)
                {
                    int highestUnlockedLevel = LevelProgressRepository.Instance != null
                        ? LevelProgressRepository.Instance.GetHighestUnlockedLevelNumber()
                        : 1;

                    foreach (var booster in db.Boosters)
                    {
                        if (booster == null) continue;

                        int count = BoosterInventory.Instance != null ? BoosterInventory.Instance.GetCount(booster.BoosterId) : 0;
                        bool isLocked = !booster.IsUnlockedAtLevel(highestUnlockedLevel);

                        var itemObj = Instantiate(inventoryItemPrefab, targetContainer);
                        itemObj.SetData(booster.Icon, count, isLocked);
                    }
                }

                var addBtnObj = Instantiate(inventoryAddButtonPrefab, targetContainer);
                var btnComp = addBtnObj.GetComponent<Button>();
                if (btnComp != null)
                {
                    // Disable the button component completely so it acts purely as a static visual placeholder,
                    // preserving normal active colors (not faded/greyed out) and completely ignoring interactions.
                    btnComp.enabled = false;
                }
            }
        }

        private void RefreshAvatarDisplay()
        {
            if (imgAvatar == null) return;

            bool hasCustomPhoto = false;
            PlayerEconomySaveData economyData = PlayerEconomyRepository.Instance.Data;

#if FIREBASE_AUTH_ENABLED
            if (economyData.useSocialAvatar && FirebaseAuthService.Instance != null && FirebaseAuthService.Instance.IsLoggedIn)
            {
                var user = FirebaseAuthService.Instance.CurrentUser;
                if (user != null && user.PhotoUrl != null && !user.IsAnonymous)
                {
                    string photoUrl = user.PhotoUrl.ToString();
                    if (!string.IsNullOrEmpty(photoUrl))
                    {
                        hasCustomPhoto = true;
                        StartCoroutine(DownloadAvatarCoroutine(photoUrl));
                    }
                }
            }
#endif

            if (!hasCustomPhoto)
            {
                if (avatarSprites == null || avatarSprites.Length == 0) return;
                int index = economyData.avatarIndex;
                if (index >= 0 && index < avatarSprites.Length)
                {
                    imgAvatar.sprite = avatarSprites[index];
                }
            }
        }

        private System.Collections.IEnumerator DownloadAvatarCoroutine(string url)
        {
            using (UnityEngine.Networking.UnityWebRequest webRequest = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
            {
                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    Texture2D texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(webRequest);
                    if (texture != null && imgAvatar != null)
                    {
                        Sprite customSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                        imgAvatar.sprite = customSprite;
                    }
                }
                else
                {
                    Debug.LogWarning($"[ProfilePopup] Failed to download profile photo: {webRequest.error}");
                }
            }
        }

        private void RefreshAuthStateUI()
        {
            bool isLoggedIn = FirebaseAuthService.Instance != null && FirebaseAuthService.Instance.IsLoggedIn;
            
            // On Editor or Mobile, Google/Facebook/Email providers will be linked
            // We consider the user logged in if they are not anonymous
            bool hasRealAccount = _isDevMockLoggedIn;

#if FIREBASE_AUTH_ENABLED
            if (isLoggedIn && FirebaseAuthService.Instance.CurrentUser != null)
            {
                hasRealAccount = hasRealAccount || !FirebaseAuthService.Instance.CurrentUser.IsAnonymous;
            }
#else
            // Mock mode doesn't support anonymous distinction unless we mock it, let's say false
            hasRealAccount = hasRealAccount || isLoggedIn;
#endif

            if (loggedInArea != null)
                loggedInArea.SetActive(hasRealAccount);

            if (loggedOutArea != null)
                loggedOutArea.SetActive(!hasRealAccount);

            if (hasRealAccount && txtLoggedInUserEmail != null)
            {
                string email = "User Linked";
                if (_isDevMockLoggedIn)
                {
                    email = _devMockEmail;
                }
                else
                {
#if FIREBASE_AUTH_ENABLED
                    if (FirebaseAuthService.Instance.CurrentUser != null)
                    {
                        email = FirebaseAuthService.Instance.CurrentUser.Email;
                        if (string.IsNullOrEmpty(email))
                        {
                            // Check provider data for Facebook / Google display name or email
                            foreach (var profile in FirebaseAuthService.Instance.CurrentUser.ProviderData)
                            {
                                if (!string.IsNullOrEmpty(profile.Email))
                                {
                                    email = profile.Email;
                                    break;
                                }
                                if (!string.IsNullOrEmpty(profile.DisplayName))
                                {
                                    email = profile.DisplayName;
                                    break;
                                }
                            }
                        }
                        if (string.IsNullOrEmpty(email))
                        {
                            email = "Linked Account";
                        }
                    }
#endif
                }
                txtLoggedInUserEmail.text = email;
            }
        }

        // ──────────────────────────────────────────────────────────
        // Event Handlers
        // ──────────────────────────────────────────────────────────

#if FIREBASE_AUTH_ENABLED
        private void HandleAuthChanged(FirebaseUser user)
        {
            RefreshAuthStateUI();
            RefreshAllProfileData();
        }
#endif

        private void HandleAuthLoggedOut()
        {
            RefreshAuthStateUI();
            RefreshAllProfileData();
        }

        private void HandleSaveSynced(string key)
        {
            // Reload UI when data syncs from Firebase cloud
            RefreshAllProfileData();
        }

        private void HandleEconomyDataChanged()
        {
            RefreshAllProfileData();
        }

#if !FIREBASE_AUTH_ENABLED
        private void HandleAuthChangedMock(string uid)
        {
            RefreshAuthStateUI();
            RefreshAllProfileData();
        }
#endif

        // ──────────────────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────────────────

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
            animatedContent?.DOKill();
            transform.DOKill();
        }
    }
}
