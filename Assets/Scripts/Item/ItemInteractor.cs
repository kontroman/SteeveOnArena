using Devotion.SDK.Controllers;
using MineArena.Controllers;
using MineArena.Managers;
using UnityEngine;

namespace MineArena.Items
{
    public class ItemInteractor : MonoBehaviour
    {
        [SerializeField] private ItemConfig _item;

        private Item item;

        public ItemConfig ItemConfig => _item;

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
            Messages.AchievementMessages.AchievementTargetTaken.Publish((_item, 1)); // test

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
            int amount = 1;
            if (item is StackableItem stackable)
            {
                amount = Mathf.Max(1, stackable.CurrentStack);
            }

            GameRoot.GetManager<InventoryManager>().AddItem(item, amount);

            LevelController.Current?.RegisterCollectedResource(_item, amount);

            Destroy(this.gameObject);
        }
    }
}
