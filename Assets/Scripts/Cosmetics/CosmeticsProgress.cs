using System;
using System.Collections.Generic;
using Devotion.SDK.Services.SaveSystem.Progress;
using UnityEngine;

namespace MineArena.Cosmetics
{
    [Serializable]
    public sealed class CosmeticsProgress : BaseProgress
    {
        [SerializeField] private List<string> owned = new();
        [SerializeField] private string equipped = "default";
        public string Equipped => Owns(equipped) ? equipped : "default";
        public bool Owns(string id) => id == "default" || (!string.IsNullOrEmpty(id) && owned != null && owned.Contains(id));
        // Caller commits once all parts of a reward/purchase have been applied.
        public bool Unlock(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || Owns(id)) return false;
            (owned ??= new List<string>()).Add(id);
            return true;
        }
        public bool Equip(string id)
        {
            if (!Owns(id) || Equipped == id) return false;
            equipped = id;
            return true;
        }
    }
}
