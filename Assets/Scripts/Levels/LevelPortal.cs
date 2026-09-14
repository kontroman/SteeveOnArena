using System;
using System.Collections.Generic;
using Devotion.SDK.Helpers;
using UnityEngine;

namespace MineArena.Levels
{
    public class LevelPortal : MonoBehaviour
    {
        public event Action Entered;

        private bool _entered;
        private readonly HashSet<Collider> _occupants = new HashSet<Collider>();

        private void OnTriggerEnter(Collider other) => TryEnter(other);
        private void OnTriggerExit(Collider other) => Exit(other);

        public void Exit(Collider other)
        {
            _occupants.Remove(other);
            if (_occupants.Count == 0) _entered = false;
        }

        public void TryEnter(Collider other)
        {
            if (!other.IsPlayer() || MineArena.PlayerSystem.PlayerMovement.IsPlayerDead)
                return;

            _occupants.Add(other);
            if (_entered) return;
            _entered = true;
            Entered?.Invoke();
        }
    }
}
