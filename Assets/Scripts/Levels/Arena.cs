using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MineArena.Levels
{
    public class Arena : MonoBehaviour
    {
        [SerializeField] private Transform _playerSpawnPosition;
        [SerializeField] private List<Transform> _oreSpawnPoints;

        public Transform PlayerSpawnPosition { get { return _playerSpawnPosition; } }

        //TODO: add particles to fire
    }
}