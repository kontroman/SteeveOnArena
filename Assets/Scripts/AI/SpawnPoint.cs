using MineArena.Controllers;
using UnityEngine;
using UnityEngine.AI;

namespace MineArena.AI
{
    public class SpawnPoint : MonoBehaviour
    {
        [SerializeField] private float _cooldown = 5f;
        [SerializeField] private LayerMask _enemyLayer;
        [SerializeField] private float _radius;
        [SerializeField] private Camera _targetCamera;

        private float _nextSpawnTime;

        private bool IsInCameraView()
        {
            Camera cam = _targetCamera ? _targetCamera : Camera.main;
            if (!cam)
                return false;

            Vector3 viewportPos = cam.WorldToViewportPoint(transform.position);
            return viewportPos.z > 0f
                && viewportPos.x >= 0f && viewportPos.x <= 1f
                && viewportPos.y >= 0f && viewportPos.y <= 1f;
        }
        private bool IsPositionClear() => Physics.OverlapSphere(transform.position, _radius, _enemyLayer).Length == 0;

        public bool IsReadyForSpawn()
        {
            return isActiveAndEnabled && Time.time >= _nextSpawnTime
                && IsPositionClear()
                && TryGetSpawnPosition(new NavMeshQueryFilter { agentTypeID = 0, areaMask = NavMesh.AllAreas }, out _)
                && (MineArena.Managers.TutorialService.Expedition || !IsInCameraView());
        }

        private bool TryGetSpawnPosition(NavMeshQueryFilter filter, out Vector3 position)
        {
            position = transform.position;
            if (!NavMesh.SamplePosition(position, out var hit, 2f, filter)) return false;
            position = hit.position;
            if (Player.Instance == null) return false;
            if (!NavMesh.SamplePosition(Player.Instance.transform.position, out var player, 3f, filter)) return false;
            var path = new NavMeshPath();
            return NavMesh.CalculatePath(position, player.position, filter, path) && path.status == NavMeshPathStatus.PathComplete;
        }

        public bool TrySpawn(GameObject mobObject)
        {
            if (!IsReadyForSpawn())
                return false;
            var agent = mobObject.GetComponent<NavMeshAgent>();
            var position = transform.position;
            if (agent != null)
            {
                var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
                if (!TryGetSpawnPosition(filter, out position)) return false;
                // Pool retrieval activates the agent at its old position. Teleport its navigation state too.
                agent.enabled = false;
            }
            mobObject.transform.position = position;
            mobObject.transform.LookAt(Player.Instance.transform.position);
            mobObject.SetActive(true);
            if (agent != null)
            {
                agent.enabled = true;
                if (!agent.Warp(position)) return false;
                agent.ResetPath();
            }

            _nextSpawnTime = Time.time + Mathf.Max(0f, _cooldown);
            return true;
        }
    }
}
