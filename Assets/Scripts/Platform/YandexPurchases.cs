using System;
using System.Collections.Generic;
using System.Linq;
using Devotion.SDK.Controllers;
using Devotion.SDK.Services.SaveSystem;
using MineArena.Managers;
using UnityEngine;

namespace MineArena.Platform
{
    public sealed class YandexPurchases : MonoBehaviour
    {
        [Serializable] public sealed class StoreProduct { public string id, title, description, price, priceCurrencyCode; }
        [Serializable] private class StoreList { public StoreProduct[] items; }
        [Serializable] private class Purchase { public string productID, purchaseToken; }
        [Serializable] private class PurchaseList { public Purchase[] items; }
        public event Action Changed;
        public StoreProduct[] Store { get; private set; } = Array.Empty<StoreProduct>();
        public bool Busy { get; private set; }
        public string Status { get; private set; } = "";
        public YandexPurchaseCatalog Catalog { get; private set; }
        private YandexPlatform Platform => YandexPlatform.Instance;
        private void Awake() => Catalog = Resources.Load<YandexPurchaseCatalog>("UI/YandexPurchaseCatalog");
        public void Restore()
        {
            if (Busy || Catalog == null || Catalog.Products.Count == 0) return;
            Busy = true;
            Platform.Request("catalog").Then(json =>
            {
                Store = JsonUtility.FromJson<StoreList>(json).items ?? Array.Empty<StoreProduct>();
                Changed?.Invoke();
                Platform.Request("purchases").Then(raw =>
                    Process(new Queue<Purchase>(JsonUtility.FromJson<PurchaseList>(raw).items ?? Array.Empty<Purchase>())))
                    .Catch(Fail);
            }).Catch(Fail);
        }
        public void Buy(string productId)
        {
            if (Busy || !Store.Any(p => p.id == productId) || Find(productId) == null) return;
            Busy = true; Status = ""; Changed?.Invoke();
            Platform.Request("purchase", productId).Then(json =>
                Process(new Queue<Purchase>(new[] { JsonUtility.FromJson<Purchase>(json) }))).Catch(Fail);
        }
        private YandexProduct Find(string id) => Catalog?.Products.FirstOrDefault(p => p.ProductId == id && p.IsValid);
        private void Process(Queue<Purchase> purchases)
        {
            if (purchases.Count == 0) { Busy = false; Changed?.Invoke(); return; }
            var purchase = purchases.Dequeue();
            var product = Find(purchase.productID);
            // Unknown products must remain unconsumed for future recovery.
            if (product == null || string.IsNullOrEmpty(purchase.purchaseToken))
            { Process(purchases); return; }
            var ledger = GameRoot.PlayerProgress.PurchasesProgress;
            ledger.GrantedTokens ??= new List<string>();
            if (!ledger.GrantedTokens.Contains(purchase.purchaseToken))
            {
                var inventory = GameRoot.GetManager<InventoryManager>();
                int current = product.Item != null ? inventory.GetItemAmount(product.Item.Name) : 0;
                if (product.Item != null && (long)current + product.Amount > int.MaxValue) { Fail(new Exception("Inventory limit")); return; }
                ledger.GrantedTokens.Add(purchase.purchaseToken);
                if (product.SkinIds != null)
                    foreach (var id in product.SkinIds) GameRoot.PlayerProgress.CosmeticsProgress.Unlock(id);
                if (product.Item != null) inventory.AddItemById(product.Item.Name, product.Amount);
                MineArena.Cosmetics.SkinService.NotifyChanged();
            }
            // Never consume until BOTH reward and receipt are durably saved in the cloud.
            // Keep the receipt on failure: a retry must save it without granting twice.
            SaveService.Instance.Save().Then(() =>
                Platform.Request("consume", purchase.purchaseToken).Then(_ =>
                { Status = "Покупка получена"; Process(purchases); }).Catch(Fail)).Catch(Fail);
        }
        private void Fail(Exception error)
        {
            Busy = false;
            Status = "Покупка не завершена. Попробуй восстановить покупки.";
            Debug.LogWarning("[Yandex purchases] " + error.Message);
            Changed?.Invoke();
        }
    }
}
