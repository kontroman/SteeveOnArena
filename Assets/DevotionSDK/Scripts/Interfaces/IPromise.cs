using Devotion.SDK.Enums;
using System;

namespace Devotion.SDK.Interfaces
{

    public interface IPromise
    {
        IPromise Then(Action onResolved);
        IPromise Then(Func<IPromise> onResolved);
        IPromise Catch(Action<Exception> onRejected);
        IPromise Finally(Action onFinally);

        void Resolve();
        void Reject(Exception ex);
        PromiseState State { get; }
    }

    public interface IPromise<T>
    {
        IPromise<T> Then(Action<T> onResolved);
        IPromise Then(Func<T, IPromise> onResolved);
        IPromise<T> Catch(Action<Exception> onRejected);
        IPromise<T> Finally(Action onFinally);

        void Resolve(T value);
        void Reject(Exception ex);
        PromiseState State { get; }
    }
}