using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Devotion.SDK.Interfaces;
using Devotion.SDK.Async;
using Devotion.SDK.Managers;

namespace Devotion.SDK.Controllers
{
    public class UnitySceneLoader : BaseManager, ISceneLoader
    {
        public IPromise LoadSceneAsync(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
        {
            var promise = new Promise();
            StartCoroutine(LoadRoutine(sceneName, mode, promise));
            return promise;
        }

        public IPromise UnloadSceneAsync(string sceneName)
        {
            var promise = new Promise();
            StartCoroutine(UnloadRoutine(sceneName, promise));
            return promise;
        }

        private IEnumerator LoadRoutine(string sceneName, LoadSceneMode mode, Promise promise)
        {
            var async = SceneManager.LoadSceneAsync(sceneName, mode);
            async.allowSceneActivation = false;

            async.allowSceneActivation = true;
            yield return new WaitUntil(() => async.isDone);

            promise.Resolve();
        }

        private IEnumerator UnloadRoutine(string sceneName, Promise promise)
        {
            var async = SceneManager.UnloadSceneAsync(sceneName);
            yield return async;
            promise.Resolve();
        }
    }
}