using System;
using System.Threading.Tasks;

namespace Devotion.Commands
{
    public interface ICommand
    {
        Task Execute(Action callback);
    }
}