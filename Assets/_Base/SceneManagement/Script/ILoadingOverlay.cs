using System;
namespace MazFramework.SceneManagement
{
    public interface ILoadingOverlay
    {
        LoadingHandle BeginLoading(Action onCovered=null);
    }
}
