using System;

namespace MazFramework.Foundation.Singleton
{
    /// <summary>Requires a scene/prefab instance. Checked before allocating any Unity object.</summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public sealed class SingletonPrefabAttribute : Attribute { }
}
