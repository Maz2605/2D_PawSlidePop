using UnityEngine;
namespace MazFramework.UI
{
    /// <summary>A local tab/subview; its parent owns navigation, not the top-level screen stack.</summary>
    public abstract class BaseSubScreen : MonoBehaviour
    {
        protected bool isInitialized;
        public virtual void Init() { isInitialized=true; }
        public virtual void Show() { gameObject.SetActive(true); }
        public virtual void Hide() { gameObject.SetActive(false); }
    }
}
