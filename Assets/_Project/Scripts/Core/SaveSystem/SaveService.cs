using UnityEngine;
using System.IO;
using System;

namespace Sol
{
    public class SaveService : ISaveService
    {
        private string SaveDirectory => SaveLocation.Directory;
        
        // void Awake()
        // {
        //     ServiceLocator.RegisterService<ISaveService>(this);
        // }

        private string GetFilePath(string slotId)
        {
            return Path.Combine(SaveDirectory, $"save_{slotId}.json");
        }

        public bool SaveExists(string slotId)
        {
            // The backup counts too: a crash between moving the live file aside and promoting the new one leaves only the backup.
            string path = GetFilePath(slotId);
            return File.Exists(path) || File.Exists(AtomicFile.BackupPath(path));
        }

        public PlayerSaveData Load(string slotId)
        {
            string path = GetFilePath(slotId);

            try
            {
                // The live file, or its last good copy from AtomicFile if the live file is damaged.
                byte[] bytes = AtomicFile.ReadWithFallback(path, IsReadableSave);
                if (bytes == null)
                {
                    Debug.LogError($"[{nameof(SaveService)}]: No readable save at {path}");
                    return null;
                }
                PlayerSaveData data = JsonUtility.FromJson<PlayerSaveData>(System.Text.Encoding.UTF8.GetString(bytes));
                Debug.Log($"[{nameof(SaveService)}]: Loaded save file at {path}");
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError($"[{nameof(SaveService)}]: Can't load save file at {path}: {e.Message}");
                return null;
            }
        }

        // Written through AtomicFile, so a crash mid-write leaves the previous copy in place. Returns false when the new copy
        // did not reach disk, so the caller can stop the save.
        public bool Save(PlayerSaveData data, string slotId)
        {
            SaveLocation.EnsureExists();
            string path = GetFilePath(slotId);
        
            try
            {
                string json = JsonUtility.ToJson(data, prettyPrint: true);
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
                bool written = AtomicFile.Write(path, bytes, IsReadableSave);
                if (written) Debug.Log($"[{nameof(SaveService)}] Saved to {path}");
                else Debug.LogError($"[{nameof(SaveService)}] Save to {path} failed verification. The previous save was kept.");
                return written;
            }
            catch (Exception e)
            {
                Debug.LogError($"[{nameof(SaveService)}] Failed to save: {e.Message}");
                return false;
            }
        }

        // A file is readable if its bytes parse as a player save. Used to verify a write before it goes live.
        private static bool IsReadableSave(byte[] bytes)
        {
            try
            {
                JsonUtility.FromJson<PlayerSaveData>(System.Text.Encoding.UTF8.GetString(bytes));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>Copies the current save file to a sibling .backup.json. Returns the backup path, or null if no file exists.</summary>
        public string Backup(string slotId)
        {
            string path = GetFilePath(slotId);
            if (!File.Exists(path)) return null;

            string backupPath = Path.Combine(SaveDirectory, $"save_{slotId}.backup.json");
            try
            {
                File.Copy(path, backupPath, overwrite: true);
                Debug.Log($"[{nameof(SaveService)}] Backed up save to {backupPath}");
                return backupPath;
            }
            catch (Exception e)
            {
                Debug.LogError($"[{nameof(SaveService)}] Backup failed: {e.Message}");
                return null;
            }
        }

        // Removes every file the slot can have: the live copy, its backup and any leftover temp file. Without the backup,
        // SaveExists and Load would find the old copy again, so a deleted slot could quietly come back.
        // Removes every file the slot can have, and its world cell folder. A slot with cells left behind would load old world
        // state into a new game, so the cells go with the player file in one call.
        public void Delete(string slotId)
        {
            string path = GetFilePath(slotId);
            foreach (string file in new[] { path, AtomicFile.BackupPath(path), AtomicFile.TempPath(path) })
            {
                if (!File.Exists(file)) continue;
                File.Delete(file);
                Debug.Log($"[{nameof(SaveService)}] Deleted {file}");
            }

            string cells = SaveLocation.CellsFolder(slotId);
            if (Directory.Exists(cells))
            {
                Directory.Delete(cells, recursive: true);
                Debug.Log($"[{nameof(SaveService)}] Deleted world cells at {cells}");
            }
        }

        /// <summary>
        /// The slot whose save was written most recently, across every slot (manual, quicksave, autosave, repair).
        /// Returns null if no save exists at all. Startup and Load both use this, so the newest save is always the one loaded.
        /// </summary>
        public string NewestSlot()
        {
            string[] slots =
            {
                SaveCoordinator.ManualSlot,
                SaveCoordinator.QuickSlot,
                SaveCoordinator.AutoSlot,
                SaveCoordinator.RepairSlot,
            };

            string newest = null;
            DateTime newestTime = DateTime.MinValue;
            foreach (string slot in slots)
            {
                string path = GetFilePath(slot);
                if (!File.Exists(path)) continue;

                DateTime written = File.GetLastWriteTimeUtc(path);
                if (written > newestTime)
                {
                    newestTime = written;
                    newest = slot;
                }
            }
            return newest;
        }
    }
}
