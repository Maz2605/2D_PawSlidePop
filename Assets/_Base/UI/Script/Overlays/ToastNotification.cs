using System;
using _PawSlidePopGame._Scripts.UI.Base;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.UI.TopLevels
{
    public class ToastNotification : BasePopup
    {
        [Header("--- UI References ---")]
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private RectTransform contentPanel; 
        
        [Header("--- Animation Config ---")]
        [SerializeField] private float defaultStayDuration = 2f; 
        [SerializeField] private float slideOffset = 100f; 
        [SerializeField] private Ease showEase = Ease.OutBack;
        [SerializeField] private Ease hideEase = Ease.InBack;  

        private Sequence _toastSequence;
        private Vector2 _originPos; 

        protected override void Awake()
        {
            base.Awake();
            if (contentPanel != null)
            {
                _originPos = contentPanel.anchoredPosition;
            }
        }

        protected override void PlayShowAnimation() { } 
        protected override void PlayHideAnimation(Action onComplete) { onComplete?.Invoke(); }

        /// <summary>
        /// Hàm gọi Toast chính từ UIManager
       /// </summary>
       public void ShowToast(string message, float duration = -1f)
       {
           if (messageText == null || contentPanel == null) return;

           messageText.text = message;
           float stayTime = (duration < 0) ? defaultStayDuration : duration;

           gameObject.SetActive(true);
           _toastSequence?.Kill(); 
    
           canvasGroup.alpha = 0f;

           contentPanel.anchoredPosition = _originPos + new Vector2(0, slideOffset);

           _toastSequence = DOTween.Sequence();

           _toastSequence.Append(contentPanel.DOAnchorPos(_originPos, animDuration).SetEase(showEase));
           _toastSequence.Join(canvasGroup.DOFade(1f, animDuration));
           _toastSequence.AppendInterval(stayTime);
           _toastSequence.Append(contentPanel.DOAnchorPos(_originPos + new Vector2(0, slideOffset), animDuration).SetEase(hideEase));
           _toastSequence.Join(canvasGroup.DOFade(0f, animDuration));

           _toastSequence.OnComplete(() => gameObject.SetActive(false));
           _toastSequence.SetUpdate(true).SetLink(gameObject); 
       }

        private void OnDestroy()
        {
            _toastSequence?.Kill();
        }
    }
}