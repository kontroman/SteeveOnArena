using Devotion.SDK.Enums;
using Devotion.SDK.Interfaces;
using System;

namespace Devotion.SDK.Async
{
    public class Promise : IPromise
    {
        private event Action _resolved;
        private event Action<Exception> _rejected;
        private event Action _finally;

        private PromiseState _state = PromiseState.Pending;
        private Exception _exception;

        public PromiseState State => _state;

        public IPromise Then(Action onResolved)
        {
            if (_state == PromiseState.Resolved) onResolved?.Invoke();
            else if (_state == PromiseState.Pending) _resolved += onResolved;
            return this;
        }

        public IPromise Then(Func<IPromise> onResolved)
        {
            var chained = new Promise();
            Then(() =>
            {
                try
                {
                    var next = onResolved();
                    next.Then(() => chained.Resolve())
                        .Catch(ex => chained.Reject(ex));
                }
                catch (Exception ex)
                {
                    chained.Reject(ex);
                }
            });
            Catch(ex => chained.Reject(ex));
            return chained;
        }

        public IPromise Catch(Action<Exception> onRejected)
        {
            if (_state == PromiseState.Rejected) onRejected?.Invoke(_exception);
            else if (_state == PromiseState.Pending) _rejected += onRejected;
            return this;
        }

        public IPromise Finally(Action onFinally)
        {
            if (_state != PromiseState.Pending) onFinally?.Invoke();
            else _finally += onFinally;
            return this;
        }

        public void Resolve()
        {
            if (_state != PromiseState.Pending) return;
            _state = PromiseState.Resolved;
            _resolved?.Invoke();
            _finally?.Invoke();
            Clear();
        }

        public static IPromise ResolveAndReturn()
        {
            var promise = new Promise();
            promise.Resolve();
            return promise;
        }

        public static IPromise RejectAndReturn(Exception ex)
        {
            var promise = new Promise();
            promise.Reject(ex);
            return promise;
        }

        public void Reject(Exception ex)
        {
            if (_state != PromiseState.Pending) return;
            _state = PromiseState.Rejected;
            _exception = ex;
            _rejected?.Invoke(ex);
            _finally?.Invoke();
            Clear();
        }

        private void Clear()
        {
            _resolved = null;
            _rejected = null;
            _finally = null;
        }
    }

    public class Promise<T> : IPromise<T>
    {
        private event Action<T> _resolved;
        private event Action<Exception> _rejected;
        private event Action _finally;

        private PromiseState _state = PromiseState.Pending;
        private Exception _exception;
        private T _value;

        public PromiseState State => _state;

        public IPromise<T> Then(Action<T> onResolved)
        {
            if (_state == PromiseState.Resolved) onResolved?.Invoke(_value);
            else if (_state == PromiseState.Pending) _resolved += onResolved;
            return this;
        }

        public IPromise Then(Func<T, IPromise> onResolved)
        {
            var chained = new Promise();
            Then(value =>
            {
                try
                {
                    var next = onResolved(value);
                    next.Then(() => chained.Resolve())
                        .Catch(ex => chained.Reject(ex));
                }
                catch (Exception ex)
                {
                    chained.Reject(ex);
                }
            });
            Catch(ex => chained.Reject(ex));
            return chained;
        }

        public IPromise<T> Catch(Action<Exception> onRejected)
        {
            if (_state == PromiseState.Rejected) onRejected?.Invoke(_exception);
            else if (_state == PromiseState.Pending) _rejected += onRejected;
            return this;
        }

        public IPromise<T> Finally(Action onFinally)
        {
            if (_state != PromiseState.Pending) onFinally?.Invoke();
            else _finally += onFinally;
            return this;
        }

        public void Resolve(T value)
        {
            if (_state != PromiseState.Pending) return;
            _state = PromiseState.Resolved;
            _value = value;
            _resolved?.Invoke(value);
            _finally?.Invoke();
            Clear();
        }

        public static IPromise<T> ResolveAndReturn(T value)
        {
            var promise = new Promise<T>();
            promise.Resolve(value);
            return promise;
        }

        public void Reject(Exception ex)
        {
            if (_state != PromiseState.Pending) return;
            _state = PromiseState.Rejected;
            _exception = ex;
            _rejected?.Invoke(ex);
            _finally?.Invoke();
            Clear();
        }

        private void Clear()
        {
            _resolved = null;
            _rejected = null;
            _finally = null;
        }
    }
}