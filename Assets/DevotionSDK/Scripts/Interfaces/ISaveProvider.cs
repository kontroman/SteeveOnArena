namespace Devotion.SDK.Interfaces
{
    public interface ISaveProvider
    {
        IPromise Save(string key, string data);
        IPromise<string> Load(string key);
    }
}
