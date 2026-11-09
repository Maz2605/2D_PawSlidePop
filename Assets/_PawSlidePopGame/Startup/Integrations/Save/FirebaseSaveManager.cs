using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using _PawSlidePopGame._Scripts.Core.Boostrap;
using _PawSlidePopGame._Scripts.Data.SaveSystem;
using _PawSlidePopGame._Scripts.Gameplay.Meta.EconomyManager;
using _PawSlidePopGame._Scripts.Services.Auth;
using _PawSlidePopGame._Scripts.Gameplay.Meta.MapManager;
using _PawSlidePopGame._Scripts.Feature.Meta.Wheel;

#if FIREBASE_AUTH_ENABLED
using Firebase.Auth;
#endif

#if FIREBASE_FIRESTORE_ENABLED
using Firebase.Firestore;
using Firebase.Extensions;
#endif

namespace _PawSlidePopGame._Scripts.Services.Save
{
    [DisallowMultipleComponent]
    public class FirebaseSaveManager : MonoBehaviour, IAppService
    {
        public static FirebaseSaveManager Instance { get; private set; }
        public static bool IsInitialized { get; private set; }

        private bool _initStarted;
        private System.Threading.SynchronizationContext _mainThreadContext;
        private FileSystemWatcher _fileWatcher;
        private readonly Dictionary<string, DateTime> _lastLocalWriteTimes = new Dictionary<string, DateTime>();

#if FIREBASE_AUTH_ENABLED && FIREBASE_FIRESTORE_ENABLED
        private Firebase.Firestore.ListenerRegistration _cloudListenerRegistration;
#endif

        public void Init()
        {
            if (_initStarted) return;
            _initStarted = true;
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeSaveManager();
        }

        private void InitializeSaveManager()
        {
            _mainThreadContext = System.Threading.SynchronizationContext.Current;
            SaveSystem.OnBeforeLocalSaveWrite += HandleBeforeLocalSaveWrite;
            SetupLocalFileWatcher();

#if FIREBASE_AUTH_ENABLED && FIREBASE_FIRESTORE_ENABLED
            SaveSystem.OnSaveCompleted += HandleLocalSaveCompleted;
            
            if (FirebaseAuthService.Instance != null)
            {
                FirebaseAuthService.Instance.OnLoginSuccess += HandleLoginSuccess;
                FirebaseAuthService.Instance.OnLoggedOut += HandleLoggedOut;
                if (FirebaseAuthService.Instance.IsLoggedIn)
                {
                    HandleLoginSuccess(FirebaseAuthService.Instance.CurrentUser);
                }
            }
            IsInitialized = true;
            Debug.Log("[FirebaseSaveManager] Initialized successfully with Firestore support.");
#else
            Debug.LogWarning("[FirebaseSaveManager] FIREBASE_FIRESTORE_ENABLED or FIREBASE_AUTH_ENABLED is not defined. Running in mock/local mode.");
            IsInitialized = true;
#endif
        }

#if FIREBASE_AUTH_ENABLED && FIREBASE_FIRESTORE_ENABLED
        private void HandleLocalSaveCompleted(string gameId, string rawJson)
        {
            if (FirebaseAuthService.Instance == null || !FirebaseAuthService.Instance.IsLoggedIn)
            {
                return;
            }

            string userId = FirebaseAuthService.Instance.UserId;
            if (string.IsNullOrEmpty(userId)) return;

            // Đẩy tất cả dữ liệu hiện có lên cloud dưới dạng 1 Batch
            UploadAllDataToCloud(userId);
        }

        private void HandleLoginSuccess(FirebaseUser user)
        {
            if (user == null) return;
            string userId = user.UserId;
            if (string.IsNullOrEmpty(userId)) return;

            _mainThreadContext?.Post(_ =>
            {
                Debug.Log($"[FirebaseSaveManager] Login success. Starting Cloud Save sync for UID: {userId}");
                SyncAllDataFromCloud(userId);
            }, null);
        }

