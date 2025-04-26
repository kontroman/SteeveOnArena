using System;
using UnityEngine;

namespace Devotion.Items
{
    [Serializable]
    public class Item
    {
        public string Name { get; }
        public GameObject Prefab { get; }
        public Sprite Icon { get; }

        public Item(string name, GameObject prefab, Sprite icon)
        {
            Name = name;
            Prefab = prefab;
            Icon = icon;
        }
    }
}
