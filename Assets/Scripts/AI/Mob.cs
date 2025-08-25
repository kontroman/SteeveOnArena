using UnityEngine;
using MineArena.ObjectPools;
using MineArena.Controllers;
using MineArena.Interfaces;
using MineArena.Structs;
using MineArena.Game.Health;
using System.Windows.Input;

namespace MineArena.AI
{ 
    public class Mob : MonoBehaviour
    {
        private MobMovement _mobMovement;
        private MobCombat _mobCombat;
        private Transform _playerTransform;

        public void Start()
        {
            //TODO: make it serializeField and remove GetComponent

            _playerTransform = Player.Instance.GetComponentFromList<Transform>();
            _mobMovement = GetComponent<MobMovement>();
            _mobCombat = GetComponent<MobCombat>();
        }
    }
}
