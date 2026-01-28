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
        [Header("Facing")]
        [SerializeField] private FacingAxis _facingAxis = FacingAxis.PositiveZ;

        private Quaternion _axisCorrection = Quaternion.identity;

        private enum FacingAxis
        {
            PositiveZ,
            NegativeZ,
            PositiveX,
            NegativeX,
            PositiveY,
            NegativeY
        }

        public void SetPlayerTransform(Transform playerTransform) => _playerTransform = playerTransform;

        private void Awake()
        {
            UpdateAxisCorrection();
        }

        private void Start()
        {
            _agent = GetComponent<NavMeshAgent>();
            if (_agent != null)
            {
                _agent.updateRotation = false;
            }
            _playerTransform = Player.Instance.GetComponentFromList<Transform>();
            _mobAnimator = GetComponent<MobAnimationController>();
        }

        private void Update()
        {
            if (_playerTransform)
            {
                _playerTransform = Player.Instance.GetComponentFromList<Transform>();
                _agent.SetDestination(_playerTransform.position);
            }

            UpdateFacing();

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

        public Quaternion ApplyAxisCorrection(Quaternion rotation)
        {
            return rotation * _axisCorrection;
        }

        public Quaternion GetAxisCorrectedLookRotation(Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.0001f)
                return transform.rotation;

            Quaternion lookRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            return ApplyAxisCorrection(lookRotation);
        }

        private void UpdateFacing()
        {
            if (_agent == null || _playerTransform == null)
                return;

            Vector3 direction = _playerTransform.position - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.0001f)
                return;

            transform.rotation = GetAxisCorrectedLookRotation(direction);
        }

        private void UpdateAxisCorrection()
        {
            Vector3 axisVector = GetAxisVector(_facingAxis);
            if (axisVector == Vector3.zero)
            {
                _axisCorrection = Quaternion.identity;
                return;
            }

            _axisCorrection = Quaternion.Inverse(Quaternion.FromToRotation(Vector3.forward, axisVector));
        }

        private static Vector3 GetAxisVector(FacingAxis axis)
        {
            switch (axis)
            {
                case FacingAxis.PositiveZ:
                    return Vector3.forward;
                case FacingAxis.NegativeZ:
                    return Vector3.back;
                case FacingAxis.PositiveX:
                    return Vector3.right;
                case FacingAxis.NegativeX:
                    return Vector3.left;
                case FacingAxis.PositiveY:
                    return Vector3.up;
                case FacingAxis.NegativeY:
                    return Vector3.down;
                default:
                    return Vector3.forward;
            }
        }

        public void SetParameters(MobPreset preset)
        {
            _agent.speed = preset.Speed;
            _agent.stoppingDistance = preset.AttackRange;
            _stoppingDistance = _agent.stoppingDistance;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            UpdateAxisCorrection();
        }
#endif
    }
}
