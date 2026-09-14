using MineArena.Commands;
using MineArena.Controllers;
using MineArena.Basics;
using MineArena.Interfaces;
using MineArena.PlayerSystem;
using MineArena.Managers;
using MineArena.Structs;
using Devotion.SDK.Controllers;
using System.Collections;
using UnityEngine;

namespace MineArena.AI
{
    public class MobCombat : MonoBehaviour, IMobComponent
    {
        [SerializeField] private float _attackRange = 2f;
        [SerializeField] private float _attackDelay = 1f;
        [SerializeField] private float _attackHitFallbackDelay = 0.25f;
        [SerializeField] private float _damage = 10f;
        [SerializeField] private float _rotationSpeed = 50f;
        [SerializeField] private MobAttackType _attackType = MobAttackType.Melee;
        [SerializeField] private GameObject _projectilePrefab;
        [SerializeField] private Transform _firePoint;
        [SerializeField] private float _explosionDelay = 1f;
        [SerializeField] private float _explosionRadius = 2f;
        [SerializeField] private LayerMask _explosionTargetMask = ~0;

        private bool _isAttack;
        private MobMovement _mobMovement;
        private MobAnimationController _mobAnimator;
        private MobTypes _mobType;
        private ICommand _attackCommand;
        private IDamageable _playerDamagable;
        private DamageData _damageData;
        private Transform _playerTransform;
        private Coroutine _attackRoutine;
        private Coroutine _hitFallbackRoutine;
        private int _attackCycleId;
        private bool _attackHitApplied;
        private float _nextAttackTime;
        private bool _isAfk;
        private bool _isDead;

        private void OnEnable()
        {
            _isDead = false;
            _mobAnimator = GetComponent<MobAnimationController>();

            if (_mobAnimator != null)
                _mobAnimator.AttackKeyframeReached += OnAttackKeyframe;

            PlayerMovement.PlayerDied += HandlePlayerDied;
            PlayerMovement.PlayerRevived += HandlePlayerRevived;
            _isAfk = PlayerMovement.IsPlayerDead;
        }

        private void OnDisable()
        {
            StopAttackInternal(false);

            if (_mobAnimator != null)
                _mobAnimator.AttackKeyframeReached -= OnAttackKeyframe;

            PlayerMovement.PlayerDied -= HandlePlayerDied;
            PlayerMovement.PlayerRevived -= HandlePlayerRevived;
        }

        private void Start()
        {
            _mobMovement = GetComponent<MobMovement>();

            TryResolvePlayer();

            _damageData = new DamageData(_damage, _playerDamagable);

            UpdateAttackCommand();

            _nextAttackTime = Time.time;
        }

        private void Update()
        {
            if (_isAfk || _isDead)
                return;

            if (_mobMovement == null)
                return;

            if (_playerTransform == null || _playerDamagable == null)
            {
                TryResolvePlayer();
                if (_playerTransform == null || _playerDamagable == null)
                    return;

                _damageData = new DamageData(_damage, _playerDamagable);
            }

            bool canAttack = _mobMovement.IsInAttackRange(_playerTransform, _attackRange) && HasClearAttackPath();
            if (!_isAttack && canAttack)
                StartAttack();
            if (_isAttack && !canAttack)
                StopAttack();

            if (_isAttack)
            {
                FollowTheTarget();
            }
        }

        private void StartAttack()
        {
            _isAttack = true;
            _mobMovement.Stop();
            _attackRoutine = StartCoroutine(Attack());
        }

        private void StopAttack()
        {
            StopAttackInternal(true);
        }

        public void CancelAttack()
        {
            StopAttackInternal(false);
        }

        public void HandleDeath()
        {
            if (_isDead)
                return;

            _isDead = true;
            StopAttackInternal(false);
        }

        private void StopAttackInternal(bool resumeMovement)
        {
            _isAttack = false;
            GetComponent<MobFeedback>()?.CancelFuse();

            if (_attackRoutine != null)
            {
                StopCoroutine(_attackRoutine);
                _attackRoutine = null;
            }

            if (_hitFallbackRoutine != null)
            {
                StopCoroutine(_hitFallbackRoutine);
                _hitFallbackRoutine = null;
            }

            if (resumeMovement && !_isDead)
                _mobMovement.Move();
        }

        private IEnumerator Attack()
        {
            while (_isAttack)
            {
                float wait = _nextAttackTime - Time.time;
                if (wait > 0f)
                {
                    yield return new WaitForSeconds(wait);
                    if (!_isAttack) yield break;
                }

                _mobAnimator?.PlayAttack();


                if (_attackType == MobAttackType.Explosion)
                {
                    yield return RunExplosionAttack();
                    yield break;
                }

                _nextAttackTime = Time.time + _attackDelay;

                _attackHitApplied = false;
                _attackCycleId++;
                if (_hitFallbackRoutine != null)
                    StopCoroutine(_hitFallbackRoutine);
                _hitFallbackRoutine = StartCoroutine(AttackHitFallback(_attackCycleId));

                // подождём кадр, чтобы не зациклить триггер моментально
                yield return null;
            }
        }

        private IEnumerator RunExplosionAttack()
        {
            GetComponent<MobFeedback>()?.Fuse(_explosionDelay);
            _attackHitApplied = false;

            if (_explosionDelay > 0f)
                yield return new WaitForSeconds(_explosionDelay);

            if (!_isAttack || _isDead || _isAfk)
                yield break;

            if (_mobMovement == null || !_mobMovement.IsInAttackRange(_playerTransform, _attackRange))
                yield break;

            ApplyAttackHit();
        }

        private void OnAttackKeyframe()
        {
            if (!_isAttack || _attackHitApplied || _attackType == MobAttackType.Explosion)
                return;

            ApplyAttackHit();
        }

