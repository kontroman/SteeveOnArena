using UnityEngine;
using MineArena.ObjectPools;
using MineArena.Controllers;
using MineArena.Interfaces;
using MineArena.Structs;
using MineArena.Game.Health;

namespace MineArena.AI
{ 
    public class Mob : MonoBehaviour, IDamageable
    {
        private MobMovement _mobMovement;
        private MobCombat _mobCombat;
        private Transform _playerTransform;
        private Health _mobHealth;

        public void Start()
        {
            //TODO: make it serializeField and remove GetComponent

            _playerTransform = Player.Instance.GetComponentFromList<Transform>();
            _mobMovement = GetComponent<MobMovement>();
            _mobCombat = GetComponent<MobCombat>();
            _mobHealth = GetComponent<Health>();
        }

        public void Kill()
        {
            ObjectPoolsManager.Instance.Release(gameObject);
        }

        public void TakeDamage(DamageData damageData)
        {
            _mobHealth.ChangeValue(-damageData.Damage);
        }
    }
}
