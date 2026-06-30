using System;
using Devotion.SDK.Async;
using Devotion.SDK.Base;
using Devotion.SDK.Enums;
using Devotion.SDK.Interfaces;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace MineArena.Windows
{
    public class BlackWindow : BaseWindow
    {
        [SerializeField] private Image blackScreen;

        private Tween _fadeTween;
        private Promise _fadePromise;

        public void DoCasualFade(bool toInvisible, float duration = 0.75f)
        {
            StartFade(toInvisible, duration, null);
        }

        public IPromise DoFade(bool toInvisible, float duration = 0.75f)
        {
            var promise = new Promise();
            StartFade(toInvisible, duration, promise);
            return promise;
        }

        private void StartFade(bool toInvisible, float duration, Promise promise)
        {
            if (blackScreen == null)
            {
                promise?.Reject(new NullReferenceException($"{nameof(BlackWindow)}: {nameof(blackScreen)} is not assigned."));
                return;
            }

            CancelActiveFade();

            _fadePromise = promise;
            float targetAlpha = toInvisible ? 0f : 1f;

            if (duration <= 0f || Mathf.Approximately(blackScreen.color.a, targetAlpha))
            {
                SetAlpha(targetAlpha);
                ResolvePromise(promise);
                ClearPromise(promise);
                return;
            }

            Tween tween = null;
            tween = blackScreen
                .DOFade(targetAlpha, duration)
                .OnComplete(() =>
                {
                    ResolvePromise(promise);
                    ClearTween(tween);
                    ClearPromise(promise);
                })
                .OnKill(() =>
                {
                    ClearTween(tween);
                });

            _fadeTween = tween;
        }

        private void CancelActiveFade()
        {
            if (_fadeTween != null && _fadeTween.IsActive())
                _fadeTween.Kill(false);

            if (_fadePromise != null && _fadePromise.State == PromiseState.Pending)
                _fadePromise.Reject(new OperationCanceledException($"{nameof(BlackWindow)} fade was interrupted."));

            _fadeTween = null;
            _fadePromise = null;
        }

        private void SetAlpha(float alpha)
        {
            Color color = blackScreen.color;
            color.a = alpha;
            blackScreen.color = color;
        }

        private void ResolvePromise(Promise promise)
        {
            if (promise != null && promise.State == PromiseState.Pending)
                promise.Resolve();
        }

        private void ClearTween(Tween tween)
        {
            if (_fadeTween == tween)
                _fadeTween = null;
        }

        private void ClearPromise(Promise promise)
        {
            if (_fadePromise == promise)
                _fadePromise = null;
        }
    }
}
