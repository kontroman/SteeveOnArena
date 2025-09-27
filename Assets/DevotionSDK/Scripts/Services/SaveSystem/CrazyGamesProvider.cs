using Devotion.SDK.Interfaces;

namespace Devotion.SDK.Services.SaveSystem
{
    public class CrazyGamesProvider : ISaveProvider
    {
        public IPromise<string> Load(string key)
        {
            throw new System.NotImplementedException();
        }

        public IPromise Save(string key, string data)
        {
            throw new System.NotImplementedException();
        }
    }
}
