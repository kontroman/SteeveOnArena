using Devotion.SDK.Async;
using Devotion.SDK.Interfaces;
using UnityEngine;

namespace Devotion.SDK.Services.SaveSystem
{
    public class YandexSaveProvider : ISaveProvider
    {
        public IPromise<string> Load(string key)
        {
            var storedData = PlayerPrefs.GetString(key, string.Empty);
            return Promise<string>.ResolveAndReturn(storedData);
        }

        public IPromise Save(string key, string data)
        {
            PlayerPrefs.SetString(key, data ?? string.Empty);
            PlayerPrefs.Save();
            return Promise.ResolveAndReturn();
        }
    }
}
