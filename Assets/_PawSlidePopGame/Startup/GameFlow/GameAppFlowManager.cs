using System;
using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Core.System.DesignPattern.Singleton;
using _PawSlidePopGame._Scripts.Data.Events;
using _PawSlidePopGame._Scripts.Data.Events.Payloads;
using _PawSlidePopGame._Scripts.Feature.Match3.Boosters;
using _PawSlidePopGame._Scripts.Feature.Match3.Flow;
using _PawSlidePopGame._Scripts.Feature.Match3.Presenter;
using _PawSlidePopGame._Scripts.Gameplay.Meta.MapManager;
using _PawSlidePopGame._Scripts.UI.Manager;
using _PawSlidePopGame._Scripts.UI.Popups;
using _PawSlidePopGame._Scripts.UI.Screens;
using _PawSlidePopGame._Scripts.UI.Screens.Gameplay;
using _PawSlidePopGame.Scripts.DesignPattern.ObserverPattern;
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
        }

        public void EnterMainMenu()
        {
            if (!EnsureUiManager())
            {
                return;
            }

            ExitGameplaySession();
            UIManager.Instance.ClearAllPopups();
            UIManager.Instance.ShowScreen<GameMenuScreen>();
            SetAppState(GameAppState.MainMenu);
        }

        public bool StartGameplaySession(string levelId = null, IReadOnlyList<BoosterDefinitionSO> preLevelBoosters = null)
        {
            if (CurrentAppState == GameAppState.EnteringGameplay)
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

            CurrentLevelId = string.IsNullOrWhiteSpace(levelId) ? tempLevelId : levelId;

            ExitGameplaySession();
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

        public void ExitGameplaySession()
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

            ExitGameplaySession();
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
            EventManager<LogicGameEvent>.Post(
                LogicGameEvent.GameAppStateChanged,
                new GameAppStateChangedPayload(previousState, CurrentAppState));
        }
    }
}