        private void HandleLoggedOut()
        {
            _mainThreadContext?.Post(_ =>
            {
                ClearLocalDataAndReset();
            }, null);
        }

        private void ClearLocalDataAndReset()
        {
            Debug.Log("[FirebaseSaveManager] Clearing all local save data and resetting repositories...");
            try
            {
                // 1. Delete/Reset PlayerEconomyRepository
                if (PlayerEconomyRepository.Instance != null)
                {
                    PlayerEconomyRepository.Instance.DeleteSave();
                    PlayerEconomyRepository.Instance.Reload();
                }

                // 2. Delete/Reset LevelProgressRepository
                if (LevelProgressRepository.Instance != null)
                {
                    LevelProgressRepository.Instance.DeleteSave();
                    LevelProgressRepository.Instance.Reload();
                }

                // 3. Delete/Reset WheelStateRepository
                var tempWheelRepo = new WheelStateRepository();
                tempWheelRepo.DeleteSave();
                tempWheelRepo.Reload();

                // 4. Clear last synced user ID from PlayerPrefs
                PlayerPrefs.DeleteKey("LastSyncedUserId");
                PlayerPrefs.Save();
                
                Debug.Log("[FirebaseSaveManager] Local save data cleared and in-memory repositories reset successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FirebaseSaveManager] Error during ClearLocalDataAndReset: {ex.Message}");
            }
        }

        private void UploadAllDataToCloud(string userId)
        {
            try
            {
                FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
                WriteBatch batch = db.StartBatch();
                bool hasUpdates = false;

                string[] syncKeys = { "player_economy", "level_progress", "wheel_state" };
                foreach (string key in syncKeys)
                {
                    string localPath = SaveSystem.GetPath(key);
                    if (File.Exists(localPath))
                    {
                        string localJson = SaveSystem.LoadRawJson(key);
                        if (!string.IsNullOrEmpty(localJson))
                        {
                            DateTime localTime = File.GetLastWriteTimeUtc(localPath);
                            DocumentReference docRef = db.Collection("users").Document(userId).Collection("data").Document(key);

                            Dictionary<string, object> data = new Dictionary<string, object>
                            {
                                { "json", localJson },
                                { "timestamp", localTime.ToString("o") }
                            };

                            batch.Set(docRef, data);
                            hasUpdates = true;
                        }
                    }
                }

                if (hasUpdates)
                {
                    Debug.Log($"[FirebaseSaveManager] Uploading ALL local data to cloud in a single batch...");
                    batch.CommitAsync().ContinueWithOnMainThread(task =>
                    {
                        if (task.IsFaulted || task.IsCanceled)
                        {
                            Debug.LogError($"[FirebaseSaveManager] Failed to upload batch to cloud: {task.Exception}");
                        }
                        else
                        {
                            Debug.Log($"[FirebaseSaveManager] Successfully uploaded all data to cloud in batch.");
                        }
                    });
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseSaveManager] Error during batch upload: {e.Message}");
            }
        }

