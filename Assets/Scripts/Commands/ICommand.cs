using System;
using UnityEngine;
using System.Threading.Tasks;

namespace Devotion.Commands
{
    public interface ICommand
    {
        Task Execute(Action callback);
        Task Execute(Component component);
    }
}