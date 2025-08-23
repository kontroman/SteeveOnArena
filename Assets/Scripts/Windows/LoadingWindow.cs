using Devotion.SDK.Base;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Devotion.SDK.Interfaces;
using Devotion.SDK.Async;

namespace MineArena.Windows
{
    public class LoadingWindow : BaseWindow
    {
        [SerializeField] private Slider _progressBar;
        [SerializeField] private float _animationDuration = 0.2f;

        private Coroutine _animationCoroutine;
        private float _currentTargetValue;
        private Promise _currentAnimationPromise;

        private void Awake()
        {
            InitWindow();
        }

        private void InitWindow()
        {
            SetProgressValueImmediate(0);
            _currentTargetValue = 0;
        }

        public IPromise SetProgressValue(float value)
        {
            value = Mathf.Clamp01(value);

            if (Mathf.Approximately(_currentTargetValue, value))
                return Promise.ResolveAndReturn();

            _currentTargetValue = value;

            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
                if (_currentAnimationPromise != null)
                {
                    _currentAnimationPromise.Resolve();
                }
            }

            _currentAnimationPromise = new Promise();
            _animationCoroutine = StartCoroutine(AnimateProgressBar(value, _currentAnimationPromise));
            return _currentAnimationPromise;
        }

        private IEnumerator AnimateProgressBar(float targetValue, Promise promise)
        {
            float startValue = _progressBar.value;
            float elapsedTime = 0f;

            while (elapsedTime < _animationDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / _animationDuration);

                _progressBar.value = Mathf.Lerp(startValue, targetValue, SmoothStep(t));

                yield return null;
            }

            _progressBar.value = targetValue;
            _animationCoroutine = null;
            promise.Resolve();
            _currentAnimationPromise = null;
        }

        private float SmoothStep(float t)
        {
            return t * t * (3f - 2f * t);
        }

        public void SetProgressValueImmediate(float value)
        {
            value = Mathf.Clamp01(value);
            _currentTargetValue = value;

            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
                if (_currentAnimationPromise != null)
                {
                    _currentAnimationPromise.Resolve();
                    _currentAnimationPromise = null;
                }
                _animationCoroutine = null;
            }

            _progressBar.value = value;
        }

        private void OnDestroy()
        {
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
                if (_currentAnimationPromise != null)
                {
                    _currentAnimationPromise.Resolve();
                }
            }
        }
    }
}