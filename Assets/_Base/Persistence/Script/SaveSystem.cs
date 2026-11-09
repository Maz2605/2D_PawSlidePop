using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
namespace MazFramework.Persistence
{
    /// <summary>JSON storage with legacy Base64 compatibility and a last-good backup.</summary>
    public static class SaveSystem
    {
        public static event Action<string,string> OnSaveCompleted;
        public static event Action<string> OnSaveSynced;
        public static event Action<string> OnBeforeLocalSaveWrite;
        public static string GetPath(string gameId) => Path.Combine(Application.persistentDataPath, gameId + ".json");
        public static void TriggerOnSaveSynced(string gameId) => OnSaveSynced?.Invoke(gameId);
        public static void Save<T>(string gameId,T data) => TrySave(gameId, data);
        public static bool TrySave<T>(string gameId, T data)
        {
            string json;
            try
            {
                OnBeforeLocalSaveWrite?.Invoke(gameId);
                json = JsonConvert.SerializeObject(data, Formatting.Indented);
                WriteJson(GetPath(gameId), json, true);
            }
            catch (Exception e) { Debug.LogError("[SaveSystem] Save failed: " + e.Message); return false; }
            // A cloud/observer failure cannot turn a completed local write into a failed write.
            if (OnSaveCompleted != null)
                foreach (Action<string, string> callback in OnSaveCompleted.GetInvocationList())
                    try { callback(gameId, json); } catch (Exception e) { Debug.LogException(e); }
            return true;
        }
        public static T Load<T>(string gameId) where T:new() => LoadFromPath<T>(GetPath(gameId),true);
        public static void SaveRawJson(string gameId,string rawJson)
        {
            try
            {
                OnBeforeLocalSaveWrite?.Invoke(gameId);
                JValidate(rawJson);
                WriteJson(GetPath(gameId),rawJson,true);
                OnSaveSynced?.Invoke(gameId);
            }
            catch(Exception e) { Debug.LogError("[SaveSystem] Cloud write failed: "+e.Message); }
        }
        public static string LoadRawJson(string gameId) => ReadJson(GetPath(gameId),true);
        public static void SaveToPath<T>(string fullPath,T data,bool useEncryption=false)
        {
            try { WriteJson(fullPath,JsonConvert.SerializeObject(data,Formatting.Indented),useEncryption); }
            catch(Exception e) { Debug.LogError("[SaveSystem] Write failed: "+e.Message); }
        }
        public static T LoadFromPath<T>(string fullPath,bool useEncryption=false) where T:new()
        {
            foreach(string path in new[] { fullPath,fullPath+".bak" })
            {
                if(!File.Exists(path)) continue;
                try
                {
                    string raw=File.ReadAllText(path);
                    if(useEncryption) raw=Encoding.UTF8.GetString(Convert.FromBase64String(raw));
                    T value=JsonConvert.DeserializeObject<T>(raw);
                    if(value!=null) return value;
                }
                catch(Exception e) { Debug.LogWarning("[SaveSystem] Cannot read "+path+": "+e.Message); }
            }
            return new T();
        }
        private static string ReadJson(string fullPath,bool base64)
        {
            foreach(string path in new[] { fullPath,fullPath+".bak" })
            {
                if(!File.Exists(path)) continue;
                try
                {
                    string raw=File.ReadAllText(path);
                    if(base64) raw=Encoding.UTF8.GetString(Convert.FromBase64String(raw));
                    JValidate(raw); return raw;
                }
                catch(Exception e) { Debug.LogWarning("[SaveSystem] Cannot read "+path+": "+e.Message); }
            }
            return null;
        }
        private static void JValidate(string raw) { Newtonsoft.Json.Linq.JToken.Parse(raw); }
        private static void WriteJson(string fullPath,string json,bool base64)
        {
            string directory=Path.GetDirectoryName(fullPath);
            if(!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            string raw=base64?Convert.ToBase64String(Encoding.UTF8.GetBytes(json)):json;
            string temp=fullPath+".tmp";
            File.WriteAllText(temp,raw);
            if(!File.Exists(fullPath)) { File.Move(temp,fullPath); return; }
            // Never overwrite a good backup with a corrupt primary after recovery.
            bool valid=false;
            try
            {
                string previous=File.ReadAllText(fullPath);
                if(base64) previous=Encoding.UTF8.GetString(Convert.FromBase64String(previous));
                JValidate(previous); valid=true;
            }
            catch { }
            if(valid)
            {
                try { File.Replace(temp,fullPath,fullPath+".bak"); return; }
                catch(PlatformNotSupportedException) { }
                catch(IOException) { if(!File.Exists(temp)) throw; }
                File.Copy(fullPath,fullPath+".bak",true);
            }
            // On platforms without replace, retain backup until the next verified save.
            File.Delete(fullPath);
            File.Move(temp,fullPath);
        }
        public static void DeleteFile(string gameId)
        {
            try
            {
                string path=GetPath(gameId);
                foreach(string suffix in new[] { "", ".tmp", ".bak" })
                    if(File.Exists(path+suffix)) File.Delete(path+suffix);
            }
            catch(Exception e) { Debug.LogError("[SaveSystem] Delete failed: "+e.Message); }
        }
    }
}
