using UnityEditor;
using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;

[InitializeOnLoad]
public static class ConfigureFacebookSettings
{
    private const string AppId = "973679888895318";
    private const string AppName = "2D Paw Slide Pop";

    static ConfigureFacebookSettings()
    {
        // 0. Ensure openssl is in the Environment PATH so Facebook SDK post-processor can find it
        SetupOpenSSLPath();

        // Run on next update to ensure AssetDatabase and other systems are fully ready
        EditorApplication.delayCall += RunConfiguration;
    }

    private static void SetupOpenSSLPath()
    {
        try
        {
            string gitUsrBin = @"C:\Program Files\Git\usr\bin";
            string currentPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            
            if (System.IO.Directory.Exists(gitUsrBin))
            {
                if (!currentPath.Contains(gitUsrBin))
                {
                    string newPath = currentPath;
                    if (!string.IsNullOrEmpty(currentPath) && !currentPath.EndsWith(";"))
                    {
                        newPath += ";";
                    }
                    newPath += gitUsrBin;
                    Environment.SetEnvironmentVariable("PATH", newPath, EnvironmentVariableTarget.Process);
                    Debug.Log($"[ConfigureFacebookSettings] Successfully appended '{gitUsrBin}' to current process PATH.");
                }
                else
                {
                    Debug.Log("[ConfigureFacebookSettings] Git usr/bin is already in process PATH.");
                }
            }
            else
            {
                Debug.LogWarning($"[ConfigureFacebookSettings] Git usr/bin directory not found at '{gitUsrBin}'. If openssl fails, you might need to install Git or OpenSSL.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[ConfigureFacebookSettings] Failed to setup OpenSSL path: {e.Message}");
        }
    }

    private static void RunConfiguration()
    {
        Debug.Log("[ConfigureFacebookSettings] Starting Facebook SDK configuration...");

        try
        {
            // 1. Find the FacebookSettings type
            Type settingsType = null;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                settingsType = assembly.GetType("Facebook.Unity.Settings.FacebookSettings");
                if (settingsType != null)
                {
                    break;
                }
            }

            if (settingsType == null)
            {
                Debug.LogError("[ConfigureFacebookSettings] FacebookSettings class not found! Make sure Facebook SDK is imported.");
                return;
            }

            // 2. Get the FacebookSettings instance. Usually it has a static property "Instance" or similar.
            PropertyInfo instanceProp = settingsType.GetProperty("Instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            object settingsInstance = null;
            if (instanceProp != null)
            {
                settingsInstance = instanceProp.GetValue(null);
            }
            else
            {
                // Fallback: search for it using Resources.Load or FindObjectOfType
                MethodInfo nullableInstanceMethod = settingsType.GetMethod("NullableInstance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (nullableInstanceMethod != null)
                {
                    settingsInstance = nullableInstanceMethod.Invoke(null, null);
                }
            }

            if (settingsInstance == null)
            {
                // If it doesn't exist, we can trigger the creation of it
                // Facebook SDK usually has a menu item or a method to select/create it.
                // Let's try calling EditSettings menu item first to force creation.
                Type editorMenuType = null;
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    editorMenuType = assembly.GetType("Facebook.Unity.Editor.EditorMenu");
                    if (editorMenuType != null)
                    {
                        break;
                    }
                }
                if (editorMenuType != null)
                {
                    MethodInfo editSettingsMethod = editorMenuType.GetMethod("EditSettings", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    if (editSettingsMethod != null)
                    {
                        Debug.Log("[ConfigureFacebookSettings] Calling EditorMenu.EditSettings to create FacebookSettings asset...");
                        editSettingsMethod.Invoke(null, null);
                        
                        // Try to get instance again
                        if (instanceProp != null)
                        {
                            settingsInstance = instanceProp.GetValue(null);
                        }
                    }
                }
            }

            if (settingsInstance == null)
            {
                Debug.LogError("[ConfigureFacebookSettings] Could not find or create FacebookSettings instance.");
                return;
            }

            Debug.Log($"[ConfigureFacebookSettings] Found FacebookSettings instance: {settingsInstance}");

            // 3. Set the AppId and AppName
            // In FacebookSettings, AppIds is typically List<string> and AppLabels is List<string>
            // Let's use reflection to set AppIds and AppLabels
            SetPropertyOrField(settingsType, settingsInstance, "AppIds", new List<string> { AppId });
            SetPropertyOrField(settingsType, settingsInstance, "AppLabels", new List<string> { AppName });

            // Mark the ScriptableObject as dirty so Unity saves it
            if (settingsInstance is UnityEngine.Object unityObj)
            {
                EditorUtility.SetDirty(unityObj);
                AssetDatabase.SaveAssets();
                Debug.Log("[ConfigureFacebookSettings] Configured FacebookSettings and saved assets.");
            }

            // 4. Generate Android Manifest
            // In Facebook.Unity.Editor.ManifestMod
            Type manifestModType = null;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                manifestModType = assembly.GetType("Facebook.Unity.Editor.ManifestMod");
                if (manifestModType != null)
                {
                    break;
                }
            }

            if (manifestModType != null)
            {
                MethodInfo generateManifestMethod = manifestModType.GetMethod("GenerateManifest", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (generateManifestMethod != null)
                {
                    Debug.Log("[ConfigureFacebookSettings] Generating Android Manifest via ManifestMod.GenerateManifest()...");
                    generateManifestMethod.Invoke(null, null);
                    Debug.Log("[ConfigureFacebookSettings] Android Manifest generated successfully!");
                }
                else
                {
                    Debug.LogError("[ConfigureFacebookSettings] GenerateManifest method not found in ManifestMod.");
                }
            }
            else
            {
                Debug.LogError("[ConfigureFacebookSettings] ManifestMod class not found.");
            }

            Debug.Log("[ConfigureFacebookSettings] Configuration completed! You can now safely delete this script or keep it.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[ConfigureFacebookSettings] Error configuring Facebook SDK: {e}");
        }
    }

    private static void SetPropertyOrField(Type type, object instance, string name, object value)
    {
        PropertyInfo prop = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        if (prop != null && prop.CanWrite)
        {
            prop.SetValue(instance, value);
            Debug.Log($"[ConfigureFacebookSettings] Set property {name} to {value}");
            return;
        }

        FieldInfo field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        if (field != null)
        {
            // If field type is string[] and value is List<string>, convert it
            if (field.FieldType == typeof(string[]) && value is List<string> listStr)
            {
                field.SetValue(instance, listStr.ToArray());
            }
            // If field type is List<string> and value is List<string>
            else if (field.FieldType == typeof(List<string>) && value is List<string>)
            {
                field.SetValue(instance, value);
            }
            else
            {
                field.SetValue(instance, value);
            }
            Debug.Log($"[ConfigureFacebookSettings] Set field {name} to value");
            return;
        }

        Debug.LogError($"[ConfigureFacebookSettings] Property or Field '{name}' not found or not writable on {type.FullName}");
    }
}
