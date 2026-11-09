using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace MazFramework.UI
{
    public class LoadingScreen : BaseOverlay
    {
        [Header("UI References")] 
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform centerIcon;

        [Header("Fast Wavy Text Effect")]
        [SerializeField] private RectTransform[] letterRects; 

        [Header("Animation Config")]
        [SerializeField] private float fadeDuration = 0.3f;
        [SerializeField] private float minShowTime = 1.0f; // Đảm bảo diễn đủ ít nhất 1s

        [Header("Wavy Settings")]
        [SerializeField] private float bobHeight = 30f;      
        [SerializeField] private float jumpDuration = 0.3f;  
        [SerializeField] private float staggerDelay = 0.05f; 
        [SerializeField] private float cycleDelay = 0.5f;    

        private Coroutine _wavyRoutine;
        private float _showStartTime; // Thời điểm bắt đầu hiện xong
        protected override void OnBeforeShow()
        {
            _showStartTime = Time.realtimeSinceStartup;
            StopWavyAnimation();
            if (centerIcon != null) { centerIcon.DOKill(); centerIcon.localScale = Vector3.zero; }
            foreach (var letter in letterRects)
                if (letter != null) letter.localScale = Vector3.zero;
            canvasGroup.alpha = 0f;
        }

        protected override Tween CreateShowTween() => canvasGroup.DOFade(1f, fadeDuration);

        protected override void OnShown()
        {
            _showStartTime = Time.realtimeSinceStartup;
            StartDisplayElements();
        }

        private void StartDisplayElements()
        {
            if (centerIcon != null)
            {
                centerIcon.DOKill();
                centerIcon.DOScale(1f, 0.4f).SetEase(Ease.OutBack).SetUpdate(true);
            }

            for (int i = 0; i < letterRects.Length; i++)
            {
                if (letterRects[i] != null)
                {
                    letterRects[i].DOKill();
                    letterRects[i].DOScale(1f, 0.3f)
                        .SetDelay(i * 0.05f)
                        .SetEase(Ease.OutBack)
                        .SetUpdate(true);
                }
            }

            StartWavyAnimation();
        }

        protected override Tween CreateHideTween()
        {
            float remainingTime = Mathf.Max(0f, minShowTime - (Time.realtimeSinceStartup - _showStartTime));
            return DOTween.Sequence().AppendInterval(remainingTime)
                .AppendCallback(StopWavyAnimation)
                .Append(canvasGroup.DOFade(0f, fadeDuration));
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            StopWavyAnimation();
            centerIcon?.DOKill();
        }
        #region Wavy Logic
        private void StartWavyAnimation()
        {
            if (_wavyRoutine != null) StopCoroutine(_wavyRoutine);
            _wavyRoutine = StartCoroutine(AnimateFastWaveRoutine());
        }

        private void StopWavyAnimation()
        {
            if (_wavyRoutine != null)
            {
                StopCoroutine(_wavyRoutine);
                _wavyRoutine = null;
            }
            
            foreach (var letter in letterRects)
            {
                if (letter != null)
                {
                    letter.DOKill();
                    letter.anchoredPosition = new Vector2(letter.anchoredPosition.x, 0);
                }
            }
        }

        private IEnumerator AnimateFastWaveRoutine()
        {
            while (true)
            {
                for (int i = 0; i < letterRects.Length; i++)
                {
                    if (letterRects[i] != null)
                    {
                        AnimateLetter(letterRects[i]);
                    }
                    yield return new WaitForSecondsRealtime(staggerDelay);
                }
                yield return new WaitForSecondsRealtime(cycleDelay);
            }
        }

        private void AnimateLetter(RectTransform letter)
        {
            Sequence s = DOTween.Sequence();
            s.Append(letter.DOAnchorPosY(bobHeight, jumpDuration / 2).SetEase(Ease.OutQuad));
            s.Append(letter.DOAnchorPosY(0f, jumpDuration / 2).SetEase(Ease.InQuad));
            s.SetUpdate(true);
            s.SetLink(letter.gameObject);
        }
        #endregion
    }
}