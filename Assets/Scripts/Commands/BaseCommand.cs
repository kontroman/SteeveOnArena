using Devotion.Commands;
using System;
using System.Threading.Tasks;
using UnityEngine;

public abstract class BaseCommand : ScriptableObject, ICommand
{
    public virtual async Task Execute(Action callback)
    {
        await Task.Run(() => Execute(callback));
        //callback?.Invoke();
    }
}
