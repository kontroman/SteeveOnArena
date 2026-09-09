using System;
using Devotion.SDK.Base;
using Devotion.SDK.Controllers;
using MineArena.Items;
using MineArena.Managers;
using MineArena.UI;
using TMPro;
using UnityEngine;

namespace MineArena.Windows
{
    public sealed class ShopWindow : BaseWindow
    {
        [SerializeField] private ShopCatalog catalog;
        [SerializeField] private Transform offersRoot;
        [SerializeField] private ShopOfferView offerPrefab;
        [SerializeField] private TMP_Text balance;
        [SerializeField] private TMP_Text feedback;
        private InventoryManager _inventory;
        private bool _buying;
        private MineArena.Platform.YandexPurchases _purchases;

        private void OnEnable()
        {
            _inventory = GameRoot.GetManager<InventoryManager>();
            if (MineArena.Platform.YandexPlatform.IsWebPlatform)
            {
                _purchases = MineArena.Platform.YandexPlatform.Instance.GetComponent<MineArena.Platform.YandexPurchases>();
                if (_purchases != null) { _purchases.Changed += Refresh; _purchases.Restore(); }
            }
            if (_inventory != null) _inventory.InventoryUpdated += Refresh;
            feedback.text = "Покупки сразу попадают в инвентарь";
            Refresh();
        }
        private void OnDisable()
        {
            if (_inventory != null) _inventory.InventoryUpdated -= Refresh;
            if (_purchases != null) _purchases.Changed -= Refresh;
        }
        public override void CloseWindow() => GameRoot.UIManager.CloseWindow<ShopWindow>();

        public void Refresh()
        {
            foreach (Transform t in offersRoot) { t.gameObject.SetActive(false); Destroy(t.gameObject); }
            if (catalog == null || catalog.Currency == null)
            { balance.text = "Магазин готовится к открытию"; return; }
            int funds = _inventory != null ? _inventory.GetItemAmount(catalog.Currency.Name) : 0;
            balance.text = catalog.Currency.DisplayName + ": " + funds;
            foreach (var offer in catalog.Offers)
            {
                if (offer == null || !offer.IsValid || offer.Item == catalog.Currency) continue;
                bool owned = !(offer.Item is StackableItemConfig) && _inventory != null && _inventory.GetItemAmount(offer.Item.Name) > 0;
                Instantiate(offerPrefab, offersRoot).Bind(offer, catalog.Currency.DisplayName, owned,
                    _inventory != null && funds >= offer.Price, () => Buy(offer));
            }
            if (_purchases == null || _purchases.Catalog == null) return;
            if (!string.IsNullOrEmpty(_purchases.Status)) feedback.text = _purchases.Status;
            foreach (var product in _purchases.Catalog.Products)
            {
                if (product.SkinIds != null && product.SkinIds.Count > 0) continue;
                if (product.Item == null || product.Amount <= 0) continue;
                var store = Array.Find(_purchases.Store, p => p.id == product.ProductId);
                if (store == null) continue;
                var view = Instantiate(offerPrefab, offersRoot);
                view.Bind(new ShopOffer { Item = product.Item, Amount = product.Amount }, "", false, !_purchases.Busy,
                    () => _purchases.Buy(product.ProductId));
                view.SetPlatformPrice(store.price + " " + store.priceCurrencyCode, !_purchases.Busy);
            }
            if (_purchases.Catalog.Products.Count > 0) AddRestorePurchasesButton();
        }
        private void AddRestorePurchasesButton()
        {
            var go = new GameObject("RestorePurchases", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            go.transform.SetParent(offersRoot, false);
            go.GetComponent<UnityEngine.UI.Image>().color = new Color32(211, 188, 141, 255);
            var button = go.GetComponent<UnityEngine.UI.Button>();
            button.interactable = !_purchases.Busy;
            button.onClick.AddListener(_purchases.Restore);
            var textObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(go.transform, false);
            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.text = "Восстановить покупки";
            text.fontSize = 20;
            text.color = new Color32(81, 71, 55, 255);
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(10, 10); text.rectTransform.offsetMax = new Vector2(-10, -10);
        }
        private void Buy(ShopOffer offer)
        {
            if (_buying || _inventory == null || catalog == null || !catalog.Offers.Contains(offer)) return;
            _buying = true;
            try
            {
                bool success = offer.IsValid && _inventory.TryExchange(catalog.Currency, offer.Price, offer.Item, offer.Amount);
                feedback.text = success ? "Куплено: " + offer.Item.DisplayName + " × " + offer.Amount : "Покупка недоступна: проверьте баланс и инвентарь";
                Refresh();
            }
            finally { _buying = false; }
        }
    }
}
