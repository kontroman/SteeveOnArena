using UnityEngine;

namespace MineArena.Levels
{
    /// <summary>
    /// Marker component for ore spawn locations inside an arena.
    /// </summary>
    public class OreSpawnPoint : MonoBehaviour
    {
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(transform.position, 0.2f);
        }
#endif
    }
}
