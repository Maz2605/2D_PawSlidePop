
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace MazFramework.UI
{
    public class ToastNotification : BaseOverlay
    {
        [Header("--- UI References ---")]
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private RectTransform contentPanel; 
        
        [Header("--- Animation Config ---")]
        [SerializeField] private float defaultStayDuration = 2f; 
        [SerializeField] private float slideOffset = 100f; 
        [SerializeField] private Ease showEase = Ease.OutBack;
        [SerializeField] private Ease hideEase = Ease.InBack;  

        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float animDuration=0.25f;
        public float DefaultStayDuration => defaultStayDuration;
        protected override bool BlocksRaycasts => false;
        private Vector2 _originPos; 

        private void Awake()
        {

            if (contentPanel != null)
            {
                _originPos = contentPanel.anchoredPosition;
            }
        }



        public void SetMessage(string message)
        {
            if (messageText != null) messageText.text = message;
        }

        protected override void OnBeforeShow()
        {
            canvasGroup.alpha = 0f;
            contentPanel.anchoredPosition = _originPos + new Vector2(0f, slideOffset);
        }

        protected override Tween CreateShowTween() => DOTween.Sequence()
            .Append(contentPanel.DOAnchorPos(_originPos, animDuration).SetEase(showEase))
            .Join(canvasGroup.DOFade(1f, animDuration));

        protected override Tween CreateHideTween() => DOTween.Sequence()
            .Append(contentPanel.DOAnchorPos(_originPos + new Vector2(0f, slideOffset), animDuration).SetEase(hideEase))
            .Join(canvasGroup.DOFade(0f, animDuration));
    }
}
