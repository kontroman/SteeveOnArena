using Devotion.SDK.Async;
using Devotion.SDK.Interfaces;
using UnityEngine;

public class BaseService : MonoBehaviour, IService
{
    public virtual IPromise Initialize()
    {
        Debug.Log($"[CLIENT] : {GetType().Name} initializing.");

        return Promise.ResolveAndReturn();
    }
}
