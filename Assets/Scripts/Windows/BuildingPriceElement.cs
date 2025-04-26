using Devotion.Buildings;
using Devotion.Items;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Devotion.Windows.Elements
{
    public class BuildingPriceElement : MonoBehaviour
    {
        [SerializeField] private Image _resourceIcon;
        [SerializeField] private TextMeshProUGUI _amountText;

        public void Setup(ResourceRequired config)
        {
            _resourceIcon.sprite = config.Resource.Icon;
            _amountText.text = config.Amount.ToString();
        }

        public void Setup(EquipmentItemConfig item)
        {
            _resourceIcon.sprite = item.Icon;
            _amountText.gameObject.SetActive(false);
        }
    }
}