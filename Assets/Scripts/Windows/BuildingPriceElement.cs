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

        public void Setup(ResourceRequired config)
        {
            _amountText.gameObject.SetActive(true);
            _resourceIcon.SetAlpha(1);

            if (config.Resource.BlockStyleIcon)
            {
                ResourceIcon icon = Instantiate(_iconPrefab, transform);
                icon.SetResource(config.Resource);
                icon.transform.localPosition = Vector3.zero;
                _resourceIcon.SetAlpha(0);
                _amountText.text = config.Amount.ToString();
                return;
            }

            _resourceIcon.sprite = config.Resource.Icon;
            _amountText.text = config.Amount.ToString();
        }

        public void Setup(ItemConfig item)
        {
            _resourceIcon.sprite = item.Icon;
            _amountText.gameObject.SetActive(false);
        }
    }
}
