using System;
using System.Collections.Generic;
using MineArena.Items;
using UnityEngine;

namespace MineArena.UI
{
    [CreateAssetMenu(menuName = "MineArena/Shop Catalog")]
    public sealed class ShopCatalog : ScriptableObject
    {
        public ItemConfig Currency;
        public List<ShopOffer> Offers = new List<ShopOffer>();
    }

    [Serializable]
    public sealed class ShopOffer
    {
        public ItemConfig Item;
        [Min(1)] public int Amount = 1;
        [Min(1)] public int Price = 1;
        public bool IsValid => Item != null && Amount > 0 && Price > 0 && (Item is StackableItemConfig || Amount == 1);
    }
}
