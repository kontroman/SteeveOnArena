using UnityEngine.SceneManagement;

namespace Devotion.SDK.Interfaces
{
    public interface ISceneLoader
    {
        IPromise LoadSceneAsync(string sceneName, LoadSceneMode mode = LoadSceneMode.Single);
        IPromise UnloadSceneAsync(string sceneName);
    }
}