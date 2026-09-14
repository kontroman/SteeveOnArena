using MineArena.Interfaces;

namespace MineArena.Structs
{
    public struct DamageData
    {
        public UnityEngine.Vector3? SourcePosition;
        public float Damage;
        public IDamageable Target;

        public DamageData(float damage, IDamageable target, UnityEngine.Vector3? sourcePosition = null)
        {
            SourcePosition = sourcePosition;
            Damage = damage;
            Target = target;
        }
    }
}