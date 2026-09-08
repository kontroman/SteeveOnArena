using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Devotion.SDK.Async;
using Devotion.SDK.Interfaces;
using Devotion.SDK.Services.SaveSystem;
using MineArena.Levels;
using UnityEngine;
using UnityEngine.Scripting;

namespace MineArena.Platform
{
    public sealed class YandexPlatform : MonoBehaviour, ILevelRewardedAdsProvider, MineArena.PlayerSystem.IReviveAdsProvider
    {
        public static bool IsWebPlatform
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }
        private static YandexPlatform _instance;
        public static YandexPlatform Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("MineArenaPlatform");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<YandexPlatform>();
                }
                return _instance;
            }
        }
        [Serializable] private class RequestData { public int id; public string op, key, data; }
        [Serializable] private class ResponseData { public int id; public bool ok; public string data, error; }
        private readonly Dictionary<int, Promise<string>> _pending = new Dictionary<int, Promise<string>>();
        private int _nextId;
        private bool _paused, _ready;
        private float _previousTimeScale, _lastInterstitial;
        private bool _previousAudioPause;
        private float _nextSave;
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void MineArena_Bind(string name);
        [DllImport("__Internal")] private static extern void MineArena_Request(string json);
#endif
        private void Awake()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            MineArena_Bind(gameObject.name);
#endif
        }
        public IPromise<string> Request(string operation, string key = "", string data = "")
        {
            var promise = new Promise<string>();
            if (!IsWebPlatform)
            {
                promise.Reject(new Exception("Yandex platform requires a WebGL player."));
                return promise;
            }
            int id = ++_nextId;
            _pending.Add(id, promise);
#if UNITY_WEBGL && !UNITY_EDITOR
            MineArena_Request(JsonUtility.ToJson(new RequestData { id = id, op = operation, key = key, data = data }));
#endif
            return promise;
        }
        [Preserve] public void OnPlatformResult(string json)
        {
            var response = JsonUtility.FromJson<ResponseData>(json);
            if (!_pending.TryGetValue(response.id, out var promise)) return;
            _pending.Remove(response.id);
            if (response.ok) promise.Resolve(response.data ?? "");
            else promise.Reject(new Exception(response.error));
        }
        [Preserve] public void OnPlatformPause(string value)
        {
            bool paused = value == "1";
            if (_paused == paused) return;
            _paused = paused;
            if (paused)
            {
                _previousTimeScale = Time.timeScale; _previousAudioPause = AudioListener.pause;
                Time.timeScale = 0; AudioListener.pause = true;
                SaveIfReady();
            }
            else { Time.timeScale = _previousTimeScale; AudioListener.pause = _previousAudioPause; }
        }
        private void LateUpdate()
        {
            if (_paused) { Time.timeScale = 0; AudioListener.pause = true; }
            if (_ready && Time.realtimeSinceStartup >= _nextSave)
            { _nextSave = Time.realtimeSinceStartup + 30; SaveIfReady(); }
        }
        private void SaveIfReady()
        {
            if (_ready && SaveService.Instance.IsLoaded) SaveService.Instance.Save().Catch(Debug.LogException);
        }
        public void Ready() => StartCoroutine(ReadyNextFrame());
        private IEnumerator ReadyNextFrame()
        {
            yield return null;
            _ready = true; _nextSave = Time.realtimeSinceStartup + 30;
            Request("ready").Catch(Debug.LogException);
            SetGameplay(true);
            GetComponent<YandexPurchases>()?.Restore();
        }
        public void SetGameplay(bool active)
        {
            if (IsWebPlatform) Request("gameplay", data: active ? "true" : "false").Catch(Debug.LogException);
        }
        public void ShowLevelDoubleRewardsAd(Action<bool> onFinished) => ShowRewarded(onFinished);
        public void ShowReviveAd(Action<bool> onFinished) => ShowRewarded(onFinished);
        public void ShowRewarded(Action<bool> onFinished)
        {
            Request("rewarded").Then(result => onFinished?.Invoke(result == "true"))
                .Catch(_ => onFinished?.Invoke(false));
        }
        public void ShowInterstitial(Action onFinished)
        {
            if (!IsWebPlatform || Time.realtimeSinceStartup - _lastInterstitial < 120)
            { onFinished?.Invoke(); return; }
            _lastInterstitial = Time.realtimeSinceStartup;
            Request("interstitial").Then(_ => onFinished?.Invoke()).Catch(_ => onFinished?.Invoke());
        }
        public void StartupError() { Request("error").Catch(Debug.LogException); }
    }
}
