using UnityEngine;

namespace MineArena.Structs
{
    public struct ExplosionAttackData
    {
        public Vector3 Position;
        public float Damage;
        public float Radius;
        public LayerMask TargetMask;
        public GameObject Owner;

        public ExplosionAttackData(Vector3 position, float damage, float radius, LayerMask targetMask, GameObject owner)
        {
            Position = position;
            Damage = damage;
            Radius = radius;
            TargetMask = targetMask;
            Owner = owner;
        }
    }
}
