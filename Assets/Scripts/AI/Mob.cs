using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.AI;
using Devotion.ObjectPools;
using Devotion.Controllers;

namespace Devotion.AI
{ 
    public class Mob : MonoBehaviour
    {
        private MobMovement _mobMovement;
        private MobCombat _mobCombat;
        private Transform _playerTransform;
        private ObjectPool _pool;

        void Start()
        {
            _playerTransform = Player.Instance.GetComponentFromList<Transform>();
            _mobMovement = GetComponent<MobMovement>();
            _mobCombat = GetComponent<MobCombat>();
            //_mobMovement.SetPlayerTransform(_playerTransform);
        }

        public void Kill()
        {
            _pool.Release(gameObject);
        }
    }
}
