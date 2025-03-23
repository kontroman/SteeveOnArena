using UnityEngine;
using Devotion.ObjectPools;
using Devotion.Controllers;

namespace Devotion.AI
{ 
    public class Mob : MonoBehaviour
    {
        private MobMovement _mobMovement;
        private MobCombat _mobCombat;
        private Transform _playerTransform;

        public void Start()
        {
            _playerTransform = Player.Instance.GetComponentFromList<Transform>();
            _mobMovement = GetComponent<MobMovement>();
            _mobCombat = GetComponent<MobCombat>();
        }

        public void Kill()
        {
            ObjectPoolsManager.Instance.Release(gameObject);
        }
    }
}
