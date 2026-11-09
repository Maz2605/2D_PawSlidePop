using System;
using System.Collections;
using _PawSlidePopGame._Scripts.Core.System.DesignPattern.Singleton;
using _PawSlidePopGame._Scripts.UI.Manager;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _PawSlidePopGame._Scripts.Core.System.SceneManagement
{
    public class SceneLoaderManager : Singleton<SceneLoaderManager>
    {
        [Header("Config")] [SerializeField] private float minLoadingTime = 1f;
        [SerializeField] private float loadingCoverTimeout = 1f;

        public void LoadScene(string sceneName, Action onSceneLoaded = null)
        {
            StartCoroutine(LoadSceneRoutine(sceneName, onSceneLoaded));
        }

        private IEnumerator LoadSceneRoutine(string sceneName, Action onSceneLoaded)
        {
            Time.timeScale = 1f;

            bool isCovered = false;
            bool hasLoadingOverlay = UIManager.Instance.ShowLoading(() => isCovered = true);

            if (hasLoadingOverlay)
            {
                float coverDeadline = Time.unscaledTime + loadingCoverTimeout;
                yield return new WaitUntil(() => isCovered || Time.unscaledTime >= coverDeadline);
            }
            else
            {
                yield return null;
            }


            yield return Resources.UnloadUnusedAssets();
            GC.Collect();

            float startTime = Time.unscaledTime;

            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            op.allowSceneActivation = false;

            while (op.progress < 0.9f)
            {
                yield return null;
            }

            float elapsedTime = Time.unscaledTime - startTime;

            if (elapsedTime < minLoadingTime)
            {
                yield return new WaitForSecondsRealtime(minLoadingTime - elapsedTime);
            }


            op.allowSceneActivation = true;

            while (!op.isDone) yield return null;

            onSceneLoaded?.Invoke();

            yield return null;

            UIManager.Instance.HideLoading();
        }
    }
}
