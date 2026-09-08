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
        [Min(1)] public int Amount = 1;
    }
}
