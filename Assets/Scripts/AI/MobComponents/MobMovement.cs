using MineArena.Controllers;
using MineArena.Interfaces;
using UnityEngine;
using UnityEngine.AI;

namespace MineArena.AI
{
    //NOTE: пока что дистанция осстановки регулируется через навмеш агента.
    public class MobMovement : MonoBehaviour, IMobComponent
    {
        private Transform _playerTransform;
        private NavMeshAgent _agent;
        private MobAnimationController _mobAnimator;
        private float _stoppingDistance;
        [Header("Visual")]
        [SerializeField] private Transform _visualRoot;
        [SerializeField] private float _visualYawOffset;

        private Quaternion _baseVisualRotation = Quaternion.identity;

        public void SetPlayerTransform(Transform playerTransform) => _playerTransform = playerTransform;

        private void Awake()
        {
            CacheVisualRotation();
        }

        private void Start()
        {
            _agent = GetComponent<NavMeshAgent>();
            _playerTransform = Player.Instance.GetComponentFromList<Transform>();
            _mobAnimator = GetComponent<MobAnimationController>();

            ApplyVisualOffset();
        }

        private void OnEnable()
        {
            ApplyVisualOffset();
        }

        private void Update()
        {
            if (_playerTransform)
            {
                _playerTransform = Player.Instance.GetComponentFromList<Transform>();
                _agent.SetDestination(_playerTransform.position);
            }

            _mobAnimator?.UpdateMoveState(_agent.velocity, _agent.isStopped);
        }

        public float DistanceToPlayer()
        {
            return _agent.remainingDistance;
        }

        public bool IsInAttackRange(Transform target, float attackRange)
        {
            if (target == null) return false;

            var myRadius = _agent != null ? _agent.radius : 0f;

            float targetRadius = 0f;
            var targetCollider = target.GetComponent<Collider>();
            if (targetCollider != null)
            {
                // Приблизительно оценим радиус по наибольшему полуразмеру.
                var extents = targetCollider.bounds.extents;
                targetRadius = Mathf.Max(extents.x, extents.z);
            }

            float distance = Vector3.Distance(transform.position, target.position);
            return distance <= attackRange + myRadius + targetRadius;
        }

        public void Stop()
        {
            _agent.isStopped = true;
        }

        public void Move()
        {
            _agent.isStopped = false;
        }

        private void CacheVisualRotation()
        {
            if (_visualRoot != null)
            {
                _baseVisualRotation = _visualRoot.localRotation;
            }
        }

        private void ApplyVisualOffset()
        {
            if (_visualRoot == null) return;
            _visualRoot.localRotation = Quaternion.Euler(0f, _visualYawOffset, 0f) * _baseVisualRotation;
        }

        public void SetParameters(MobPreset preset)
        {
            _agent.speed = preset.Speed;
            _agent.stoppingDistance = preset.AttackRange;
            _stoppingDistance = _agent.stoppingDistance;
        }
    }
}
