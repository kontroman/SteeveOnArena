using MineArena.Controllers;
using MineArena.Interfaces;
using MineArena.PlayerSystem;
using System.Collections;
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
        private Coroutine _retreatRoutine;
        private bool _isRetreating;
        private bool _isAfk;
        private bool _isDead;

        private const float RetreatDuration = 1.5f;
        [Header("Facing")]
        [SerializeField] private bool _useFacingAxis;
        [SerializeField] private FacingAxis _facingAxis = FacingAxis.PositiveZ;
        [SerializeField] private FacingAxis _facingUpAxis = FacingAxis.PositiveY;
        [SerializeField] private Vector3 _facingRotationOffsetEuler = Vector3.zero;

        private Quaternion _axisCorrection = Quaternion.identity;
        private Quaternion _manualFacingOffset = Quaternion.identity;

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

        private void OnEnable()
        {
            PlayerMovement.PlayerDied += HandlePlayerDied;

            _isDead = false;
            _isAfk = false;
            _isRetreating = false;

            if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
                _agent.isStopped = false;

            if (PlayerMovement.IsPlayerDead && Player.Instance != null)
                StartRetreatFrom(Player.Instance.transform);
        }

        private void OnDisable()
        {
            PlayerMovement.PlayerDied -= HandlePlayerDied;

            if (_retreatRoutine != null)
            {
                StopCoroutine(_retreatRoutine);
                _retreatRoutine = null;
            }
        }

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

            if (Player.Instance != null)
                _playerTransform = Player.Instance.GetComponentFromList<Transform>();

            _mobAnimator = GetComponent<MobAnimationController>();
        }

        private void Update()
        {
            if (_isDead)
            {
                if (_agent != null)
                    _mobAnimator?.UpdateMoveState(Vector3.zero, true);
                return;
            }

            if (_isAfk)
            {
                _mobAnimator?.ForceIdle();
                return;
            }

            if (_playerTransform == null && Player.Instance != null)
                _playerTransform = Player.Instance.GetComponentFromList<Transform>();

            if (_agent != null && _agent.isOnNavMesh && _playerTransform != null && !_isRetreating)
                _agent.SetDestination(_playerTransform.position);

            if (_isRetreating)
                UpdateRetreatFacing();
            else
                UpdateFacing();

            if (_agent != null)
                _mobAnimator?.UpdateMoveState(_agent.velocity, _agent.isStopped);
        }

        public float DistanceToPlayer()
        {
            return _agent != null ? _agent.remainingDistance : float.PositiveInfinity;
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
            if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh)
                return;

            _agent.isStopped = true;
        }

        public void Move()
        {
            if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh || _isAfk || _isDead)
                return;

            _agent.isStopped = false;
        }

        public void HandleDeath()
        {
            if (_isDead)
                return;

            _isDead = true;
            _isAfk = false;
            _isRetreating = false;

            if (_retreatRoutine != null)
            {
                StopCoroutine(_retreatRoutine);
                _retreatRoutine = null;
            }

            if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh)
                return;

            _agent.isStopped = true;
            _agent.ResetPath();
            _agent.velocity = Vector3.zero;
        }

        public Quaternion ApplyAxisCorrection(Quaternion rotation)
        {
            Quaternion axisCorrection = _useFacingAxis ? _axisCorrection : Quaternion.identity;
            return rotation * axisCorrection * _manualFacingOffset;
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

        private void UpdateRetreatFacing()
        {
            if (_agent == null)
                return;

            Vector3 direction = _agent.velocity;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.0001f)
                return;

            transform.rotation = GetAxisCorrectedLookRotation(direction);
        }

        private void HandlePlayerDied(Transform playerTransform)
        {
            if (_isDead)
                return;

            StartRetreatFrom(playerTransform);
        }

        private void StartRetreatFrom(Transform playerTransform)
        {
            if (_isDead || playerTransform == null)
                return;

            if (_agent == null)
                _agent = GetComponent<NavMeshAgent>();

            if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh || !isActiveAndEnabled)
                return;

            if (_retreatRoutine != null)
                StopCoroutine(_retreatRoutine);

            _retreatRoutine = StartCoroutine(RetreatRoutine(playerTransform));
        }

        private IEnumerator RetreatRoutine(Transform playerTransform)
        {
            _isAfk = false;
            _isRetreating = true;

            Vector3 retreatDirection = transform.position - playerTransform.position;
            retreatDirection.y = 0f;

            if (retreatDirection.sqrMagnitude < 0.0001f)
                retreatDirection = -transform.forward;

            retreatDirection.Normalize();

            float distance = Mathf.Max(_agent.speed * RetreatDuration, _agent.stoppingDistance + 0.5f);
            Vector3 targetPosition = transform.position + retreatDirection * distance;

            if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, distance, _agent.areaMask))
                targetPosition = hit.position;

            if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
            {
                _agent.isStopped = false;
                _agent.ResetPath();
                _agent.SetDestination(targetPosition);
            }

            yield return new WaitForSeconds(RetreatDuration);

            _isRetreating = false;
            _isAfk = true;

            if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
            {
                _agent.isStopped = true;
                _agent.ResetPath();
                _agent.velocity = Vector3.zero;
            }

            _mobAnimator?.ForceIdle();
            _retreatRoutine = null;
        }

        private void UpdateAxisCorrection()
        {
            _manualFacingOffset = Quaternion.Euler(_facingRotationOffsetEuler);

            if (!_useFacingAxis)
            {
                _axisCorrection = Quaternion.identity;
                return;
            }

            Vector3 forwardAxis = GetAxisVector(_facingAxis);
            Vector3 upAxis = GetAxisVector(_facingUpAxis);

            if (forwardAxis == Vector3.zero || upAxis == Vector3.zero)
            {
                _axisCorrection = Quaternion.identity;
                return;
            }

            bool invalidUpAxis = Mathf.Abs(Vector3.Dot(forwardAxis.normalized, upAxis.normalized)) > 0.999f;
            if (invalidUpAxis)
            {
                _axisCorrection = Quaternion.identity;
                return;
            }

            Quaternion localAxesRotation = Quaternion.LookRotation(forwardAxis, upAxis);
            _axisCorrection = Quaternion.Inverse(localAxesRotation);
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
            _isDead = false;
            if (_agent == null) return;

            _agent.speed = preset.Speed;
            _agent.stoppingDistance = preset.AttackRange;
            _stoppingDistance = _agent.stoppingDistance;

            if (_agent.enabled && _agent.isOnNavMesh && !_isAfk)
                _agent.isStopped = false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            UpdateAxisCorrection();
        }
#endif
    }
}
