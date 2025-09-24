using MineArena.Commands;
using MineArena.Controllers;
using MineArena.Interfaces;
using MineArena.Structs;
using System.Collections;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Rendering;

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

        private bool _isAttack = false;
        private MobMovement _mobMovement;
        private ICommand _damageCommand;
        private ICommand _attackCommand;
        private IDamageable _playerDamagable;
        private DamageData _damageData;
        private Transform _playerTransform;

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
        }

        private void Update()
        {
            if (!_isAttack && (_mobMovement.DistanceToPlayer() < _attackRange))
                StartAttack();
            if (_isAttack && _mobMovement.DistanceToPlayer() > _attackRange)
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
            StartCoroutine("Attack");
        }

        private void StopAttack()
        {
            _isAttack = false;
            _mobMovement.Move();
            StopCoroutine("Attack");
        }

        private IEnumerator Attack()
        {
            yield return new WaitForSeconds(_attackDelay);

            while (_isAttack)
            {
                if (_isRanged)
                {
                    var rangeAttackData = new RangeAttackData(_damageData.Damage, _projectilePrefab, _playerTransform, _firePoint);
                    _attackCommand.Execute(rangeAttackData);
                }
                else
                    _attackCommand.Execute(_damageData);

                yield return new WaitForSeconds(_attackDelay);
            }
        }

        private void FollowTheTarget()
        {
            Vector3 direction = _playerTransform.position - transform.position;
            direction.y = 0; // Только горизонтальный поворот

            if (direction.sqrMagnitude > 0.01f) // Защита от ошибки при нулевом векторе
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
        }
    }
}
