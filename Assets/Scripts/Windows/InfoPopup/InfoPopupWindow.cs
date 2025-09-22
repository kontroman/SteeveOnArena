using Devotion.SDK.Base;
using Devotion.SDK.Controllers;
using Devotion.SDK.Services;
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

        [SerializeField] private Button _okButton;

        public void Setup(ItemPrize prize)
        {
            var localizationService = ServiceLocator.Resolve<ILocalizationService>();

            _titleText.text = localizationService.GetLocalizedText(Constants.UIKeys.PrizeKey);

            _iconImage.sprite = prize.Icon;

            _descriptionText.text = prize.Amount == 0 ? "" : "x" + prize.Amount;
        }

        private void Awake()
        {
            _okButton.onClick.AddListener(() => GameRoot.UIManager.CloseWindow<InfoPopupWindow>());
        }
    }
}
