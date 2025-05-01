using MineArena.Structs;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace MineArena.Commands
{
    public class DamageCommand : ScriptableObject, ICommand
    {
        public Task Execute(Action callback)
        {
            throw new NotImplementedException();
        }

        public Task Execute(object data)
        {
            if (data is DamageData damageData && damageData.Target != null)
            {
                damageData.Target.TakeDamage(damageData);
            }

            return Task.CompletedTask;
        }

        public Task Execute(Component component)
        {
            throw new NotImplementedException();
        }
    }
}