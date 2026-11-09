using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace MazFramework.Bootstrap
{
    [DisallowMultipleComponent]
    public sealed class AppBootstrap : MonoBehaviour
    {
        [Tooltip("Active scene components implementing IAppSystem, in initialization order.")]
        [SerializeField] private MonoBehaviour[] systems = Array.Empty<MonoBehaviour>();
        [SerializeField] private UnityEvent onReady = new UnityEvent();
        [SerializeField] private UnityEvent onFailed = new UnityEvent();

        private bool _initializing;

        public bool IsReady { get; private set; }
        public string LastError { get; private set; }
        public UnityEvent OnReady => onReady;
        public UnityEvent OnFailed => onFailed;

        public void Configure(params MonoBehaviour[] orderedSystems)
        {
            if (_initializing || IsReady) throw new InvalidOperationException("Bootstrap already started.");
            systems = orderedSystems ?? Array.Empty<MonoBehaviour>();
        }

        // All active scene systems have registered their instances in Awake by now.
        // Mixer parameters can also be applied safely during this startup phase.
        private void Start() => InitializeSystems();

        /// <summary>Run in list order. A retry keeps systems that already initialized.</summary>
        public bool InitializeSystems()
        {
            if (!Application.isPlaying || _initializing || !isActiveAndEnabled) return false;
            if (IsReady) return true;

            _initializing = true;
            LastError = null;
            try
            {
                if (systems == null || systems.Length == 0)
                    return Fail("Assign at least one scene system.");

                // Validate the complete list before initializing any system.
                MonoBehaviour[] orderedSystems = (MonoBehaviour[])systems.Clone();
                var uniqueSystems = new HashSet<MonoBehaviour>();
                for (int i = 0; i < orderedSystems.Length; i++)
                {
                    MonoBehaviour component = orderedSystems[i];
                    if (component == null) return Fail($"System [{i}] is missing.");
                    if (!(component is IAppSystem))
                        return Fail($"System [{i}] {component.name} must implement IAppSystem.");
                    if (!component.gameObject.scene.IsValid() || !component.isActiveAndEnabled)
                        return Fail($"System [{i}] {component.name} must be an active, enabled scene instance.");
                    if (!uniqueSystems.Add(component))
                        return Fail($"System [{i}] {component.name} is listed more than once.");
                }

                foreach (MonoBehaviour component in orderedSystems)
                {
                    if (component == null || !component.isActiveAndEnabled)
                        return Fail("A system was destroyed or disabled during initialization.");

                    try
                    {
                        var system = (IAppSystem)component;
                        if (!system.IsInitialized && (!system.Initialize() || !system.IsInitialized))
                            return Fail($"{component.name} did not initialize. Check its configuration and list order.");
                        if (!isActiveAndEnabled)
                            return Fail("Bootstrap was disabled during initialization.");
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception, component);
                        return Fail($"{component.name} threw during initialization: {exception.Message}");
                    }
                }

                IsReady = true;
            }
            finally
            {
                _initializing = false;
            }

            Notify(onReady);
            return true;
        }

        private bool Fail(string message)
        {
            IsReady = false;
            LastError = message;
            Debug.LogError($"[AppBootstrap] {message}", this);
            Notify(onFailed);
            return false;
        }

        private void Notify(UnityEvent notification)
        {
            try { notification?.Invoke(); }
            catch (Exception exception) { Debug.LogException(exception, this); }
        }
    }
}
