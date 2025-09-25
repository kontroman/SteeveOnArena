namespace Devotion.SDK.Interfaces
{
    public interface ISaveService
    {
        IPromise Save();
        IPromise Load();
    }
}
