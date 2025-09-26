using System;
using System.IO;
using Devotion.SDK.Async;
using Devotion.SDK.Interfaces;
using UnityEngine;

namespace Devotion.SDK.Services.SaveSystem
{
    public class DesktopSaveProvider : ISaveProvider
    {
        private static readonly string RootFolder = Path.Combine(Application.persistentDataPath, "Saves");
        private const string FileExtension = ".json";

        public IPromise Save(string key, string data)
        {
            var promise = new Promise();
            try
            {
                var path = GetSavePath(key);
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(path, data ?? string.Empty);
                promise.Resolve();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Failed to save data for key {key}: {ex}");
                promise.Reject(ex);
            }

            return promise;
        }

        public IPromise<string> Load(string key)
        {
            var promise = new Promise<string>();
            try
            {
                var path = GetSavePath(key);
                if (File.Exists(path))
                {
                    promise.Resolve(File.ReadAllText(path));
                }
                else
                {
                    promise.Resolve(string.Empty);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Failed to load data for key {key}: {ex}");
                promise.Reject(ex);
            }

            return promise;
        }

        public static void DeleteSave(string key)
        {
            try
            {
                var path = GetSavePath(key);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Failed to delete data for key {key}: {ex}");
            }
        }

        public static void DeleteAllSaves()
        {
            try
            {
                if (Directory.Exists(RootFolder))
                {
                    Directory.Delete(RootFolder, true);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Failed to delete save directory: {ex}");
            }
        }

        private static string GetSavePath(string key)
        {
            var fileName = $"{key}{FileExtension}";
            return Path.Combine(RootFolder, fileName);
        }
    }
}
