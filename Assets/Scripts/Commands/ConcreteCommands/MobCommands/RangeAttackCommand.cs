using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using MineArena.Structs;
using MineArena.Controllers;
using System.Runtime.Serialization;
using MineArena.ObjectPools;
using System;

namespace MineArena.Commands
{
    //TODO: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ ObjectPool
    public class RangeAttackCommand : BaseCommand
    {
        private Transform firePoint;
        private GameObject projectilePrefab;
        private Transform owner;
        public override Task Execute(object data)
        {
            if (data is RangeAttackData rangeAttackData)
            {
                firePoint = rangeAttackData.FirePoint;
                projectilePrefab  = rangeAttackData.ProjectilePrefab;
                owner = rangeAttackData.Owner;
                
                SpawnProjectile(rangeAttackData.Target, rangeAttackData.DamageData);
            }

            return Task.CompletedTask;
        }

        private void SpawnProjectile(Transform target, DamageData damageData)
        {
            if (projectilePrefab == null || firePoint == null) return;

            GameObject projectile = GetProjectileFromPool();
            if (projectile == null) return;
            projectile.transform.position = firePoint.position;

            // пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ
            Projectile projectileScript = projectile.GetComponent<Projectile>();
            if (projectileScript != null)
            {
                projectileScript.SetParameters(target, damageData, owner);
            }
        }

        private GameObject GetProjectileFromPool()
        {
            Projectile projectileComponent = projectilePrefab.GetComponent<Projectile>();
            if (projectileComponent == null)
            {
                Debug.LogError($"Projectile prefab {projectilePrefab.name} does not contain a Projectile component.");
                return null;
            }

            Type projectileType = projectileComponent.GetType();
            return ObjectPoolsManager.Instance != null && ObjectPoolsManager.Instance.HasPool(projectileType)
                ? ObjectPoolsManager.Instance.Get(projectileType)
                : Instantiate(projectilePrefab);
        }
    }
}
