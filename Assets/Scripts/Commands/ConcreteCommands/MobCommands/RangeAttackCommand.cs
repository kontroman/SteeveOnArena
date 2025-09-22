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
    //TODO: реализовать генерацию снарядов через ObjectPool
    public class RangeAttackCommand : BaseCommand
    {
        private Transform firePoint;
        private GameObject projectilePrefab;
        public override Task Execute(object data)
        {
            var damageCommand = ScriptableObject.CreateInstance<DamageCommand>();

            if (data is RangeAttackData rangeAttackData)
            {
                firePoint = rangeAttackData.FirePoint;
                projectilePrefab  = rangeAttackData.ProjectilePrefab;
                
                if(rangeAttackData.DamageData is DamageData damageData) 
                    SpawnProjectile(Player.Instance.transform, damageData);
            }

            return Task.CompletedTask;
        }

        private void SpawnProjectile(Transform target, DamageData damageData)
        {
            if (projectilePrefab == null || firePoint == null) return;

            //GameObject projectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
            GameObject projectile = ObjectPoolsManager.Instance.Get<Arrow, Projectile>();
           // projectile.transform.position = firePoint.position;

            // Передаём цель или направление в снаряд
            Projectile projectileScript = projectile.GetComponent<Projectile>();
            if (projectileScript != null)
            {
                projectileScript.SetParameters(target, damageData);
            }
        }
    }
}
