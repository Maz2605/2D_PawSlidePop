using System;
using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Core.Boostrap;
using _PawSlidePopGame._Scripts.Core.System.GameFlow;
using MazFramework.UI;
using _PawSlidePopGame.Startup;
using MazFramework.Device;
using MazFramework.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using BaseBootstrap = MazFramework.Bootstrap.AppBootstrap;
namespace _PawSlidePopGame._Scripts.Core.System.Boostrap
{
    public sealed class AppBootstrap : MonoBehaviour
    {
        [SerializeField] private string nameInitScene="LoadingScene";
        [SerializeField] private string nameMainScene="GameplayScene";
        [SerializeField] private GameObject uiManagerPrefab;
        [SerializeField] private GameObject audioPrefab;
        [SerializeField] private GameObject hapticsPrefab;
        [SerializeField] private GameObject settingsPrefab;
        [SerializeField] private List<MonoBehaviour> coreServices;
        private static AppBootstrap _owner;
        private bool _started;
        private void Awake()
        {
            if(_owner!=null && _owner!=this) { Destroy(gameObject); return; }
            _owner=this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded+=HandleSceneLoaded;
        }
        private void Start()
        {
            if(SceneManager.GetActiveScene().name==nameInitScene) RunInitFlow(false);
            else if(SceneManager.GetActiveScene().name==nameMainScene) RunInitFlow(true);
        }
        public void RunInitFlow(bool isEditorAutoInject)
        {
            if(_started || (_owner!=null && _owner!=this)) return;
            _started=true;
            LegacySettingsMigration.Run();
            try
            {
                var audio=Ensure<MazFramework.Audio.AudioManager>(audioPrefab);
                var haptics=Ensure<MazFramework.Haptics.HapticManager>(hapticsPrefab);
                var settings=Ensure<MazFramework.Settings.SettingsManager>(settingsPrefab);
                UIManager ui=UIManager.ExistingInstance;
                if(ui==null) ui=FindFirstObjectByType<UIManager>(FindObjectsInactive.Exclude);
                if(ui==null) ui=Instantiate(uiManagerPrefab).GetComponent<UIManager>();
                if(ui==null) throw new InvalidOperationException("[AppBootstrap] UI prefab needs UIManager.");
                var device=GetComponent<DeviceSettingService>();
                if(device==null) device=gameObject.AddComponent<DeviceSettingService>();
                BaseBootstrap bootstrap=gameObject.AddComponent<BaseBootstrap>();
                bootstrap.Configure(device,audio,haptics,settings,
                    ui);
                if(!bootstrap.InitializeSystems()) return;
                ui.BindSceneCamera(Camera.main);
                if (GetComponent<_PawSlidePopGame.UI.Navigation.GameBackNavigation>() == null)
                    gameObject.AddComponent<_PawSlidePopGame.UI.Navigation.GameBackNavigation>();
                if (!(audio.Config is _PawSlidePopGame._Scripts.Data.Config.PawSlidePopAudioConfig))
                    Debug.LogError("[AppBootstrap] PawAudioManager needs PawSlidePopAudioConfig. Game audio will be skipped.", audio);
                SceneLoaderManager loader=SceneLoaderManager.Instance;
                loader.LoadingOverlay=ui;
                // Async SDKs keep their own readiness and must not block offline play.
                foreach(MonoBehaviour component in coreServices)
                    if(component is IAppService service)
                        try { service.Init(); } catch(Exception e) { Debug.LogException(e,component); }
                GameAppFlowManager.Instance.Initialize();
                if(!isEditorAutoInject) loader.LoadScene(nameMainScene);
            }
            catch(Exception e) { Debug.LogException(e,this); }
        }
        private static T Ensure<T>(GameObject prefab) where T:MonoBehaviour
        {
            T existing=FindFirstObjectByType<T>(FindObjectsInactive.Include);
            if(existing!=null) return existing;
            if(prefab==null) throw new InvalidOperationException("Missing service prefab for "+typeof(T).Name);
            T service=Instantiate(prefab).GetComponent<T>();
            if(service==null) throw new InvalidOperationException("Missing service component "+typeof(T).Name);
            return service;
        }
        private void HandleSceneLoaded(Scene scene,LoadSceneMode mode)
        {
            if (_owner!=this || scene!=SceneManager.GetActiveScene()) return;
            UIManager ui=UIManager.ExistingInstance;
            if(ui!=null && ui.IsInitialized) ui.BindSceneCamera(Camera.main);
        }
        private void OnDestroy()
        {
            SceneManager.sceneLoaded-=HandleSceneLoaded;
            if(_owner==this) _owner=null;
        }
    }
}
