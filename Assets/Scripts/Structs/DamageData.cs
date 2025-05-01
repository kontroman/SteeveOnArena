using MineArena.Interfaces;

namespace MineArena.Structs
{
    public struct DamageData
    {
        public float Damage;
        public IDamageable Target;

        public DamageData(float damage, IDamageable target)
        {
            Damage = damage;
            Target = target;
        }
    }
}