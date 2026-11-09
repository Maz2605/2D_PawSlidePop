using System;
using System.Threading;

namespace MazFramework.SceneManagement
{
    /// <summary>Releases one loading request. Dispose on Unity's main thread; repeated disposal is safe.</summary>
    public sealed class LoadingHandle : IDisposable
    {
        private Action _release;

        public LoadingHandle(Action release) => _release = release;

        public void Dispose() => Interlocked.Exchange(ref _release, null)?.Invoke();
    }
}
