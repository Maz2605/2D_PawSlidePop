using System;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace _PawSlidePopGame.Startup
{
    public static class LegacySettingsMigration
    {
        private const string Marker = "PawSlidePop.SettingsMigration.v1";
        public static void Run()
        {
            if (PlayerPrefs.GetInt(Marker, 0) == 1) return;
            string path = Path.Combine(Application.persistentDataPath, "settings.json");
            if (!File.Exists(path)) return;
            try
            {
                if (ImportJson(File.ReadAllText(path), PlayerPrefs.HasKey,
                    PlayerPrefs.SetInt, PlayerPrefs.SetFloat, PlayerPrefs.Save))
                {
                    PlayerPrefs.SetInt(Marker, 1);
                    PlayerPrefs.Save();
                }
                else Debug.LogWarning("[SettingsMigration] Invalid legacy settings; kept original file.");
            }
            catch (Exception e) { Debug.LogWarning("[SettingsMigration] Import incomplete; will retry: " + e.Message); }
        }
        public static bool ImportJson(string json, Func<string, bool> hasKey,
            Action<string, int> setInt, Action<string, float> setFloat, Action save)
        {
            JObject data;
            try
            {
                data = JObject.Parse(json);
                if (data.Value<int?>("schemaVersion") is int version && version != 1) return false;
                foreach (string key in new[] { "musicVolume", "sfxVolume" })
                {
                    if (data[key] == null) return false;
                    float value = data.Value<float>(key);
                    if (float.IsNaN(value) || float.IsInfinity(value)) return false;
                }
                foreach (string key in new[] { "musicEnabled", "sfxEnabled", "vibrationEnabled" })
                    if (data[key]?.Type != JTokenType.Boolean) return false;
            }
            catch { return false; }
            float music = Mathf.Clamp01(data.Value<float>("musicVolume"));
            float sfx = Mathf.Clamp01(data.Value<float>("sfxVolume"));
            SetFloat("MusicVolume", music); SetFloat("SfxVolume", sfx); SetFloat("MasterVolume", 1f);
            SetBool("MusicEnabled", data.Value<bool>("musicEnabled") && music > 0.001f);
            SetBool("SfxEnabled", data.Value<bool>("sfxEnabled") && sfx > 0.001f);
            SetBool("HapticsEnabled", data.Value<bool>("vibrationEnabled"));
            save();
            return true;
            void SetFloat(string key, float value) { key = "Settings." + key; if (!hasKey(key)) setFloat(key, value); }
            void SetBool(string key, bool value) { key = "Settings." + key; if (!hasKey(key)) setInt(key, value ? 1 : 0); }
        }
    }
}
