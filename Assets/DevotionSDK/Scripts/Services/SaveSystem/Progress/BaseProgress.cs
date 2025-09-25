using System;

namespace Devotion.SDK.Services.SaveSystem.Progress
{
    [Serializable]
    public class BaseProgress
    {
        public void Save() => Messages.Player.SavePlayerProgress.Publish();
    }
}