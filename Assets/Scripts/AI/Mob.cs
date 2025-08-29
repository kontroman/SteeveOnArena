using UnityEngine;
using MineArena.ObjectPools;
using MineArena.Controllers;
using MineArena.Interfaces;
using MineArena.Structs;
using MineArena.Game.Health;

namespace MineArena.AI
{
    public class Mob : MonoBehaviour
    {
        private Transform _playerTransform;

        [SerializeField] private MobCombat _mobCombat;
        [SerializeField] private MobMovement _mobMovement;

        public void Start()
        {
            _playerTransform = Player.Instance.GetComponentFromList<Transform>();
        }
    }
}