        private IEnumerator AttackHitFallback(int cycleId)
        {
            if (_attackHitFallbackDelay > 0f)
                yield return new WaitForSeconds(_attackHitFallbackDelay);

            if (!_isAttack || cycleId != _attackCycleId || _attackHitApplied)
                yield break;

            if (_mobMovement == null || !_mobMovement.IsInAttackRange(_playerTransform, _attackRange))
                yield break;

            ApplyAttackHit();
        }

        private void ApplyAttackHit()
        {
            if (_isAfk || _isDead || !_isAttack || _attackHitApplied || _playerTransform == null ||
                _mobMovement == null || !_mobMovement.IsInAttackRange(_playerTransform, _attackRange) || !HasClearAttackPath())
                return;

            _attackHitApplied = true;

            if (_attackType == MobAttackType.Range)
            {
                if (_projectilePrefab == null || _firePoint == null)
                {
                    Debug.LogError($"Ranged mob {name} has no projectile or fire point.", this);
                    return;
                }

                var rangeAttackData = new RangeAttackData(_damageData, _projectilePrefab, _playerTransform, _firePoint) { Owner = transform };
                GetComponent<MobFeedback>()?.Attack();
                _attackCommand.Execute(rangeAttackData);
                return;
            }

            if (_attackType == MobAttackType.Explosion)
            {
                var explosionData = new ExplosionAttackData(
                    transform.position,
                    _damage,
                    _explosionRadius > 0f ? _explosionRadius : _attackRange,
                    _explosionTargetMask,
                    gameObject);

                GetComponent<MobFeedback>()?.Explode(_explosionRadius);
                _attackCommand.Execute(explosionData);
                return;
            }

            GetComponent<MobFeedback>()?.Attack();
            _damageData.SourcePosition = transform.position;
            _attackCommand.Execute(_damageData);
        }

        private void FollowTheTarget()
        {
            Vector3 direction = _playerTransform.position - transform.position;
            direction.y = 0;

            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion correctedRotation = _mobMovement != null
                    ? _mobMovement.GetAxisCorrectedLookRotation(direction)
                    : Quaternion.LookRotation(direction);

                transform.rotation = Quaternion.Slerp(transform.rotation, correctedRotation, _rotationSpeed * Time.deltaTime);
            }
        }

        public void SetParameters(MobPreset preset)
        {
            StopAttackInternal(false);
            _nextAttackTime = Time.time;
            _attackHitApplied = false;
            _mobMovement = GetComponent<MobMovement>();
            _isDead = false;
            _mobType = preset.MobType;
            _damage = preset.Damage;
            if (TutorialService.Expedition) _damage = Mathf.Min(_damage, 3f);
            _attackDelay = Mathf.Max(0.1f, preset.AttackDelay);
            _attackRange = preset.AttackRange;
            _rotationSpeed = preset.RotationSpeed;
            _projectilePrefab = preset.Projectile;
            _explosionDelay = preset.ExplosionDelay > 0f ? preset.ExplosionDelay : preset.AttackDelay;
            _explosionRadius = preset.ExplosionRadius > 0f ? preset.ExplosionRadius : preset.AttackRange;
            _explosionTargetMask = preset.ExplosionTargetMask.value == 0
                ? (LayerMask)Physics.AllLayers
                : preset.ExplosionTargetMask;
            _attackType = ResolveAttackType(preset);
            UpdateAttackCommand();

            TryResolvePlayer();
            _damageData = new DamageData(_damage, _playerDamagable);
        }

        private bool HasClearAttackPath()
        {
            if (_playerTransform == null) return false;
            Vector3 origin = _attackType == MobAttackType.Range && _firePoint != null
                ? _firePoint.position : CombatTargeting.AimPoint(transform);
            return CombatTargeting.HasLineOfSight(origin, CombatTargeting.AimPoint(_playerTransform), transform, _playerTransform);
        }

        private static MobAttackType ResolveAttackType(MobPreset preset)
        {
            if (preset.AttackType != MobAttackType.Melee)
                return preset.AttackType;

            return preset.IsRangeAttack ? MobAttackType.Range : MobAttackType.Melee;
        }

        private void UpdateAttackCommand()
        {
            bool matches = _attackType == MobAttackType.Range ? _attackCommand is RangeAttackCommand :
                _attackType == MobAttackType.Explosion ? _attackCommand is ExplosionAttackCommand : _attackCommand is MeleeAttackCommand;
            if (matches) return;
            if (_attackCommand is UnityEngine.Object oldCommand) Destroy(oldCommand);
            switch (_attackType)
            {
                case MobAttackType.Range:
                    if (_attackCommand is not RangeAttackCommand)
                        _attackCommand = ScriptableObject.CreateInstance<RangeAttackCommand>();
                    break;
                case MobAttackType.Explosion:
                    if (_attackCommand is not ExplosionAttackCommand)
                        _attackCommand = ScriptableObject.CreateInstance<ExplosionAttackCommand>();
                    break;
                default:
                    if (_attackCommand is not MeleeAttackCommand)
                        _attackCommand = ScriptableObject.CreateInstance<MeleeAttackCommand>();
                    break;
            }
        }

        private void OnDestroy()
        {
            if (_attackCommand is UnityEngine.Object command) Destroy(command);
        }

        private void TryResolvePlayer()
        {
            if (Player.Instance == null)
                return;

            _playerTransform = Player.Instance.transform;
            _playerDamagable = Player.Instance.GetComponent<IDamageable>();
        }

        private void HandlePlayerRevived(Transform playerTransform)
        {
            if (_isDead) return;
            _isAfk = false;
        }

        private void HandlePlayerDied(Transform playerTransform)
        {
            _isAfk = true;
            CancelAttack();
        }
    }
}
