using Devotion.SDK.Async;
using Devotion.SDK.Interfaces;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Devotion.SDK.Controllers
{
    public class SceneTransitionController : MonoBehaviour
    {
        [SerializeField] private UnitySceneLoader _loader;
        [SerializeField] private float _minLoadingTime = 2f;

        private readonly Dictionary<string, ISceneInitializer> _initializers = new();

        public event Action<float> OnProgressChanged;
        public event Action OnLoadingStarted;
        public event Action OnLoadingCompleted;

        public void RegisterInitializer(ISceneInitializer initializer)
        {
            _initializers[initializer.SceneName] = initializer;
        }

        public IPromise TransitionTo(string sceneName)
        {
            OnLoadingStarted?.Invoke();

            return _loader.LoadSceneAsync(sceneName)
                .Then(progress =>
                {
                    OnProgressChanged?.Invoke(progress);
                    return _initializers.ContainsKey(sceneName)
                        ? _initializers[sceneName].Initialize()
                        : Promise.ResolveAndReturn();
                })
                .Then(() =>
                {
                    OnLoadingCompleted?.Invoke();
                });
        }
    }
}