using MineArena.Interfaces;
using MineArena.Structs;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace MineArena.Commands
{
    public class ExplosionAttackCommand : BaseCommand
    {
        public override Task Execute(object data)
        {
            if (data is not ExplosionAttackData explosionData)
                return Task.CompletedTask;

            int targetMask = explosionData.TargetMask.value == 0
                ? Physics.AllLayers
                : explosionData.TargetMask.value;

            var hits = Physics.OverlapSphere(explosionData.Position, explosionData.Radius, targetMask);
            var damagedTargets = new HashSet<IDamageable>();

            foreach (var hit in hits)
            {
                if (hit == null)
                    continue;

                if (explosionData.Owner != null && hit.transform.root.gameObject == explosionData.Owner)
                    continue;

                var damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable == null || !damagedTargets.Add(damageable))
                    continue;

                damageable.TakeDamage(new DamageData(explosionData.Damage, damageable));
            }

            var selfDamageable = explosionData.Owner != null
                ? explosionData.Owner.GetComponent<IDamageable>()
                : null;

            selfDamageable?.TakeDamage(new DamageData(float.MaxValue, selfDamageable));

            return Task.CompletedTask;
        }
    }
}
