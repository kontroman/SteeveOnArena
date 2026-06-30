using MineArena.Controllers;
using UnityEngine;

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
            return Time.time >= _nextSpawnTime
                && IsPositionClear()
                && !IsInCameraView();
        }

        public bool TrySpawn(GameObject mobObject)
        {
            if (!IsReadyForSpawn())
                return false;

            Debug.Log(transform.position);
            mobObject.transform.position = transform.position;
            mobObject.transform.LookAt(Player.Instance.transform.position);
            mobObject.SetActive(true);

            _nextSpawnTime = Time.time + Mathf.Max(0f, _cooldown);
            return true;
        }
    }
}
