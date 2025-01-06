using Devotion.Controllers;
using UnityEngine;

namespace Devotion.Items
{
    public class ItemInteractor : MonoBehaviour
    {
        [SerializeField] private ItemConfig _item;

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.TryGetComponent(out Inventory inventoryPlayer))
            {
                if (_item.Usable)
                {
                    _item.Command.Execute(() => { Destroy(gameObject); });
                }
                else
                {
                    AddInventory(inventoryPlayer);
                }
            }
        }

        private void AddInventory(Inventory inventoryPlayer)
        {
            //TODO: add to inventory

            Destroy(this.gameObject);
        }
    }
}
