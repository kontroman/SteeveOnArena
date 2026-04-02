using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using MineArena.Structs;
using MineArena.Controllers;
using System.Runtime.Serialization;
using MineArena.ObjectPools;

namespace MineArena.Commands
{
    //TODO: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ ObjectPool
    public class RangeAttackCommand : BaseCommand
    {
        private Transform firePoint;
        private GameObject projectilePrefab;
        public override Task Execute(object data)
        {
            if (data is RangeAttackData rangeAttackData)
            {
                firePoint = rangeAttackData.FirePoint;
                projectilePrefab  = rangeAttackData.ProjectilePrefab;
                
                SpawnProjectile(rangeAttackData.Target, rangeAttackData.DamageData);
            }

            return Task.CompletedTask;
        }

        private void SpawnProjectile(Transform target, DamageData damageData)
        {
            if (projectilePrefab == null || firePoint == null) return;

            GameObject projectile = ObjectPoolsManager.Instance.Get<Arrow, Projectile>();
            if (projectile == null) return;
            projectile.transform.position = firePoint.position;

            // пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ
            Projectile projectileScript = projectile.GetComponent<Projectile>();
            if (projectileScript != null)
            {
                projectileScript.SetParameters(target, damageData);
            }
        }
    }
}
