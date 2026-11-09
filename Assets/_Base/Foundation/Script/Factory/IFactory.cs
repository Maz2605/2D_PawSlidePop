using UnityEngine;

namespace MazFramework.Foundation.Factory
{
    public interface IFactory<TData, TVisual> where TVisual : MonoBehaviour
    {
        TVisual CreateVisual(TData data, Transform parent);
        void ReturnVisual(TVisual visual);
    }
}