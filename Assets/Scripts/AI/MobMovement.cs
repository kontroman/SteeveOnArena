using MineArena.Controllers;
using Sirenix.Config;
using UnityEngine;
using UnityEngine.AI;

namespace MineArena.AI
{
    //NOTE: пока что дистанция осстановки регулируется через навмеш агента.
    public class MobMovement : MonoBehaviour
    {
        private Transform _playerTransform;
        private NavMeshAgent _agent;

        public void SetPlayerTransform(Transform playerTransform) => _playerTransform = playerTransform;

        private void Start()
        {
            _agent = GetComponent<NavMeshAgent>();
            _playerTransform = Player.Instance.GetComponentFromList<Transform>();
        }

        private void Update()
        {
            if ( _playerTransform)
            {
                _playerTransform = Player.Instance.GetComponentFromList<Transform>();
                _agent.SetDestination(_playerTransform.position);
            }
        }

        public float DistanceToPlayer()
        {
            return _agent.remainingDistance;
        }

        public void Stop()
        {
            _agent.isStopped = true;
        }

        public void Move()
        {
            _agent.isStopped = false;
        }
    }
}
