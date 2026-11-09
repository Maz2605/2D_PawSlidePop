using System;
using UnityEngine;
using _PawSlidePopGame._Scripts.Core.Boostrap;
using _PawSlidePopGame._Scripts.Services.Analytics;

#if FIREBASE_AUTH_ENABLED
using Firebase.Auth;
using Firebase.Extensions;
#endif

#if GOOGLE_SIGNIN_ENABLED
using Google;
#endif

#if FACEBOOK_SDK_ENABLED
using Facebook.Unity;
#endif

namespace _PawSlidePopGame._Scripts.Services.Auth
{
    [DisallowMultipleComponent]
    public class FirebaseAuthService : MonoBehaviour, IAppService
    {
        public static FirebaseAuthService Instance { get; private set; }
        public static bool IsInitialized { get; private set; }

#if FIREBASE_AUTH_ENABLED
        private FirebaseAuth _auth;
        private FirebaseUser _cachedUser;
        
        public FirebaseUser CurrentUser => _auth?.CurrentUser;
        public string UserId => CurrentUser?.UserId;
        public bool IsLoggedIn => CurrentUser != null;
        
        public event Action<FirebaseUser> OnLoginSuccess;
        public event Action<string> OnLoginFailed;
        public event Action OnLoggedOut;
#else
        public string UserId => "LocalPlayer";
        public bool IsLoggedIn => false;
        
        public event Action<string> OnLoginSuccess; // Fallback dummy signature
        public event Action<string> OnLoginFailed;
        public event Action OnLoggedOut;
#endif

        private bool _initStarted;

        public void Init()
        {
            if (_initStarted)
            {
                return;
            }

            _initStarted = true;
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (FirebaseService.IsReady)
            {
                InitializeAuth();
            }
            else
            {
                FirebaseService.OnReady += InitializeAuth;
            }
        }

        private void InitializeAuth()
        {
            FirebaseService.OnReady -= InitializeAuth;

#if FIREBASE_AUTH_ENABLED
            try
            {
                _auth = FirebaseAuth.DefaultInstance;
                _auth.StateChanged += AuthStateChanged;
                _cachedUser = _auth.CurrentUser;
                IsInitialized = true;
                Debug.Log("[FirebaseAuthService] Firebase Auth Initialized successfully.");

                // Tự động đăng nhập ẩn danh khi khởi động nếu chưa có user
                if (!IsLoggedIn)
                {
                    SignInAnonymously();
                }
                else
                {
                    Debug.Log($"[FirebaseAuthService] User already logged in: {CurrentUser.UserId}");
                    OnLoginSuccess?.Invoke(CurrentUser);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseAuthService] Failed to initialize Firebase Auth: {e.Message}");
            }
#else
            Debug.LogWarning("[FirebaseAuthService] FIREBASE_AUTH_ENABLED is not defined. Firebase Auth is running in mock/local mode.");
            IsInitialized = true;
#endif
        }

#if FIREBASE_AUTH_ENABLED
        private void HandleLoginSuccessInternal(FirebaseUser user)
        {
            if (user == null) return;
            if (_cachedUser != null && _cachedUser.UserId == user.UserId)
            {
                return;
            }

            _cachedUser = user;
            Debug.Log($"[FirebaseAuthService] Handling Login/Link Event for UID: {user.UserId}");
            OnLoginSuccess?.Invoke(user);
        }

        private void AuthStateChanged(object sender, EventArgs eventArgs)
        {
            FirebaseUser user = _auth.CurrentUser;
            string currentUid = user?.UserId;
            string cachedUid = _cachedUser?.UserId;

            if (currentUid != cachedUid)
            {
                if (user != null)
                {
                    HandleLoginSuccessInternal(user);
                }
                else
                {
                    _cachedUser = null;
                    Debug.Log("[FirebaseAuthService] User signed out.");
                    OnLoggedOut?.Invoke();
                }
            }
        }

        public void SignInAnonymously()
        {
            if (!IsInitialized || _auth == null)
            {
                Debug.LogError("[FirebaseAuthService] Cannot Sign In: Auth not initialized.");
                OnLoginFailed?.Invoke("Auth not initialized");
                return;
            }

            Debug.Log("[FirebaseAuthService] Attempting to Sign In Anonymously...");
            _auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled)
                {
                    Debug.LogError("[FirebaseAuthService] Anonymous sign-in was canceled.");
                    OnLoginFailed?.Invoke("Canceled");
                    return;
                }
                if (task.IsFaulted)
                {
                    Debug.LogError($"[FirebaseAuthService] Anonymous sign-in failed: {task.Exception}");
                    OnLoginFailed?.Invoke(task.Exception?.Message ?? "Unknown error");
                    return;
                }

