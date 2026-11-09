using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class EditorPopup : MonoBehaviour
    {
        [SerializeField] protected CanvasGroup canvasGroup;

        public bool IsVisible { get; private set; }

        public virtual void Show()
        {
            if (canvasGroup == null)
            {
                return;
            }

            gameObject.SetActive(true);
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            IsVisible = true;
            OnShown();
        }

        public virtual void Hide()
        {
            if (canvasGroup == null)
            {
                return;
            }

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

    }
}
