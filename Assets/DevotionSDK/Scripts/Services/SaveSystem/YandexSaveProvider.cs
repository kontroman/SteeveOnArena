using Devotion.SDK.Async;
using Devotion.SDK.Interfaces;
using UnityEngine;

namespace Devotion.SDK.Services.SaveSystem
{
    public class YandexSaveProvider : ISaveProvider
    {
        public IPromise<string> Load(string key)
        {
            if (MineArena.Platform.YandexPlatform.IsWebPlatform)
                return MineArena.Platform.YandexPlatform.Instance.Request("load", key);
            var storedData = PlayerPrefs.GetString(key, string.Empty);
            return Promise<string>.ResolveAndReturn(storedData);
        }

        public IPromise Save(string key, string data)
        {
            if (MineArena.Platform.YandexPlatform.IsWebPlatform)
            {
                var promise = new Promise();
                MineArena.Platform.YandexPlatform.Instance.Request("save", key, data)
                    .Then(_ => promise.Resolve()).Catch(promise.Reject);
                return promise;
            }
            PlayerPrefs.SetString(key, data ?? string.Empty);
            PlayerPrefs.Save();
            return Promise.ResolveAndReturn();
        }
    }
}