                AuthResult result = task.Result;
                Debug.Log($"[FirebaseAuthService] Anonymous sign-in successful. UID: {result.User.UserId}");
                HandleLoginSuccessInternal(result.User);
            });
        }

        public void SignOut()
        {
            if (_auth != null && IsLoggedIn)
            {
                _auth.SignOut();
            }
        }

        private void UpdateDisplayNameFromGame(FirebaseUser user)
        {
            if (user == null) return;
            string gameUsername = _PawSlidePopGame._Scripts.Gameplay.Meta.EconomyManager.PlayerEconomyRepository.Instance.Data.username;
            if (!string.IsNullOrEmpty(gameUsername) && gameUsername != "Player")
            {
                UserProfile profile = new UserProfile { DisplayName = gameUsername };
                user.UpdateUserProfileAsync(profile).ContinueWithOnMainThread(t => {
                    if (t.IsCompletedSuccessfully)
                    {
                        Debug.Log($"[FirebaseAuthService] DisplayName sync success: {gameUsername}");
                    }
                });
            }
        }

        public void LinkAccount(Credential credential, Action<FirebaseUser> onSuccess, Action<string> onFailure)
        {
            if (CurrentUser == null)
            {
                Debug.LogError("[FirebaseAuthService] Cannot link account: No current user logged in.");
                onFailure?.Invoke("No current user logged in.");
                return;
            }

            Debug.Log("[FirebaseAuthService] Attempting to link current account with new credential...");
            CurrentUser.LinkWithCredentialAsync(credential).ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled)
                {
                    Debug.LogError("[FirebaseAuthService] Account linking was canceled.");
                    onFailure?.Invoke("Canceled");
                    return;
                }
                if (task.IsFaulted)
                {
                    Debug.LogError($"[FirebaseAuthService] Account linking failed: {task.Exception}");
                    onFailure?.Invoke(task.Exception?.Message ?? "Unknown error");
                    return;
                }

                AuthResult result = task.Result;
                Debug.Log($"[FirebaseAuthService] Account linked successfully! UID: {result.User.UserId}");
                UpdateDisplayNameFromGame(result.User);
                onSuccess?.Invoke(result.User);
            });
        }

        public void SignInWithGoogle(string webClientId, Action<FirebaseUser> onSuccess, Action<string> onFailure)
        {
#if GOOGLE_SIGNIN_ENABLED && FIREBASE_AUTH_ENABLED
            Debug.Log("[FirebaseAuthService] Configuring Google Sign-In...");
            var config = new GoogleSignInConfiguration
            {
                WebClientId = webClientId,
                RequestIdToken = true,
                RequestEmail = true
            };
            GoogleSignIn.Configuration = config;

            Debug.Log("[FirebaseAuthService] Starting Google Sign-In...");
            GoogleSignIn.DefaultInstance.SignIn().ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled)
                {
                    Debug.LogError("[FirebaseAuthService] Google Sign-In was canceled.");
                    onFailure?.Invoke("Canceled");
                    return;
                }
                if (task.IsFaulted)
                {
                    Debug.LogError($"[FirebaseAuthService] Google Sign-In failed: {task.Exception}");
                    onFailure?.Invoke(task.Exception?.Message ?? "Unknown error");
                    return;
                }

                GoogleSignInUser googleUser = task.Result;
                Debug.Log($"[FirebaseAuthService] Google Sign-In successful. Email: {googleUser.Email}");

                // Create Firebase Credential
                Credential credential = GoogleAuthProvider.GetCredential(googleUser.IdToken, null);

                // Link with current Anonymous Account (or Sign In if not logged in)
                if (IsLoggedIn)
                {
                    LinkAccount(credential, onSuccess, onFailure);
                }
                else
                {
                    _auth.SignInWithCredentialAsync(credential).ContinueWithOnMainThread((System.Threading.Tasks.Task<FirebaseUser> signInTask) =>
                    {
                        if (signInTask.IsFaulted || signInTask.IsCanceled)
                        {
                            Debug.LogError($"[FirebaseAuthService] Firebase Google Sign-In failed: {signInTask.Exception}");
                            onFailure?.Invoke(signInTask.Exception?.Message ?? "Sign in failed");
                        }
                        else
                        {
                            FirebaseUser user = signInTask.Result;
                            Debug.Log($"[FirebaseAuthService] Firebase Google Sign-In successful! UID: {user.UserId}");
                            onSuccess?.Invoke(user);
                        }
                    });
                }
            });
