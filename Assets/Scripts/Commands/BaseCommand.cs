using System;
using System.Threading.Tasks;
using UnityEngine;

namespace MineArena.Commands
{
    public abstract class BaseCommand : ScriptableObject, ICommand
    {
        public virtual async Task Execute(Action callback)
        {
            await Task.Run(() => Execute(callback));
            //callback?.Invoke();
        }

        public virtual async Task Execute(Component component)
        {
            await Task.Run(() => Execute(component));
        }

        public Task Execute(object data)
        {
            throw new NotImplementedException();
        }
    }
}
