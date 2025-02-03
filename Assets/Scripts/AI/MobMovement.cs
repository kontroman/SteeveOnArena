using Devotion.Controllers;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AI;

namespace Devotion.AI
{
    public class MobMovement : MonoBehaviour
    {
        private Transform _playerTransform;
        private bool _isMove = true;
        private NavMeshAgent _agent;

        public void SetPlayerTransform(Transform playerTransform) => _playerTransform = playerTransform;

        private void Start()
        {
            _agent = GetComponent<NavMeshAgent>();
            _playerTransform = Player.Instance.GetComponentFromList<Transform>();
        }

        private void Update()
        {
            if (_isMove && _playerTransform)
                _agent.SetDestination(_playerTransform.position);
        }

        public void Move()
        {
            _isMove = true;
        }

        public float DistanceToPlayer()
        {
            return _agent.remainingDistance;
        }
    }
}
