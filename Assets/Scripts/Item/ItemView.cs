using Devotion.Controllers;
using UnityEngine;

namespace Devotion.Items
{
    public class ItemView : MonoBehaviour
    {
        [SerializeField] private Item _item;

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.TryGetComponent(out Inventory inventoryPlayer))
            {
                if (_item.IsAddInventory == false)
                {
                    _item.command.Execute(() => { Destroy(gameObject); });
                }
                else
                {
                    AddInventory(inventoryPlayer);
                }

            }
        }

        private void AddInventory(Inventory inventoryPlayer)
        {
            inventoryPlayer.AddItem(this);

            Destroy(gameObject);
        }
    }
}
