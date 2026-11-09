using System;
using System.Collections;
using MazFramework.Foundation.Singleton;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace MazFramework.SceneManagement
{
    public sealed class SceneLoaderManager : Singleton<SceneLoaderManager>
    {
        [SerializeField] private float minLoadingTime=1f;
        [SerializeField] private float loadingCoverTimeout=1f;
        public ILoadingOverlay LoadingOverlay { get; set; }
        private LoadingHandle _loadingHandle;
        private ILoadingOverlay ActiveOverlay
        {
            get
            {
                if (LoadingOverlay is UnityEngine.Object owner && owner == null) return null;
                if (LoadingOverlay is Behaviour behaviour && !behaviour.isActiveAndEnabled) return null;
                return LoadingOverlay;
            }
        }
        public bool IsLoading { get; private set; }
        public void LoadScene(string sceneName,Action onSceneLoaded=null)
        {
            if(IsLoading) return;
            if(string.IsNullOrWhiteSpace(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError("[SceneLoader] Scene is not in the build: "+sceneName);
                return;
            }
            IsLoading=true;
            StartCoroutine(LoadRoutine(sceneName,onSceneLoaded));
        }
        private IEnumerator LoadRoutine(string sceneName,Action callback)
        {
            try
            {
                Time.timeScale=1f;
                bool covered=false;
                ILoadingOverlay overlay=ActiveOverlay;
                LoadingHandle acquired=overlay?.BeginLoading(()=>covered=true);
                if (!isActiveAndEnabled)
                {
                    acquired?.Dispose();
                    yield break;
                }
                _loadingHandle=acquired;
                float deadline=Time.unscaledTime+loadingCoverTimeout;
                while(overlay!=null && !covered && Time.unscaledTime<deadline) yield return null;
                float start=Time.unscaledTime;
                AsyncOperation operation=SceneManager.LoadSceneAsync(sceneName);
                if(operation==null) yield break;
                operation.allowSceneActivation=false;
                while(operation.progress<0.9f || Time.unscaledTime-start<minLoadingTime) yield return null;
                operation.allowSceneActivation=true;
                while(!operation.isDone) yield return null;
                callback?.Invoke();
            }
            finally { IsLoading=false; ReleaseLoading(); }
        }
        private void ReleaseLoading()
        {
            LoadingHandle handle = _loadingHandle;
            _loadingHandle = null;
            handle?.Dispose();
        }
        private void OnDisable() { StopAllCoroutines(); IsLoading=false; ReleaseLoading(); }
        protected override void OnDestroy() { ReleaseLoading(); base.OnDestroy(); }
    }
}
