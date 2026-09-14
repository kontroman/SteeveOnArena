using Devotion.SDK.Extensions;
using MineArena.Buildings;
using MineArena.Items;
using MineArena.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MineArena.Windows.Elements
{
    public class BuildingPriceElement : MonoBehaviour
    {
        [SerializeField] private Image _resourceIcon;
        [SerializeField] private TextMeshProUGUI _amountText;
        [SerializeField] private ResourceIcon _iconPrefab;

        [SerializeField] private ResourceIcon _blockIcon;
        [SerializeField] private TMP_Text _nameText;

        private ResourceRequired _cost;
        public void Setup(ResourceRequired config)
        {
            Setup(config.Resource, config.Amount);
            _cost = config;
            ResourceSourceNavigation.Bind(gameObject, config.Resource, GetComponentInParent<Devotion.SDK.Base.BaseWindow>());
            RefreshCost();
        }
        public void RefreshCost()
        {
            if (_cost.Resource == null) return;
            int owned = Devotion.SDK.Controllers.GameRoot.GetManager<MineArena.Managers.InventoryManager>()?.GetItemAmount(_cost.Resource.Name) ?? 0;
            _amountText.text = $"Есть {owned} / нужно {_cost.Amount}";
            _amountText.enableAutoSizing = true;
            _amountText.fontSizeMin = 12;
            _amountText.fontSizeMax = 16;
            _amountText.color = owned >= _cost.Amount ? new Color(.16f, .40f, .31f) : new Color(.7f, .18f, .12f);
        }
        public void Setup(ItemConfig item, int amount = 0)
        {
            _cost = default;
            var sourceButton = GetComponent<Button>();
            if (sourceButton != null) { sourceButton.onClick.RemoveAllListeners(); sourceButton.interactable = false; }
            if (item == null) return;
            bool cube = item.BlockStyleIcon && item is StackableItemConfig;
            if (cube && _blockIcon == null && _iconPrefab != null)
                _blockIcon = Instantiate(_iconPrefab, _resourceIcon.transform.parent);
            if (_blockIcon != null)
            {
                _blockIcon.gameObject.SetActive(cube);
                if (cube) _blockIcon.SetResource((StackableItemConfig)item);
            }
            _resourceIcon.sprite = item.Icon;
            _resourceIcon.gameObject.SetActive(!cube || _blockIcon == null);
            _amountText.text = amount.ToString();
            _amountText.gameObject.SetActive(amount > 0);
            if (_nameText != null) _nameText.text = item.DisplayName;
        }
    }
}
