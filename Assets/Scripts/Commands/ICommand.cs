using System;
using UnityEngine;
using System.Threading.Tasks;

namespace MineArena.Commands
{
    public interface ICommand
    {
        Task Execute(Action callback);
        Task Execute(Component component);
        Task Execute(object data);
    }
}