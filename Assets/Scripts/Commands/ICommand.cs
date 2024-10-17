namespace Devotion.Commands
{
    public interface ICommand
    {
        void Execute();
        void Execute(System.Action callback);
    }
}