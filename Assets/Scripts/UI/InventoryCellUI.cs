using Devotion.Items;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Devotion.UI
{
    public class InventoryCellUI : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _counter;

        private Items.Item _item;

        public Items.Item Item => _item;
        public bool HasItem => _item != null;
        public Image Icon => _icon;

        private void Awake()
        {
            _icon.enabled = false;
            _counter.enabled = false;
        }

        public void Setup(Items.Item item)
        {
            _item = item;
            _icon.sprite = item.Icon;
            _icon.enabled = true;

            if (item is StackableItem stackableItem)
                _counter.text = stackableItem.CurrentStack.ToString();
            else
                _counter.text = string.Empty;

            _counter.enabled = true;
        }

        public void Clear()
        {
            _item = null;
            _icon.sprite = null;
            _icon.enabled = false;
            _counter.text = string.Empty;
            _counter.enabled = false;
        }
    }
}