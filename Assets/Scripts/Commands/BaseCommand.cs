using Devotion.Commands;
using UnityEngine;

public abstract class BaseCommand : ScriptableObject, ICommand
{
    public abstract void Execute();

    public virtual void Execute(System.Action callback)
    {
        Execute();
        callback?.Invoke();
    }
}
