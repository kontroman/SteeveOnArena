using System;
using System.Collections;
using UnityEngine;

namespace Devotion.SDK.Helpers
{
    public static class CoroutineHelper
    {
        private class CoroutineRunner : MonoBehaviour { }

        private static CoroutineRunner _runner;

        private static CoroutineRunner Runner
        {
            get
            {
                if (_runner == null)
                {
                    GameObject go = new GameObject("[CoroutineHelper]");
                    UnityEngine.Object.DontDestroyOnLoad(go);
                    _runner = go.AddComponent<CoroutineRunner>();
                }

                return _runner;
            }
        }

        public static Coroutine Delay(float delay, Action action)
        {
            return Runner.StartCoroutine(DelayCoroutine(delay, action));
        }

        public static Coroutine NextFrame(Action action)
        {
            return Runner.StartCoroutine(NextFrameCoroutine(action));
        }

        private static IEnumerator DelayCoroutine(float delay, Action action)
        {
            yield return new WaitForSeconds(delay);
            action?.Invoke();
        }

        private static IEnumerator NextFrameCoroutine(Action action)
        {
            yield return null;
            action?.Invoke();
        }
    }
}