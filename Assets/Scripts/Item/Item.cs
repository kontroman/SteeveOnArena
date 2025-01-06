using UnityEngine;

namespace Devotion.Items
{
    public abstract class Item
    {
        public string Name { get; }
        public GameObject Prefab { get; }
        public Sprite Icon { get; }

        protected Item(string name, GameObject prefab, Sprite icon)
        {
            Name = name;
            Prefab = prefab;
            Icon = icon;
        }
    }
}
