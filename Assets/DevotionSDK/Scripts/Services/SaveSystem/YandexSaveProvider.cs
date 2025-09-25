using Devotion.SDK.Async;
using Devotion.SDK.Interfaces;
using UnityEngine;

namespace Devotion.SDK.Services.SaveSystem
{
    public class YandexSaveProvider : ISaveProvider
    {
        public IPromise<string> Load()
        {
            var promise = new Promise<string>();
            var loadedData = PlayerPrefs.GetString("SaveData");
            promise.Resolve(loadedData);
            return promise;
        }

        public IPromise Save(string data)
        {
            PlayerPrefs.SetString("SaveData", data);
            return Promise.ResolveAndReturn();
        }
    }
}