using UnityEngine;
using System.IO;
using System;

namespace Sol
{
    public class SaveService : ISaveService
    {
        private string SaveDirectory => Application.persistentDataPath;
        
        // void Awake()
        // {
        //     ServiceLocator.RegisterService<ISaveService>(this);
        // }

        private string GetFilePath(string slotId)
        {
            return Path.Combine(SaveDirectory, $"save_{slotId}.json");
        }

        public bool SaveExists(string slotId = "default")
        {
            return (File.Exists(GetFilePath(slotId)));
        }

        public PlayerSaveData Load(string slotId = "default")
        {
            string path = GetFilePath(slotId);

            if (!File.Exists(path))
            {
                Debug.Log($"[{nameof(SaveService)}]: Can't find save file at {path}");
                return new PlayerSaveData();
            }

            try
            {
                string json = File.ReadAllText(path);
                PlayerSaveData data = JsonUtility.FromJson<PlayerSaveData>(json);
                Debug.Log($"[{nameof(SaveService)}]: Loaded save file at {path}");
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError($"[{nameof(SaveService)}]: Can't load save file at {path}");
                return new PlayerSaveData();
            }
        }

        public void Save(PlayerSaveData data, string slotId = "default")
        {
            string path = GetFilePath(slotId);
        
            try
            {
                string json = JsonUtility.ToJson(data, prettyPrint: true);
                File.WriteAllText(path, json);
                Debug.Log($"[{nameof(SaveService)}] Saved to {path}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[{nameof(SaveService)}] Failed to save: {e.Message}");
            }
        }

        public void Delete(string slotId = "default")
        {
            string path = GetFilePath(slotId);
            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log($"[{nameof(SaveService)}] Deleted save at {path}");
            }
        }

        public string GetLastSaveTime(string slotId = "default")
        {
            string path = GetFilePath(slotId);
            if (!File.Exists(path)) return "Never";
        
            DateTime lastWrite = File.GetLastWriteTime(path);
            TimeSpan since = DateTime.Now - lastWrite;
        
            if (since.TotalMinutes < 1) return "Just now";
            if (since.TotalHours < 1) return $"{(int)since.TotalMinutes}m ago";
            if (since.TotalDays < 1) return $"{(int)since.TotalHours}h ago";
            return $"{(int)since.TotalDays}d ago";
        }
    }
}