#else
            Debug.LogWarning("[FirebaseAuthService] GOOGLE_SIGNIN_ENABLED is not defined. Google Sign-In running in mock mode.");
            onSuccess?.Invoke(null); // Mock success
#endif
        }

        public void SignInWithFacebook(Action<FirebaseUser> onSuccess, Action<string> onFailure)
        {
#if FACEBOOK_SDK_ENABLED && FIREBASE_AUTH_ENABLED
            if (!FB.IsInitialized)
            {
                Debug.Log("[FirebaseAuthService] Initializing Facebook SDK...");
                FB.Init(() =>
                {
                    FB.ActivateApp();
                    PerformFacebookLogin(onSuccess, onFailure);
                }, 
                isGameShown =>
                {
                    Time.timeScale = isGameShown ? 1 : 0;
                });
            }
            else
            {
                PerformFacebookLogin(onSuccess, onFailure);
            }
#else
            Debug.LogWarning("[FirebaseAuthService] FACEBOOK_SDK_ENABLED is not defined. Facebook Sign-In running in mock mode.");
            onSuccess?.Invoke(null); // Mock success
#endif
        }

        public void RegisterWithEmail(string email, string password, Action<FirebaseUser> onSuccess, Action<string> onFailure)
        {
            if (!IsInitialized || _auth == null)
            {
                onFailure?.Invoke("Auth not initialized");
                return;
            }

            Debug.Log($"[FirebaseAuthService] Registering with Email: {email}...");
            Credential credential = EmailAuthProvider.GetCredential(email, password);

            if (IsLoggedIn && CurrentUser.IsAnonymous)
            {
                LinkAccount(credential, 
                    user => {
                        UpdateDisplayNameFromGame(user);
                        onSuccess?.Invoke(user);
                    }, 
                    onFailure);
            }
            else
            {
                _auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
                {
                    if (task.IsCanceled)
                    {
                        onFailure?.Invoke("Canceled");
                        return;
                    }
                    if (task.IsFaulted)
                    {
                        onFailure?.Invoke(task.Exception?.GetBaseException()?.Message ?? "Registration failed");
                        return;
                    }

                    AuthResult result = task.Result;
                    Debug.Log($"[FirebaseAuthService] Registration successful! UID: {result.User.UserId}");
                    UpdateDisplayNameFromGame(result.User);
                    HandleLoginSuccessInternal(result.User);
                    onSuccess?.Invoke(result.User);
                });
            }
        }

        public void SignInWithEmail(string email, string password, Action<FirebaseUser> onSuccess, Action<string> onFailure)
        {
            if (!IsInitialized || _auth == null)
            {
                onFailure?.Invoke("Auth not initialized");
                return;
            }

            Debug.Log($"[FirebaseAuthService] Signing in with Email: {email}...");
            Credential credential = EmailAuthProvider.GetCredential(email, password);

            if (IsLoggedIn && CurrentUser.IsAnonymous)
            {
                LinkAccount(credential, onSuccess, err =>
                {
                    Debug.LogWarning($"[FirebaseAuthService] Link failed (email might be in use), signing in directly: {err}");
                    PerformEmailSignIn(credential, onSuccess, onFailure);
                });
            }
            else
            {
                PerformEmailSignIn(credential, onSuccess, onFailure);
            }
        }

        private void PerformEmailSignIn(Credential credential, Action<FirebaseUser> onSuccess, Action<string> onFailure)
        {
            _auth.SignInWithCredentialAsync(credential).ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled)
                {
                    onFailure?.Invoke("Canceled");
                    return;
                }
                if (task.IsFaulted)
                {
                    onFailure?.Invoke(task.Exception?.GetBaseException()?.Message ?? "Sign in failed");
                    return;
                }

                FirebaseUser user = task.Result;
                Debug.Log($"[FirebaseAuthService] Email Sign-In successful! UID: {user.UserId}");
                HandleLoginSuccessInternal(user);
                onSuccess?.Invoke(user);
            });
        }


