using MineArena.Commands;
using MineArena.Controllers;
using MineArena.Interfaces;
using MineArena.Structs;
using System.Collections;
using UnityEngine;

namespace MineArena.AI
{
    public class MobCombat : MonoBehaviour
    {
        [SerializeField] private float _attackRange = 2f;
        [SerializeField] private float _attackDelay = 1f;
        [SerializeField] private float _damage = 10;
        [SerializeField] private bool _isRanged = false;

        private bool _isAttack = false;
        private MobMovement _mobMovement;
        private ICommand _damageCommand;
        private IDamageable _playerDamagable;

        private void Start()
        {
            _mobMovement = GetComponent<MobMovement>();
            _damageCommand = ScriptableObject.CreateInstance<DamageCommand>();
            _playerDamagable = Player.Instance.GetComponent<IDamageable>();
        }

        private void Update()
        {
            if (!_isAttack && (_mobMovement.DistanceToPlayer() < _attackRange))
                StartAttack();
            if (_isAttack && _mobMovement.DistanceToPlayer() > _attackRange)
                StopAttack();
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
                _damageCommand.Execute(new DamageData(
                            _damage,
                            _playerDamagable
                        ));
                yield return new WaitForSeconds(_attackDelay);
            }
        }

        public void SetParameters(float damage, float attackSpeed, float attackRange)
        {
            _attackRange = attackRange;
            _attackDelay = attackSpeed;
            _damage = damage;
        }
    }
}
