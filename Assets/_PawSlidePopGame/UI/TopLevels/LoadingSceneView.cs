using UnityEngine;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.UI.TopLevels
{
    public sealed class LoadingSceneView : MonoBehaviour
    {
        [SerializeField] private Text loadingLabel;
        [SerializeField] private RectTransform spinner;
        [SerializeField] private float dotInterval = 0.35f;
        [SerializeField] private float spinnerSpeed = 160f;

        private static readonly string[] LoadingTexts =
        {
            "Loading",
            "Loading.",
            "Loading..",
            "Loading..."
        };

        private float _nextDotTime;
        private int _dotCount;

        private void OnEnable()
        {
            _nextDotTime = 0f;
            _dotCount = 0;
            RefreshLabel();
        }

        private void Update()
        {
            if (spinner != null)
            {
                spinner.Rotate(0f, 0f, -spinnerSpeed * Time.unscaledDeltaTime);
            }

            if (Time.unscaledTime < _nextDotTime)
            {
                return;
            }

            _nextDotTime = Time.unscaledTime + dotInterval;
            _dotCount = (_dotCount + 1) % 4;
            RefreshLabel();
        }

        private void RefreshLabel()
        {
            if (loadingLabel == null)
            {
                return;
            }

            loadingLabel.text = LoadingTexts[_dotCount];
        }
    }
}
