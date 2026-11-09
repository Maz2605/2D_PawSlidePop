using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class EditorScreen : MonoBehaviour
    {
        [SerializeField] protected CanvasGroup canvasGroup;

        public bool IsVisible { get; private set; }

        protected virtual void Awake()
        {
            EnsureCanvasGroup();
        }

        public virtual void Show()
        {
            EnsureCanvasGroup();
            gameObject.SetActive(true);
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            IsVisible = true;
            OnShown();
        }

        public virtual void Hide()
        {
            EnsureCanvasGroup();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            IsVisible = false;
            OnHidden();
            gameObject.SetActive(false);
        }

        protected virtual void OnShown()
        {
        }

        protected virtual void OnHidden()
        {
        }

        private void EnsureCanvasGroup()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }
        }
    }
}
