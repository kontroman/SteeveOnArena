using Devotion.AI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Devotion.AI
{
    public class MobCombat : MonoBehaviour
    {
        private float _attackSpeed = 1f;
        private float _attackRange = 2f;
        private float _damage;
        private bool _isAttack = false;
        private MobMovement _mobMovement;

        private void Start()
        {
            _mobMovement = GetComponent<MobMovement>();
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
            StartCoroutine("Attack");
        }

        private void StopAttack()
        {
            _isAttack = false;
            StopCoroutine("Attack");
        }

        private IEnumerator Attack()
        {
            while (_isAttack)
            {
                yield return new WaitForSeconds(_attackSpeed);
                //print("тычка " + temp);
            }
        }

        public void SetParameters(float damage, float attackSpeed, float attackRange)
        {
            _attackRange = attackRange;
            _attackSpeed = attackSpeed;
            _damage = damage;
        }
    }
}
