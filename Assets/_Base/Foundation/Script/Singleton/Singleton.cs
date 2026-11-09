using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MazFramework.Foundation.Singleton
{
    /// <summary>Main-thread lookup; configured services opt out of creation with SingletonPrefab.</summary>
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static bool _applicationIsQuitting;
        private static readonly bool RequiresPrefab = typeof(T).IsDefined(typeof(SingletonPrefabAttribute), true);
        static Singleton() => SingletonRuntimeState.Register(ResetForPlaySession);

        public static bool DontDestroyOnLoadEnabled { get; set; } = true;
        public static T ExistingInstance => _instance != null ? _instance : null;

        public static T Instance
        {
            get
            {
                if (_applicationIsQuitting) return null;
                if (_instance != null) return _instance;
                T[] instances = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                if (instances.Length > 1)
                {
                    Debug.LogError($"[Singleton] Multiple {typeof(T).Name} instances without a registered owner. Assign one owner before lookup.");
                    return null;
                }
                if (instances.Length == 1)
                {
                    _instance = instances[0];
                    return _instance;
                }
                if (!Application.isPlaying) return null;
                if (RequiresPrefab)
                {
                    Debug.LogError($"[Singleton] {typeof(T).Name} requires a configured scene/prefab instance. Initialize startup first.");
                    return null;
                }
                var singleton = new GameObject("[Singleton] " + typeof(T).Name);
                // Awake registers the owner and applies persistence, including derived Awake overrides.
                T component = singleton.AddComponent<T>();
                return _instance != null ? _instance : component;
            }
        }

        protected virtual void Awake()
        {
            if (_applicationIsQuitting) return;
            // Lookup can register an inactive scene instance before its first Awake.
            if (_instance == null || _instance == this)
            {
                _instance = this as T;
                if (DontDestroyOnLoadEnabled && transform.parent == null && Application.isPlaying)
                    DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                if (GetComponents<Component>().Length > 2) Destroy(this);
                else Destroy(gameObject);
            }
        }

        protected virtual void OnApplicationQuit()
        {
            if (_instance == this) _applicationIsQuitting = true;
        }

        public static void ResetQuittingFlag() => _applicationIsQuitting = false;

        private static void ResetForPlaySession()
        {
            _instance = null;
            _applicationIsQuitting = false;
            DontDestroyOnLoadEnabled = true;
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }

    // Kept in this file so Singleton has no dependency on a shared reset service.
    // Unity's runtime initialization entry point lives outside the generic type.
    internal static class SingletonRuntimeState
    {
        private static readonly HashSet<Action> Resets = new HashSet<Action>();

        internal static void Register(Action reset) => Resets.Add(reset);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlaySession()
        {
            // Registrations must survive when Domain Reload is disabled.
            foreach (Action reset in new List<Action>(Resets)) reset();
        }
    }
}
