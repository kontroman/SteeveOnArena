using System;
using MineArena.Windows.SelectLevel;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MineArena.UI
{
    public sealed class ShopOfferView : MonoBehaviour
    {
        [SerializeField] private LevelResourceChip item;
        [SerializeField] private TMP_Text price;
        [SerializeField] private TMP_Text status;
        [SerializeField] private Button buy;
        public void SetPlatformPrice(string value, bool available)
        {
            price.text = value;
            status.text = available ? "Купить" : "Подождите…";
            buy.interactable = available;
        }
        public void Bind(ShopOffer offer, string currency, bool owned, bool affordable, Action purchase)
        {
            item.Bind(offer.Item, offer.Amount);
            price.text = offer.Price + " × " + currency;
            status.text = owned ? "Уже есть" : affordable ? "Купить" : "Не хватает ресурсов";
            buy.interactable = !owned && affordable;
            buy.onClick.RemoveAllListeners();
            buy.onClick.AddListener(() => purchase());
        }
    }
}
