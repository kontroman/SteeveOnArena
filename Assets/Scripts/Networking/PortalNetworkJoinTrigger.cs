using Devotion.SDK.Helpers;
using UnityEngine;

namespace MineArena.Networking
{
    [DisallowMultipleComponent]
    public class PortalNetworkJoinTrigger : MonoBehaviour
    {
        [Header("Arena")]
        [SerializeField, Tooltip("Room/arena id to join on this portal.")]
        private string portalRoomId = "arena-1";
        [SerializeField, Tooltip("Optional spawn point for local player when the portal does not load another scene.")]
        private Transform spawnPoint;

        [Header("Scene Loading")]
        [SerializeField, Tooltip("Load arena scene before connecting to the multiplayer room.")]
        private bool loadArenaSceneBeforeConnect = true;
        [SerializeField, Tooltip("Scene with the multiplayer arena. Add it to Build Settings.")]
        private string arenaSceneName = "GameplayScene";
        [SerializeField, Tooltip("Optional object name to find as spawn point after the arena scene is loaded.")]
        private string arenaSpawnPointName = "NetworkSpawnPoint";

        [Header("Behaviour")]
        [SerializeField, Tooltip("Ignore repeated trigger enters after the first connect attempt.")]
        private bool singleUseUntilDisconnect = true;
        [SerializeField]
        private bool debugLogs = true;

        private bool joinInProgress;
        private bool joined;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.IsPlayer())
                return;

            if (singleUseUntilDisconnect && (joinInProgress || joined))
                return;

            var manager = NetworkClientManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning("[PortalNetworkJoinTrigger] NetworkClientManager is missing in scene.");
                return;
            }

            joinInProgress = true;
            var sceneToLoad = loadArenaSceneBeforeConnect ? arenaSceneName : null;
            manager.ConnectThroughPortal(portalRoomId, sceneToLoad, arenaSpawnPointName, spawnPoint);
            joined = true;

            if (debugLogs)
                Debug.Log("[PortalNetworkJoinTrigger] Portal network flow started. Room=" + portalRoomId + ", scene=" + sceneToLoad);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            var collider = GetComponent<Collider>();
            if (collider != null)
                Gizmos.DrawWireCube(collider.bounds.center, collider.bounds.size);

            if (spawnPoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(spawnPoint.position, 0.35f);
                Gizmos.DrawLine(transform.position, spawnPoint.position);
            }
        }
    }
}
