namespace MazFramework.Bootstrap
{
    /// <summary>A scene system initialized synchronously by AppBootstrap.</summary>
    public interface IAppSystem
    {
        bool IsInitialized { get; }
        bool Initialize();
    }
}
