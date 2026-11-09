using System;
using System.Collections.Generic;
using UnityEngine;

namespace MazFramework.Foundation.Events
{
    /// <summary>Main-thread multicast events. Each populated ID has one callback signature.</summary>
    public static class EventManager<T> where T : struct, Enum
    {
        private static readonly Dictionary<T, Delegate> Events = new Dictionary<T, Delegate>();
        static EventManager() => EventManagerRuntimeState.Register(ClearAll);

        public static void AddListener(T eventID, Action callback) => Add(eventID, callback);
        public static void AddListener<TParam>(T eventID, Action<TParam> callback) => Add(eventID, callback);
        public static void RemoveListener(T eventID, Action callback) => Remove(eventID, callback);
        public static void RemoveListener<TParam>(T eventID, Action<TParam> callback) => Remove(eventID, callback);

        public static void Post(T eventID)
        {
            if (TryGet(eventID, typeof(Action), out Delegate callback)) ((Action)callback).Invoke();
        }

        public static void Post<TParam>(T eventID, TParam param)
        {
            if (TryGet(eventID, typeof(Action<TParam>), out Delegate callback))
                ((Action<TParam>)callback).Invoke(param);
        }

        public static void ClearAll() => Events.Clear();

        private static void Add(T eventID, Delegate callback)
        {
            if (callback == null) return;
            Events.TryGetValue(eventID, out Delegate current);
            if (current != null && !Matches(eventID, current, callback.GetType())) return;
            Events[eventID] = Delegate.Combine(current, callback);
        }

        private static void Remove(T eventID, Delegate callback)
        {
            if (callback == null || !TryGet(eventID, callback.GetType(), out Delegate current)) return;
            Delegate remaining = Delegate.Remove(current, callback);
            if (remaining == null) Events.Remove(eventID);
            else Events[eventID] = remaining;
        }

        private static bool TryGet(T eventID, Type signature, out Delegate callback) =>
            Events.TryGetValue(eventID, out callback) && Matches(eventID, callback, signature);

        private static bool Matches(T eventID, Delegate current, Type received)
        {
            Type expected = current.GetType();
            if (expected == received) return true;
            Debug.LogError($"[EventManager] {typeof(T).FullName}.{eventID}: expected {expected}, received {received}.");
            return false;
        }
    }

    // Kept in this file so EventManager can be used independently of Singleton.
    // Unity's runtime initialization entry point lives outside the generic type.
    internal static class EventManagerRuntimeState
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
