using UnityEngine;

namespace Devotion.SDK.Base
{

    [CreateAssetMenu(fileName = "New " + nameof(DevotionRemoteConfig), menuName = "New " + nameof(DevotionRemoteConfig))]
    public class DevotionRemoteConfig : ScriptableObject
    {
        [SerializeField] private RemoteConfig _remoteConfig;
    }
}