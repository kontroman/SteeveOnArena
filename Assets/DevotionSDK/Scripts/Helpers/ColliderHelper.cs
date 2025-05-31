using MineArena.Basics;
using UnityEngine;

namespace Devotion.SDK.Helpers
{
    public static class ColliderHelper
    {
        public static bool IsPlayer(this Collider collider)
        {
            return collider.CompareTag(Constants.GameTags.Player);
        }
    }
}