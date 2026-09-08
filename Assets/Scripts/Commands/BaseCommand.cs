using System;
using System.Threading.Tasks;
using UnityEngine;

namespace MineArena.Commands
{
    public abstract class BaseCommand : ScriptableObject, ICommand
    {
        public virtual Task Execute(Action callback)
        {
            return Task.FromException(new NotSupportedException($"{GetType().Name} does not support Execute(Action)."));
        }

        public virtual Task Execute(Component component)
        {
            return Task.FromException(new NotSupportedException($"{GetType().Name} does not support Execute(Component)."));
        }

        public virtual Task Execute(object data)
        {
            return Task.FromException(new NotSupportedException($"{GetType().Name} does not support Execute(object)."));
        }
    }
}