        private void SyncAllDataFromCloud(string userId)
        {
            try
            {
                FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
                CollectionReference colRef = db.Collection("users").Document(userId).Collection("data");

                // Check if account has switched
                string lastSyncedUid = PlayerPrefs.GetString("LastSyncedUserId", "");
                bool isNewUser = lastSyncedUid != userId;

                _cloudListenerRegistration?.Stop();
                _cloudListenerRegistration = colRef.Listen(snapshot =>
                {
                    if (snapshot == null) return;

                    _mainThreadContext.Post(_ =>
                    {
                        try
                        {
                            HashSet<string> cloudKeys = new HashSet<string>();
                            bool needsLocalToCloudUpload = false;

                            foreach (DocumentSnapshot doc in snapshot.Documents)
                            {
                                string gameId = doc.Id;
                                cloudKeys.Add(gameId);
                                if (!doc.ContainsField("json")) continue;

                                string cloudJson = doc.GetValue<string>("json");
                                string localPath = SaveSystem.GetPath(gameId);
                                bool localExists = File.Exists(localPath);
                                string localJson = localExists ? SaveSystem.LoadRawJson(gameId) : null;

                                bool needsSyncToLocal = false;

                                if (!localExists)
                                {
                                    needsSyncToLocal = true;
                                }
                                else if (cloudJson != localJson)
                                {
                                    if (isNewUser)
                                    {
                                        // New user logged in, overwrite local with cloud data
                                        needsSyncToLocal = true;
                                    }
                                    else
                                    {
                                        // Same user, resolve conflict via timestamps
                                        DateTime cloudTime = DateTime.MinValue;
                                        if (doc.ContainsField("timestamp"))
                                        {
                                            string tsStr = doc.GetValue<string>("timestamp");
                                            if (DateTime.TryParse(tsStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime parsedTime))
                                            {
                                                cloudTime = parsedTime.ToUniversalTime();
                                            }
                                        }

                                        DateTime localTime = File.GetLastWriteTimeUtc(localPath);

                                        if (cloudTime > localTime)
                                        {
                                            if (!doc.Metadata.HasPendingWrites)
                                            {
                                                needsSyncToLocal = true;
                                            }
                                        }
                                        else if (localTime > cloudTime)
                                        {
                                            needsLocalToCloudUpload = true;
                                        }
                                    }
                                }

                                if (needsSyncToLocal)
                                {
                                    Debug.Log($"[FirebaseSaveManager] Cloud data change for '{gameId}' detected (Cloud is newer). Syncing to local...");
                                    
                                    // Đánh dấu thời gian để FileWatcher bỏ qua sự kiện ghi file này (chống lặp)
                                    lock (_lastLocalWriteTimes)
                                    {
                                        _lastLocalWriteTimes[gameId] = DateTime.UtcNow;
                                    }

                                    SaveSystem.SaveRawJson(gameId, cloudJson);
                                }
                                else
                                {
                                    Debug.Log($"[FirebaseSaveManager] '{gameId}' is in sync.");
                                }
                            }

                            // Update last synced user ID
                            if (isNewUser)
                            {
                                PlayerPrefs.SetString("LastSyncedUserId", userId);
                                PlayerPrefs.Save();
                                isNewUser = false;
                            }

                            // Nếu Cloud thiếu bất kỳ key nào, hoặc local có thay đổi mới hơn
                            bool cloudMissingData = false;
                            string[] syncKeys = { "player_economy", "level_progress", "wheel_state" };
                            foreach (string key in syncKeys)
                            {
                                if (!cloudKeys.Contains(key))
                                {
                                    cloudMissingData = true;
                                    break;
                                }
                            }

                            if (cloudMissingData || needsLocalToCloudUpload)
                            {
                                Debug.Log("[FirebaseSaveManager] Cloud is missing data keys or local data is newer. Uploading all local data in batch...");
                                UploadAllDataToCloud(userId);
                            }

                            // Tự động cập nhật tên người chơi từ tài khoản liên kết nếu tên hiện tại trong game là mặc định
                            var currentUser = FirebaseAuthService.Instance.CurrentUser;
                            if (currentUser != null && !currentUser.IsAnonymous)
                            {
                                string socialName = currentUser.DisplayName;
                                if (!string.IsNullOrEmpty(socialName))
                                {
                                    var economy = PlayerEconomyRepository.Instance;
                                    if (economy != null && economy.Data != null)
                                    {
                                        string currentName = economy.Data.username;
                                        if (string.IsNullOrEmpty(currentName) || currentName == "Player" || 
                                            (currentName.StartsWith("user") && currentName.Length == 7 && int.TryParse(currentName.Substring(4), out int dummy)))
                                        {
                                            Debug.Log($"[FirebaseSaveManager] Setting default username '{currentName}' to social name '{socialName}'");
                                            economy.Data.username = socialName;
                                            economy.Save();
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception innerEx)
                        {
                            Debug.LogError($"[FirebaseSaveManager] Exception in main thread dispatcher: {innerEx.Message}");
                        }
                    }, null);
                });
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseSaveManager] Error during sync: {e.Message}");
            }
        }
#endif

        private void OnDestroy()
        {
            SaveSystem.OnBeforeLocalSaveWrite -= HandleBeforeLocalSaveWrite;

            if (_fileWatcher != null)
            {
                _fileWatcher.Changed -= OnLocalFileChanged;
                _fileWatcher.Created -= OnLocalFileChanged;
                _fileWatcher.Dispose();
                _fileWatcher = null;
            }

#if FIREBASE_AUTH_ENABLED && FIREBASE_FIRESTORE_ENABLED
            _cloudListenerRegistration?.Stop();
            _cloudListenerRegistration = null;

            SaveSystem.OnSaveCompleted -= HandleLocalSaveCompleted;
            if (FirebaseAuthService.Instance != null)
            {
                FirebaseAuthService.Instance.OnLoginSuccess -= HandleLoginSuccess;
                FirebaseAuthService.Instance.OnLoggedOut -= HandleLoggedOut;
            }
#endif
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void HandleBeforeLocalSaveWrite(string gameId)
        {
            lock (_lastLocalWriteTimes)
            {
                _lastLocalWriteTimes[gameId] = DateTime.UtcNow;
            }
        }

        private void SetupLocalFileWatcher()
        {
            try
            {
                string path = Application.persistentDataPath;
                if (!Directory.Exists(path)) return;

                _fileWatcher = new FileSystemWatcher
                {
                    Path = path,
                    Filter = "*.json",
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size
                };

                _fileWatcher.Changed += OnLocalFileChanged;
                _fileWatcher.Created += OnLocalFileChanged;
                _fileWatcher.EnableRaisingEvents = true;

                Debug.Log($"[FirebaseSaveManager] Local file watcher started on: {path}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseSaveManager] Failed to setup file watcher: {e.Message}");
            }
        }

        private void OnLocalFileChanged(object sender, FileSystemEventArgs e)
        {
            string fileName = Path.GetFileNameWithoutExtension(e.FullPath);
            if (fileName != "player_economy" && fileName != "level_progress" && fileName != "wheel_state")
            {
                return;
            }

            lock (_lastLocalWriteTimes)
            {
                if (_lastLocalWriteTimes.TryGetValue(fileName, out DateTime lastWrite))
                {
                    if ((DateTime.UtcNow - lastWrite).TotalSeconds < 1.5f)
                    {
                        return;
                    }
                }
            }

            _mainThreadContext?.Post(_ =>
            {
                if (gameObject != null && gameObject.activeInHierarchy)
                {
                    StartCoroutine(DelayedReloadLocalFile(fileName, e.FullPath));
                }
            }, null);
        }

        private System.Collections.IEnumerator DelayedReloadLocalFile(string gameId, string fullPath)
        {
            yield return new WaitForSecondsRealtime(0.15f);

            try
            {
                if (!File.Exists(fullPath)) yield break;

                string diskJson = SaveSystem.LoadRawJson(gameId);
                if (string.IsNullOrEmpty(diskJson)) yield break;

                Debug.Log($"[FirebaseSaveManager] External local file change detected for '{gameId}'. Triggering reload...");
                
                SaveSystem.TriggerOnSaveSynced(gameId);

#if FIREBASE_AUTH_ENABLED && FIREBASE_FIRESTORE_ENABLED
                if (FirebaseAuthService.Instance != null && FirebaseAuthService.Instance.IsLoggedIn)
                {
                    string userId = FirebaseAuthService.Instance.UserId;
                    if (!string.IsNullOrEmpty(userId))
                    {
                        Debug.Log($"[FirebaseSaveManager] Syncing external manual change of '{gameId}' to Cloud (uploading all data in batch)...");
                        UploadAllDataToCloud(userId);
                    }
                }
#endif
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FirebaseSaveManager] Failed to reload external change of '{gameId}': {ex.Message}");
            }
        }
    }
}
