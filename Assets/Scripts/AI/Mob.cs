using UnityEngine;
using Devotion.ObjectPools;
using Devotion.Controllers;
using Devotion.Interfaces;
using Devotion.Structs;
using Divotion.Game.Health;

namespace Devotion.AI
{ 
    public class Mob : MonoBehaviour, IDamageable
    {
        private MobMovement _mobMovement;
        private MobCombat _mobCombat;
        private Transform _playerTransform;
        private Health _mobHealth;

        public void Start()
        {
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
