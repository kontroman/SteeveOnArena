using System.Collections.Generic;
using UnityEngine;
using Devotion.Items;
using Devotion.Resourse;

namespace Devotion.Controllers
{
    public class Inventory : MonoBehaviour
    {
        private List<ItemView> items;

        private void Start()
        {
            items = new List<ItemView>();
        }

        public void AddItem(ItemView item)
        {
            items.Add(item);
        }

        public void AddResource(Resource resource, int amount)
        {
            Debug.Log($"Added {amount} of {resource.name} to inventory.");
        }
    }
}
