using MineArena.Controllers;
using MineArena.ObjectPools;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MineArena.AI
{
    public class SpawnPoint : MonoBehaviour
    {
        private bool _isReady;

        [SerializeField] private float _cooldown = 5f;
        [SerializeField] private LayerMask _enemyLayer;
        [SerializeField] private float _radius;
        [SerializeField] private Camera _targetCamera;

        private Transform _playerTransform;

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

        void Start()
        {
            UpdateState();
        }

        public bool IsReadyForSpawn()
        {
            return IsPositionClear() && !IsInCameraView();
        }

        public bool TrySpawn(GameObject mobObject)
        {
            if (_isReady)
            {
                Debug.Log(transform.position);
                mobObject.transform.position = transform.position;
                mobObject.transform.LookAt(Player.Instance.transform.position);
                mobObject.SetActive(true);
                StartCooldown();
                return true;
            }
            else
                return false;
        }

        private void StartCooldown()
        {
            _isReady = false;
            Invoke("UpdateState", _cooldown);
        }

        private IEnumerator CheckReadinessForSpawn()
        {
            UpdateState();
            yield return new WaitForSeconds(_cooldown);
        }

        private void UpdateState() => _isReady = IsReadyForSpawn();
    }
}
