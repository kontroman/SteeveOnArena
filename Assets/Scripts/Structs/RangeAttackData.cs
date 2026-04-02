using MineArena.Controllers;
using MineArena.Interfaces;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MineArena.Structs
{
    public struct RangeAttackData
    {
        public DamageData DamageData;
        public GameObject ProjectilePrefab;
        public Transform Target;
        public Transform FirePoint;

        public RangeAttackData(DamageData damageData, GameObject projectilePrefab, Transform target, Transform firePoint)
        {
            DamageData = damageData;
            ProjectilePrefab = projectilePrefab;
            Target = target;
            FirePoint = firePoint;
        }

        public RangeAttackData(float damage, GameObject projectilePrefab, Transform target, Transform firePoint)
        {
            ProjectilePrefab = projectilePrefab;
            Target = target;
            FirePoint = firePoint;

            //TODO: пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ, пїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ-пїЅпїЅ пїЅпїЅ-пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
            IDamageable damageable = null;
            if (target != null)
                damageable = target.GetComponentInParent<IDamageable>();

            if (damageable == null && Player.Instance != null)
                damageable = Player.Instance.GetComponent<IDamageable>();

            DamageData = new DamageData(damage, damageable);
        }
    }
}
