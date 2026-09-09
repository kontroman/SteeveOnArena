using System;
using System.Collections.Generic;
using MineArena.Items;
using UnityEngine;

namespace MineArena.Platform
{
    [CreateAssetMenu(menuName = "MineArena/Yandex Purchase Catalog")]
    public sealed class YandexPurchaseCatalog : ScriptableObject
    {
        public List<YandexProduct> Products = new List<YandexProduct>();
    }
    [Serializable] public sealed class YandexProduct
    {
        public string ProductId;
        public StackableItemConfig Item;
        public List<string> SkinIds = new List<string>();
        [Min(1)] public int Amount = 1;
        public bool IsValid
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ProductId)) return false;
                bool skins = SkinIds != null && SkinIds.Count > 0;
                if (Item != null && Amount <= 0) return false;
                if (skins)
                {
                    var catalog = MineArena.Cosmetics.SkinCatalog.Load();
                    if (catalog == null) return false;
                    foreach (var id in SkinIds)
                    {
                        var skin = catalog.Find(id);
                        if (skin == null || (skin.Source != MineArena.Cosmetics.SkinSource.IAP && skin.Source != MineArena.Cosmetics.SkinSource.Offer) || skin.ProductId != ProductId) return false;
                    }
                }
                return Item != null || skins;
            }
        }
    }
}
