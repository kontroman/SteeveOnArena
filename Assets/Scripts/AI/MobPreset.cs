using MineArena.Items;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using Achievements;
using UnityEngine;

namespace MineArena.AI
{
    [CreateAssetMenu(fileName = "New MobPreset", menuName = "MobPreset")]
    public class MobPreset : ScriptableObject, IAchievementTarget
    {
        [Header("Mian Settings")]
        public string name;
        public Sprite Icon;
        public MobTypes MobType;

        [Header("Combat Settings")]
        public MobAttackType AttackType;
        public float Damage;
        public float AttackDelay;
        [HideInInspector] public bool IsRangeAttack;
        public float AttackRange = 1;
        [ShowIf("@AttackType == MobAttackType.Range")] public GameObject Projectile;
        [ShowIf("@AttackType == MobAttackType.Explosion")] public float ExplosionDelay = 1f;
        [ShowIf("@AttackType == MobAttackType.Explosion")] public float ExplosionRadius = 2f;
        [ShowIf("@AttackType == MobAttackType.Explosion")] public LayerMask ExplosionTargetMask = ~0;

        [Header("Movement Settings")]
        public float Speed;
        public float RotationSpeed;

        [Header("Health Settings")]
        public float MaxHealth;

        //DOTO: �������� ������� ����� � ������ 
        /*[System.Serializable]
        public class DropEntry
        {
            public ItemConfig Item;

            [Range(0, 100)] public float DropChance;
            public int MinQuantity = 1;
            public int MaxQuantity = 1;
        }

        [Header("Drop Settings")]
        [Header("List drops")]
        [SerializeField] private List<DropEntry> _dropTable = new List<DropEntry>();

        [Header("Drop only one or more items")]
        [SerializeField] private bool _isOneDrop;*/
        public string Name { get; }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (AttackType == MobAttackType.Melee && IsRangeAttack)
                AttackType = MobAttackType.Range;

            IsRangeAttack = AttackType == MobAttackType.Range;
        }
#endif
    }
}
