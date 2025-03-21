using UnityEngine;

namespace Devotion.Items
{
    public class ItemInteractor : MonoBehaviour
    {
        [SerializeField] private ItemConfig _item;

        private Item item;

        private void Awake()
        {
            if (_item.Stackable)
            {
                item = new StackableItem(_item as StackableItemConfig, 1);
            }
            else
            {
                item = new EquipmentItem(_item as EquipmentItemConfig);
            }
            
        }

        public void Interact()
        {
            if (_item.Usable)
            {
                _item.Command.Execute(() => { Destroy(gameObject); });
            }
            else
            {
                AddToInventory(item);
            }
        }

        private void AddToInventory(Item item)
        {
            InventoryManager.Instance.AddItem(item);

            Destroy(this.gameObject);
        }
    }
}
