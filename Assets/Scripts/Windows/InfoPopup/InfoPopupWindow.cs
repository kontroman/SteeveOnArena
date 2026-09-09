using Devotion.SDK.Base;
using Devotion.SDK.Controllers;
using Devotion.SDK.Services.Localization;
using MineArena.Basics;
using MineArena.UI.FortuneWheel;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MineArena.Windows.InfoPopup
{
    public class InfoPopupWindow : BaseWindow
    {
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _descriptionText;

        [SerializeField] private Image _iconImage;
        [SerializeField] private MineArena.UI.ResourceIcon _resourceIconPrefab;
        private MineArena.UI.ResourceIcon _resourceIcon;

        [SerializeField] private Button _okButton;

        public void Setup(ItemPrize prize)
        {
            _titleText.text = LocalizationService.GetLocalizedText(Constants.UIKeys.PrizeKey);

            _iconImage.sprite = prize.Icon;
            bool useBlock = prize.ItemConfig is MineArena.Items.StackableItemConfig && prize.ItemConfig.BlockStyleIcon;
            if (useBlock && _resourceIcon == null)
            {
                var prefab = _resourceIconPrefab;
                if (prefab != null)
                {
                    _resourceIcon = Instantiate(prefab, _iconImage.transform, false);
                    var rect = (RectTransform)_resourceIcon.transform;
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.offsetMin = rect.offsetMax = Vector2.zero;
                    rect.localRotation = Quaternion.identity;
                    rect.localScale = Vector3.one;
                }
            }
            if (_resourceIcon != null)
            {
                _resourceIcon.gameObject.SetActive(useBlock);
                if (useBlock) _resourceIcon.SetResource((MineArena.Items.StackableItemConfig)prize.ItemConfig);
            }
            _iconImage.enabled = !useBlock || _resourceIcon == null;

            _descriptionText.text = prize.Amount == 0 ? "" : "x" + prize.Amount;
        }

        private void Awake()
        {
            _okButton.onClick.AddListener(() => GameRoot.UIManager.CloseWindow<InfoPopupWindow>());
        }
    }
}
