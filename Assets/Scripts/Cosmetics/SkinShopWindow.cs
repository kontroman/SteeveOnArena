using System;
using System.Linq;
using Devotion.SDK.Base;
using Devotion.SDK.Controllers;
using MineArena.Managers;
using MineArena.Platform;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MineArena.Cosmetics
{
    public sealed class SkinShopWindow : BaseWindow
    {
        [SerializeField] private Transform cards;
        [SerializeField] private Button cardPrefab;
        [SerializeField] private Image preview;
        [SerializeField] private TMP_Text skinName;
        [SerializeField] private TMP_Text description;
        [SerializeField] private TMP_Text balance;
        [SerializeField] private TMP_Text actionLabel;
        [SerializeField] private Button action;
        [SerializeField] private Button close;
        [SerializeField] private Button restore;
        private SkinCatalog catalog;
        private CharacterSkin selected;
        private InventoryManager inventory;
        private YandexPurchases purchases;

        private void Awake()
        {
            close.onClick.AddListener(CloseWindow);
            action.onClick.AddListener(Act);
            restore.onClick.AddListener(() => purchases?.Restore());
        }
        private void OnEnable()
        {
            catalog = SkinCatalog.Load();
            inventory = GameRoot.GetManager<InventoryManager>();
            if (YandexPlatform.IsWebPlatform)
                purchases = YandexPlatform.Instance.GetComponent<YandexPurchases>();
            if (purchases != null) { purchases.Changed += Refresh; purchases.Restore(); }
            if (inventory != null) inventory.InventoryUpdated += Refresh;
            SkinService.Changed += Refresh;
            selected = catalog?.Find(SkinService.Progress?.Equipped ?? "default") ?? catalog?.Skins.FirstOrDefault();
            Refresh();
        }
        private void OnDisable()
        {
            if (inventory != null) inventory.InventoryUpdated -= Refresh;
            if (purchases != null) purchases.Changed -= Refresh;
            SkinService.Changed -= Refresh;
        }
        public override void CloseWindow() => GameRoot.UIManager.CloseWindow<SkinShopWindow>();
        private void Update() { if (Input.GetKeyDown(KeyCode.Escape)) CloseWindow(); }

        public void Refresh()
        {
            for (int i = cards.childCount - 1; i >= 0; i--)
            {
                var child = cards.GetChild(i).gameObject; child.SetActive(false);
                if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
            }
            if (catalog == null || selected == null) return;
            foreach (var skin in catalog.Skins)
            {
                var card = Instantiate(cardPrefab, cards);
                card.gameObject.SetActive(true);
                card.GetComponentsInChildren<Image>().First(i => i.name == "Portrait").sprite = skin.Preview;
                card.GetComponentInChildren<TMP_Text>().text = skin.DisplayName + "\n" +
                    (SkinService.Progress?.Equipped == skin.Id ? "Надет" : SkinService.Owns(skin.Id) ? "Получен" : SourceLabel(skin.Source));
                card.onClick.AddListener(() => { selected = skin; Refresh(); });
                card.GetComponent<Image>().color = selected == skin ? new Color32(242, 204, 116, 255) : Color.white;
            }
            preview.sprite = selected.Preview;
            skinName.text = selected.DisplayName;
            description.text = "Только внешний вид · без бонусов к характеристикам\n\n" + selected.SourceDescription;
            balance.text = selected.Currency != null ? selected.Currency.DisplayName + ": " + (inventory?.GetItemAmount(selected.Currency.Name) ?? 0) : "Коллекция: " + catalog.Skins.Count(s => SkinService.Owns(s.Id)) + " / " + catalog.Skins.Count;
            restore.gameObject.SetActive(purchases != null);
            restore.interactable = purchases != null && !purchases.Busy;
            action.interactable = false;
            if (SkinService.Owns(selected.Id))
            {
                bool equipped = SkinService.Progress?.Equipped == selected.Id;
                actionLabel.text = equipped ? "Надет" : "Надеть";
                action.interactable = !equipped;
                return;
            }
            actionLabel.text = SourceLabel(selected.Source);
            switch (selected.Source)
            {
                case SkinSource.Currency:
                    actionLabel.text = "Купить · " + selected.Price + " " + selected.Currency?.DisplayName;
                    action.interactable = selected.Currency != null && selected.Price > 0 && inventory != null && inventory.GetItemAmount(selected.Currency.Name) >= selected.Price;
                    if (!action.interactable) description.text += "\nНе хватает валюты";
                    break;
                case SkinSource.Quest:
                    var quests = GameRoot.PlayerProgress?.AchievementProgress?.Achievements;
                    action.interactable = quests != null && quests.TryGetValue(selected.QuestId, out var quest) && quest.IsCompleted;
                    actionLabel.text = action.interactable ? "Получить награду" : "Награда за квест";
                    break;
                case SkinSource.IAP:
                case SkinSource.Offer:
                    var product = purchases?.Catalog?.Products.FirstOrDefault(p => p.ProductId == selected.ProductId && p.IsValid && p.SkinIds.Contains(selected.Id));
                    var store = product != null ? purchases.Store.FirstOrDefault(p => p.id == selected.ProductId) : null;
                    if (product?.Item != null)
                        description.text += "\nВ наборе: этот скин + " + product.Amount + " " + product.Item.DisplayName;
                    action.interactable = store != null && !purchases.Busy;
                    actionLabel.text = store == null ? "Скоро в продаже" : purchases.Busy ? "Подождите…" : store.price + " " + store.priceCurrencyCode;
                    if (!string.IsNullOrEmpty(purchases?.Status)) description.text += "\n" + purchases.Status;
                    break;
            }
        }
        private void Act()
        {
            if (selected == null) return;
            if (SkinService.Owns(selected.Id)) SkinService.Equip(selected.Id);
            else if (selected.Source == SkinSource.Currency) SkinService.Buy(selected.Id);
            else if (selected.Source == SkinSource.Quest) SkinService.ClaimQuest(selected.Id);
            else if (selected.Source == SkinSource.IAP || selected.Source == SkinSource.Offer) purchases?.Buy(selected.ProductId);
            Refresh();
        }
        public static string SourceLabel(SkinSource source) => source switch
        {
            SkinSource.Currency => "За валюту", SkinSource.Quest => "За квест", SkinSource.Reward => "Награда",
            SkinSource.IAP => "Покупка", SkinSource.Offer => "В оффере", _ => "Базовый"
        };
    }
}
