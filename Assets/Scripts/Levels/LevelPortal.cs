using System;
using Devotion.SDK.Helpers;
using UnityEngine;

namespace MineArena.Levels
{
    public class LevelPortal : MonoBehaviour
    {
        public event Action Entered;

        private bool _entered;

        private void OnTriggerEnter(Collider other)
        {
            if (_entered || !other.IsPlayer())
                return;

            _entered = true;
            Entered?.Invoke();
        }
    }
}
