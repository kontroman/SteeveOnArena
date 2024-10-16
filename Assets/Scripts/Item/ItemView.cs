using Devotion.Item;
using UnityEngine;
using UnityEngine.UI;
using Devotion.Controllers;

namespace Devotion.Item
{
    public class ItemView : MonoBehaviour
    {
        [SerializeField] private Item _item;
        [SerializeField] private GameObject _prefab;

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.TryGetComponent(out Inventory inventoryPlayer))
            {
                if (_item.IsAddInventory == false)
                    ActivateInstantly();
                else
                    AddInventory(inventoryPlayer);

            }
        }

        private void ActivateInstantly() => _item.Activation();

        public void AddInventory(Inventory inventoryPlayer)
        {
            inventoryPlayer.GetItem(_item);
            gameObject.SetActive(false); // late add( create)  pool items 
        }
    }
}
