using MazFramework.Audio;
using MazFramework.Haptics;
using PawAudioConfig = _PawSlidePopGame._Scripts.Data.Config.PawSlidePopAudioConfig;
using System;
using System.Collections.Generic;
using MazFramework.Foundation.Singleton;
using _PawSlidePopGame._Scripts.Data.Events;
using _PawSlidePopGame._Scripts.Data.Events.Payloads;
using _PawSlidePopGame._Scripts.Feature.Match3.Boosters;
using _PawSlidePopGame._Scripts.Feature.Match3.Flow;
using _PawSlidePopGame._Scripts.Feature.Match3.Presenter;
using _PawSlidePopGame._Scripts.Gameplay.Meta.MapManager;
using MazFramework.UI;
using _PawSlidePopGame._Scripts.UI.Popups;
using _PawSlidePopGame._Scripts.UI.Screens;
using _PawSlidePopGame._Scripts.UI.Screens.Gameplay;
using MazFramework.Foundation.Events;
using _PawSlidePopGame._Scripts.Gameplay.Meta.EconomyManager;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _PawSlidePopGame._Scripts.Core.System.GameFlow
{
    [DisallowMultipleComponent]
    public sealed class GameAppFlowManager : Singleton<GameAppFlowManager>
    {
        [SerializeField] private string gameplaySceneName = "GameplayScene";
        [SerializeField] private string gameplaySessionRootName = "===Gameplay===";
        [SerializeField] private string tempLevelId = "Level_001";

        private bool _isInitialized;
        private GameFlowManager _gameFlowManager;
        private Match3GameManager _gameManager;
        private Match3LevelManager _levelManager;
        private Match3BoardPresenter _boardPresenter;
        private Transform _gameplaySessionRoot;
        private bool _hasShownStartScreen;
        // Keep IDs unique even if this owner is recreated while the persistent UI remains alive.
        private static long _nextSessionId;
        private bool _changingSession;
        private bool _resultPublished;
        private bool _exitingSession;
        private string _resultLevelId;
        private bool _resultCommitted;
        private bool _committingResult;
        public long CurrentSessionId { get; private set; }
        public event Action<long> SessionChanged;
        public event Action<long, bool, GameplayHudSnapshot> ResultReady;
        private bool TryCommitWin(long sessionId, GameplayHudSnapshot snapshot)
        {
            if (sessionId <= 0 || sessionId != CurrentSessionId || snapshot == null || _committingResult) return false;
            if (_resultCommitted) return true;
            _committingResult = true;
            try
            {
                if (!LevelProgressRepository.Instance.TryRecordWinAndAdvance(
                    _resultLevelId, snapshot.reachedStars, snapshot.currentScore)) return false;
                if (sessionId != CurrentSessionId) return false;
                _resultCommitted = true;
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                return false;
            }
            finally { _committingResult = false; }
        }

        private bool PublishFinalResult(long sessionId, bool won, GameplayHudSnapshot snapshot)
        {
            if (sessionId != CurrentSessionId || sessionId == 0 || snapshot == null) return false;
            bool persisted = !won || TryCommitWin(sessionId, snapshot);
            if (!persisted)
            {
                Debug.LogError("[GameAppFlowManager] Cannot save result for session " + sessionId + " / " + CurrentLevelId, this);
                UIManager.Instance?.ShowToast("Could not save the result. Please try again.");
            }
            PresentFinalResult(sessionId, won, snapshot);
            return persisted;
        }

        private void PresentFinalResult(long sessionId, bool won, GameplayHudSnapshot snapshot)
        {
            if (!_resultPublished && !_exitingSession && sessionId == CurrentSessionId && snapshot != null)
            {
                _resultPublished = true;
                ResultReady?.Invoke(sessionId, won, snapshot.Clone());
            }
        }

        // Result buttons remain available after a failed write; each navigation attempt retries safely.
        public bool TryCommitCurrentResult()
        {
            if (CurrentSessionId == 0 || _gameFlowManager == null || !_gameFlowManager.IsResultFinalized) return true;
            return PublishFinalResult(CurrentSessionId, _gameFlowManager.ResultWon, _gameFlowManager.LastSnapshot);
        }
        private void HandleResultFinalized(long sessionId, bool won, GameplayHudSnapshot snapshot) =>
            PublishFinalResult(sessionId, won, snapshot);

        public bool TryExitGameplaySession()
        {
            if (_changingSession) return false;
            _changingSession = true;
            _exitingSession = true;
            try
            {
                var sessionId = CurrentSessionId;
                if (sessionId != 0 && _gameFlowManager != null &&
                    _gameFlowManager.CurrentInGameSubState == InGameSubState.Victory)
                {
                    if (!_gameFlowManager.IsResultFinalized &&
                        (_boardPresenter == null || !_boardPresenter.TryCompleteVictoryImmediately(sessionId)))
                    {
                        Debug.LogError("[GameAppFlowManager] Victory could not be finalized; navigation canceled.", this);
                        return false;
                    }
                    if (!PublishFinalResult(sessionId, true, _gameFlowManager.LastSnapshot)) return false;
                }
                if (_gameFlowManager != null) _gameFlowManager.ResultFinalized -= HandleResultFinalized;
                _resultLevelId = null;
                _resultCommitted = false;
                CurrentSessionId = 0;
                SessionChanged?.Invoke(0);
                ExitGameplaySessionCore();
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                UIManager.Instance?.ShowToast("Could not finish the result. Please try again.");
                return false;
            }
            finally
            {
                _exitingSession = false;
                _changingSession = false;
                // A failed early-exit write must still expose result buttons for a safe retry.
                if (CurrentSessionId != 0 && _gameFlowManager != null && _gameFlowManager.IsResultFinalized)
                    PresentFinalResult(CurrentSessionId, _gameFlowManager.ResultWon, _gameFlowManager.LastSnapshot);
            }
        }

        public GameAppState CurrentAppState { get; private set; } = GameAppState.None;
        public string CurrentLevelId { get; private set; }

        protected override void Awake()
        {
            base.Awake();
        }

        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            EventManager<LogicGameEvent>.AddListener<InGameSubStateChangedPayload>(
                LogicGameEvent.InGameSubStateChanged,
                HandleInGameSubStateChanged);

            HeartManager.Instance.InitializeHeartSystem();

            SetAppState(GameAppState.Bootstrapping);
            TryResolveGameplayFlowManager();
            // Direct Editor entry initializes after sceneLoaded; handle the current scene too.
            HandleSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        public void EnterMainMenu()
        {
            if (!EnsureUiManager())
            {
                return;
            }

            if (!TryExitGameplaySession()) return;
            UIManager.Instance.ClearAllPopups();
            UIManager.Instance.ShowScreen<GameMenuScreen>();
            SetAppState(GameAppState.MainMenu);
        }

        public bool StartGameplaySession(string levelId = null, IReadOnlyList<BoosterDefinitionSO> preLevelBoosters = null)
        {
            if (_changingSession || CurrentAppState == GameAppState.EnteringGameplay)
            {
                return false;
            }

            if (!EnsureUiManager())
            {
                return false;
            }

            if (!HeartManager.Instance.CanSpendHeart())
            {
                UIManager.Instance?.ShowToast("Not enough hearts!");
                if (CurrentAppState == GameAppState.Gameplay || CurrentAppState == GameAppState.EnteringGameplay)
                {
                    EnterMainMenu();
                }
                UIManager.Instance?.ShowPopup<RefillHeartPopup>();
                return false;
            }

            if (!TryResolveGameplayFlowManager())
            {
                Debug.LogError("[GameAppFlowManager] Missing GameFlowManager in active scene.", this);
                return false;
            }

            if (!TryResolveGameplaySessionRoot())
            {
                Debug.LogError("[GameAppFlowManager] Missing gameplay session root in active scene.", this);
                return false;
            }

            string requestedLevel = string.IsNullOrWhiteSpace(levelId) ? tempLevelId : levelId;
            if (!TryExitGameplaySession()) return false;
            CurrentLevelId = requestedLevel;
            if (!TryResolveLevelManager())
            {
                Debug.LogError("[GameAppFlowManager] Missing Match3LevelManager in active scene.", this);
                return false;
            }

            _levelManager.SetRequestedLevelId(CurrentLevelId);
            if (!_levelManager.TryLoadCurrentLevel(out var levelData))
            {
                Debug.LogError($"[GameAppFlowManager] Failed to load level '{CurrentLevelId}'.", this);
                return false;
            }

            _gameManager?.SetLevelData(levelData);
            _gameManager?.SetPreLevelBoosters(preLevelBoosters);

            HeartManager.Instance.SetMatchStarted();

            CurrentSessionId = ++_nextSessionId;
            _resultPublished = false;
            _resultLevelId = CurrentLevelId;
            _resultCommitted = false;
            _gameFlowManager.BindSession(CurrentSessionId);
            _gameFlowManager.ResultFinalized -= HandleResultFinalized;
            _gameFlowManager.ResultFinalized += HandleResultFinalized;
            SessionChanged?.Invoke(CurrentSessionId);
            SetAppState(GameAppState.EnteringGameplay);
            SetGameplaySessionActive(true);
            UIManager.Instance.ClearAllPopups();
            UIManager.Instance.ShowScreen<GameplayScreen>();
            _gameFlowManager.EnterGameplay();

            if (_gameFlowManager.CurrentInGameSubState == InGameSubState.PlayerTurn)
            {
                SetAppState(GameAppState.Gameplay);
            }

            return true;
        }

        public bool RequestStartLevel(string levelId = null)
        {
            if (!EnsureUiManager())
            {
                return false;
            }

            if (!TryResolveLevelManager())
            {
                Debug.LogError("[GameAppFlowManager] Missing Match3LevelManager in active scene.", this);
                return false;
            }

            string targetLevelId = string.IsNullOrWhiteSpace(levelId) ? tempLevelId : levelId;
            LevelProgressRepository.Instance.EnsureInitializedProgress(tempLevelId);

            if (!LevelProgressRepository.Instance.IsLevelUnlocked(targetLevelId))
            {
                UIManager.Instance.ShowPopup<LevelLockedPopup>(popup => popup.Setup(targetLevelId));
                return false;
            }

            _levelManager.SetRequestedLevelId(targetLevelId);
            if (!_levelManager.TryLoadCurrentLevel(out var levelData))
            {
                Debug.LogError($"[GameAppFlowManager] Failed to load level '{targetLevelId}' for introduction.", this);
                return false;
            }

            UIManager.Instance.ShowPopup<LevelIntroductionPopup>(popup =>
            {
                popup.Setup(targetLevelId, levelData, selectedPreLevelBoosters =>
                {
                    StartGameplaySession(targetLevelId, selectedPreLevelBoosters);
                });
            });

            return true;
        }

        public void ExitGameplaySession() => TryExitGameplaySession();

        private void ExitGameplaySessionCore()
        {
            TryResolveGameplayFlowManager();
            TryResolveGameplaySessionRoot();

            if (_gameFlowManager != null)
            {
                _gameFlowManager.ExitGameplay();
            }

            if (HeartManager.Instance.IsMatchActive)
            {
                HeartManager.Instance.SetMatchFinished(false);
            }

            CloseGameplayPopups();
            _boardPresenter?.ResetPresentation();
            _gameManager?.ResetGame();
            _levelManager?.ClearLoadedLevel();
            SetGameplaySessionActive(false);
        }

        public void RestartGameplay()
        {
            StartGameplaySession(CurrentLevelId);
        }

        public void StartNextLevel()
        {
            if (!TryCommitCurrentResult()) return;
            string nextLevelId = GetNextLevelId(CurrentLevelId);

            if (TryResolveLevelManager())
            {
                if (_levelManager.TryLoadLevel(nextLevelId, out _))
                {
                    RequestStartLevel(nextLevelId);
                    return;
                }
            }

            Debug.LogWarning($"[GameAppFlowManager] Next level '{nextLevelId}' could not be loaded or level manager is missing. Returning to main menu.", this);
            EnterMainMenu();
        }

        private string GetNextLevelId(string currentId)
        {
            if (string.IsNullOrWhiteSpace(currentId))
            {
                return "Level_001";
            }

            int idx = currentId.LastIndexOf('_');
            if (idx >= 0 && idx < currentId.Length - 1)
            {
                string prefix = currentId.Substring(0, idx + 1);
                string numStr = currentId.Substring(idx + 1);
                if (int.TryParse(numStr, out int num))
                {
                    return $"{prefix}{(num + 1):D3}";
                }
            }

            return "Level_001";
        }

        protected override void OnDestroy()
        {
            if (_gameFlowManager != null) _gameFlowManager.ResultFinalized -= HandleResultFinalized;
            CurrentSessionId = 0;
            _resultLevelId = null;
            _resultCommitted = false;
            if (_isInitialized)
            {
                SceneManager.sceneLoaded -= HandleSceneLoaded;
                EventManager<LogicGameEvent>.RemoveListener<InGameSubStateChangedPayload>(
                    LogicGameEvent.InGameSubStateChanged,
                    HandleInGameSubStateChanged);
            }

            base.OnDestroy();
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode loadMode)
        {
            if (!string.Equals(scene.name, gameplaySceneName, StringComparison.Ordinal))
            {
                return;
            }

            TryResolveGameplayFlowManager();
            TryResolveGameplaySessionRoot();

            if (!_hasShownStartScreen)
            {
                _hasShownStartScreen = true;
                ShowStartScreen();
            }
            else
            {
                EnterMainMenu();
            }
        }

        public void ShowStartScreen()
        {
            if (!EnsureUiManager())
            {
                return;
            }

            if (!TryExitGameplaySession()) return;
            UIManager.Instance.ClearAllPopups();
            UIManager.Instance.ShowScreen<StartScreen>();
            SetAppState(GameAppState.MainMenu);
        }

        private void HandleInGameSubStateChanged(InGameSubStateChangedPayload payload)
        {
            if (CurrentAppState == GameAppState.EnteringGameplay &&
                payload.Current == InGameSubState.PlayerTurn)
            {
                SetAppState(GameAppState.Gameplay);
            }
        }

        private bool TryResolveGameplayFlowManager()
        {
            _gameFlowManager = FindFirstObjectByType<GameFlowManager>(FindObjectsInactive.Include);
            _gameManager = Match3GameManager.Instance != null 
                ? Match3GameManager.Instance 
                : (_gameFlowManager != null ? _gameFlowManager.GetComponent<Match3GameManager>() : null);
            _levelManager = Match3LevelManager.Instance;
            _boardPresenter = Match3BoardPresenter.Instance;
            return _gameFlowManager != null;
        }

        private bool TryResolveLevelManager()
        {
            if (_levelManager != null)
            {
                return true;
            }

            _levelManager = Match3LevelManager.Instance;
            return _levelManager != null;
        }

        private bool TryResolveGameplaySessionRoot()
        {
            if (_gameplaySessionRoot != null)
            {
                return true;
            }

            GameObject rootObject = GameObject.Find(gameplaySessionRootName);
            if (rootObject != null)
            {
                _gameplaySessionRoot = rootObject.transform;
                return true;
            }

            if (_gameFlowManager != null)
            {
                _gameplaySessionRoot = _gameFlowManager.transform.root;
            }

            return _gameplaySessionRoot != null;
        }

        private bool EnsureUiManager()
        {
            if (UIManager.Instance != null)
            {
                return true;
            }

            Debug.LogError("[GameAppFlowManager] Missing UIManager instance.", this);
            return false;
        }

        private void SetGameplaySessionActive(bool isActive)
        {
            if (_gameplaySessionRoot == null)
            {
                return;
            }

            if (_gameplaySessionRoot.gameObject.activeSelf == isActive)
            {
                return;
            }

            _gameplaySessionRoot.gameObject.SetActive(isActive);
        }

        private void CloseGameplayPopups()
        {
            if (UIManager.Instance == null)
            {
                return;
            }

            UIManager.Instance.ClosePopup<PausePopup>();
            UIManager.Instance.ClosePopup<WinPopup>();
            UIManager.Instance.ClosePopup<LosePopup>();
            UIManager.Instance.ClosePopup<LevelIntroductionPopup>();
            UIManager.Instance.ClosePopup<SettingsPopup>();
        }

        private void SetAppState(GameAppState nextState)
        {
            if (CurrentAppState == nextState)
            {
                return;
            }

            GameAppState previousState = CurrentAppState;
            CurrentAppState = nextState;
            var audio = AudioManager.ExistingInstance;
            if (audio?.Config is PawAudioConfig config)
            {
                switch (nextState)
                {
                    case GameAppState.Bootstrapping:
                    case GameAppState.MainMenu:
                        audio.PlayMusic(config.Music.mainMenuBackground);
                        break;
                    case GameAppState.EnteringGameplay:
                        audio.PlayMusic(config.Music.gameplayBackground);
                        break;
                    case GameAppState.Gameplay when previousState == GameAppState.EnteringGameplay:
                        audio.PlaySfx(config.Match3Sfx.levelStart);
                        break;
                }
            }
            EventManager<LogicGameEvent>.Post(
                LogicGameEvent.GameAppStateChanged,
                new GameAppStateChangedPayload(previousState, CurrentAppState));
        }
    }
}
