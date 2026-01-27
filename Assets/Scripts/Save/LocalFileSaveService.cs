using System;
using System.IO;
using UnityEngine;

namespace RoguelikeCardBattler.Save
{
    /// <summary>
    /// Implementación local en JSON (Application.persistentDataPath).
    /// No usa PlayerPrefs para evitar acoplar lógica de gameplay.
    /// </summary>
    public class LocalFileSaveService : ISaveService
    {
        private const string FileName = "save_v1.json";
        private const string TempFileName = "save_v1.tmp";

        private static string GetSavePath()
        {
            return Path.Combine(Application.persistentDataPath, FileName);
        }

        private static string GetTempPath()
        {
            return Path.Combine(Application.persistentDataPath, TempFileName);
        }

        public void SaveMeta(SaveData data)
        {
            if (data == null)
            {
                Debug.LogWarning("[Save] SaveMeta called with null data.");
                return;
            }

            data.version = SaveData.CurrentVersion;
            data.TouchTimestampUtc();

            string json = JsonUtility.ToJson(data, true);
            string path = GetSavePath();
            string tempPath = GetTempPath();

            try
            {
                // Escritura segura: escribimos a temp y luego reemplazamos.
                File.WriteAllText(tempPath, json);
                File.Copy(tempPath, path, true);
                File.Delete(tempPath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Save] Failed to write save file. {ex.Message}");
            }
        }

        public bool TryLoadMeta(out SaveData data)
        {
            data = null;
            string path = GetSavePath();
            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                string json = File.ReadAllText(path);
                SaveData loaded = JsonUtility.FromJson<SaveData>(json);
                if (loaded == null)
                {
                    Debug.LogWarning("[Save] Save file is empty or invalid JSON.");
                    return false;
                }

                if (loaded.version != SaveData.CurrentVersion)
                {
                    Debug.LogWarning($"[Save] Save version mismatch. Expected {SaveData.CurrentVersion}, got {loaded.version}.");
                    return false;
                }

                data = loaded;
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Save] Failed to load save file. {ex.Message}");
                return false;
            }
        }

        public bool HasValidSave()
        {
            return TryLoadMeta(out _);
        }

        public void DeleteSave()
        {
            string path = GetSavePath();
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Save] Failed to delete save file. {ex.Message}");
            }
        }
    }
}
