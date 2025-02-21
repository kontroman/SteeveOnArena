using Devotion.SDK.Interfaces;
using UnityEngine;

public class BaseService : MonoBehaviour, IService
{
    public virtual void Initialize()
    {
        Debug.Log($"[CLIENT] : {GetType().Name} initializing.");
    }
}
