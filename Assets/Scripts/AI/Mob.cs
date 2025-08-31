using UnityEngine;
using MineArena.Controllers;

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

        public void MakeFreeze()
        {

        }

        public void MakeUnfreeze()
        {

        }
    }
}
