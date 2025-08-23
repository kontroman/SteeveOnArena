namespace Devotion.SDK.Interfaces
{
    public interface ISceneInitializer
    {
        string SceneName { get; }
        IPromise Initialize();
    }
}