#if FACEBOOK_SDK_ENABLED && FIREBASE_AUTH_ENABLED
        private void PerformFacebookLogin(Action<FirebaseUser> onSuccess, Action<string> onFailure)
        {
            var permissions = new System.Collections.Generic.List<string>() { "public_profile", "email" };
            Debug.Log("[FirebaseAuthService] Starting Facebook Login...");
            FB.LogInWithReadPermissions(permissions, result =>
            {
                if (result == null || !string.IsNullOrEmpty(result.Error))
                {
                    string err = result?.Error ?? "Unknown error";
                    Debug.LogError($"[FirebaseAuthService] Facebook Login failed: {err}");
                    onFailure?.Invoke(err);
                    return;
                }

                if (result.Cancelled)
                {
                    Debug.LogWarning("[FirebaseAuthService] Facebook Login was canceled by user.");
                    onFailure?.Invoke("Canceled");
                    return;
                }

                if (FB.IsLoggedIn)
                {
                    string accessToken = Facebook.Unity.AccessToken.CurrentAccessToken.TokenString;
                    Debug.Log("[FirebaseAuthService] Facebook Login successful. Token retrieved.");

                    // Create Firebase Credential
                    Credential credential = FacebookAuthProvider.GetCredential(accessToken);

                    // Link with current Anonymous Account (or Sign In if not logged in)
                    if (IsLoggedIn)
                    {
                        LinkAccount(credential, onSuccess, onFailure);
                    }
                    else
                    {
                        _auth.SignInWithCredentialAsync(credential).ContinueWithOnMainThread((System.Threading.Tasks.Task<FirebaseUser> signInTask) =>
                        {
                            if (signInTask.IsFaulted || signInTask.IsCanceled)
                            {
                                Debug.LogError($"[FirebaseAuthService] Firebase Facebook Sign-In failed: {signInTask.Exception}");
                                onFailure?.Invoke(signInTask.Exception?.Message ?? "Sign in failed");
                            }
                            else
                            {
                                FirebaseUser user = signInTask.Result;
                                Debug.Log($"[FirebaseAuthService] Firebase Facebook Sign-In successful! UID: {user.UserId}");
                                onSuccess?.Invoke(user);
                            }
                        });
                    }
                }
                else
                {
                    Debug.LogError("[FirebaseAuthService] Facebook Login failed: LoggedIn is false after call.");
                    onFailure?.Invoke("Login failed");
                }
            });
        }
#endif
#else
        public void SignInAnonymously()
        {
            Debug.Log("[FirebaseAuthService] Mock Sign In Anonymously successful. UID: LocalPlayer");
            OnLoginSuccess?.Invoke("LocalPlayer");
        }

        public void SignOut()
        {
            Debug.Log("[FirebaseAuthService] Mock Sign Out.");
            OnLoggedOut?.Invoke();
        }

        public void LinkAccount(object credential, Action<string> onSuccess, Action<string> onFailure)
        {
            Debug.Log("[FirebaseAuthService] Mock Link Account successful.");
            onSuccess?.Invoke("LocalPlayer");
        }

        public void SignInWithGoogle(string webClientId, Action<string> onSuccess, Action<string> onFailure)
        {
            Debug.Log("[FirebaseAuthService] Mock Sign In With Google successful.");
            onSuccess?.Invoke("LocalPlayer");
        }

        public void SignInWithFacebook(Action<string> onSuccess, Action<string> onFailure)
        {
            Debug.Log("[FirebaseAuthService] Mock Sign In With Facebook successful.");
            onSuccess?.Invoke("LocalPlayer");
        }

        public void RegisterWithEmail(string email, string password, Action<string> onSuccess, Action<string> onFailure)
        {
            Debug.Log($"[FirebaseAuthService] Mock Register successful for {email}.");
            OnLoginSuccess?.Invoke("LocalPlayer");
            onSuccess?.Invoke("LocalPlayer");
        }

        public void SignInWithEmail(string email, string password, Action<string> onSuccess, Action<string> onFailure)
        {
            Debug.Log($"[FirebaseAuthService] Mock Sign In successful for {email}.");
            OnLoginSuccess?.Invoke("LocalPlayer");
            onSuccess?.Invoke("LocalPlayer");
        }
#endif

        private void OnDestroy()
        {
#if FIREBASE_AUTH_ENABLED
            if (_auth != null)
            {
                _auth.StateChanged -= AuthStateChanged;
            }
#endif
            if (Instance == this)
            {
                Instance = null;
                IsInitialized = false;
            }
        }
    }
}
