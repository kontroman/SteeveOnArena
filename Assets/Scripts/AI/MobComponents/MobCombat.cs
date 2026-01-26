using MineArena.Commands;
using MineArena.Controllers;
using MineArena.Interfaces;
using MineArena.Structs;
using System.Collections;
using UnityEngine;

namespace MineArena.AI
{
    public class MobCombat : MonoBehaviour, IMobComponent
    {
        [SerializeField] private float _attackRange = 2f;
        [SerializeField] private float _attackDelay = 1f;
        [SerializeField] private float _damage = 10f;
        [SerializeField] private float _rotationSpeed = 50f;
        [SerializeField] private bool _isRanged = false;
        [SerializeField] private GameObject _projectilePrefab;
        [SerializeField] private Transform _firePoint;

        private bool _isAttack;
        private MobMovement _mobMovement;
        private MobAnimationController _mobAnimator;
        private ICommand _damageCommand;
        private ICommand _attackCommand;
        private IDamageable _playerDamagable;
        private DamageData _damageData;
        private Transform _playerTransform;
        private Coroutine _attackRoutine;
        private float _nextAttackTime;

        private void OnEnable()
        {
            _mobAnimator = GetComponent<MobAnimationController>();

            if (_mobAnimator != null)
                _mobAnimator.AttackKeyframeReached += OnAttackKeyframe;
        }

        private void OnDisable()
        {
            if (_mobAnimator != null)
                _mobAnimator.AttackKeyframeReached -= OnAttackKeyframe;
        }

        private void Start()
        {
            _mobMovement = GetComponent<MobMovement>();
            _damageCommand = ScriptableObject.CreateInstance<DamageCommand>();
            _playerDamagable = Player.Instance.GetComponent<IDamageable>();
            _playerTransform = Player.Instance.transform;

            _damageData = new DamageData(_damage, _playerDamagable);

            if (_isRanged)
                _attackCommand = ScriptableObject.CreateInstance<RangeAttackCommand>();
            else
                _attackCommand = ScriptableObject.CreateInstance<MeleeAttackCommand>();

            _nextAttackTime = Time.time;
        }

        private void Update()
        {
            if (!_isAttack && _mobMovement.IsInAttackRange(_playerTransform, _attackRange))
                StartAttack();
            if (_isAttack && !_mobMovement.IsInAttackRange(_playerTransform, _attackRange))
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

        private void StopAttackInternal(bool resumeMovement)
        {
            _isAttack = false;

            if (_attackRoutine != null)
            {
                StopCoroutine(_attackRoutine);
                _attackRoutine = null;
            }

            if (resumeMovement)
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
                _nextAttackTime = Time.time + _attackDelay;

                // подождём кадр, чтобы не зациклить триггер моментально
                yield return null;
            }
        }

        private void OnAttackKeyframe()
        {
            if (!_isAttack)
                return;

            if (_isRanged)
            {
                var rangeAttackData = new RangeAttackData(_damageData.Damage, _projectilePrefab, _playerTransform, _firePoint);
                _attackCommand.Execute(rangeAttackData);
            }
            else
            {
                _attackCommand.Execute(_damageData);
            }
        }

        private void FollowTheTarget()
        {
            Vector3 direction = _playerTransform.position - transform.position;
            direction.y = 0;

            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
            }
        }

        public void SetParameters(MobPreset preset)
        {
            _damage = preset.Damage;
            _attackRange = preset.AttackRange;
            _attackDelay = preset.AttackDelay;
            _rotationSpeed = preset.RotationSpeed;
            _projectilePrefab = preset.Projectile;
            _damageData = new DamageData(_damage, _playerDamagable);
        }
    }
}
