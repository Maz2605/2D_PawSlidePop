using System;
using Firebase;
using Firebase.Analytics;
using Firebase.Crashlytics;
using Firebase.Extensions;

namespace MazFramework.Integrations.Firebase
{
    /// <summary>SDK operations only; no game event names, saves or progression.</summary>
    public static class FirebaseRuntime
    {
        public static void Initialize(bool analyticsEnabled, bool crashlyticsEnabled, Action ready, Action<string> failed)
        {
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled) { failed?.Invoke(task.Exception?.Message ?? "Canceled"); return; }
                if (task.Result != DependencyStatus.Available) { failed?.Invoke(task.Result.ToString()); return; }
                _ = FirebaseApp.DefaultInstance;
                FirebaseAnalytics.SetAnalyticsCollectionEnabled(analyticsEnabled);
                Crashlytics.IsCrashlyticsCollectionEnabled = crashlyticsEnabled;
                ready?.Invoke();
            });
        }
        public static void LogEvent(string name, params Parameter[] parameters) => FirebaseAnalytics.LogEvent(name, parameters);
#if FIREBASE_AUTH_ENABLED
        public static global::Firebase.Auth.FirebaseAuth Auth => global::Firebase.Auth.FirebaseAuth.DefaultInstance;
#endif
#if FIREBASE_FIRESTORE_ENABLED
        public static global::Firebase.Firestore.FirebaseFirestore Store => global::Firebase.Firestore.FirebaseFirestore.DefaultInstance;
#endif
    }
}
