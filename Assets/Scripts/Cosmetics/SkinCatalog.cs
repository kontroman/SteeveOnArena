using System;
using System.Collections.Generic;
using System.Linq;
using MineArena.Buildings;
using MineArena.Items;
using UnityEngine;

namespace MineArena.Cosmetics
{
    public enum SkinSource { Default, Currency, Quest, Reward, IAP, Offer }

    [Serializable]
    public sealed class CharacterSkin
    {
        public string Id;
        public string DisplayName;
        public Texture2D Texture;
        public Sprite Preview;
        public SkinSource Source;
        public ItemConfig Currency;
        [Min(1)] public int Price = 1;
        public int QuestId = -1;
        public string SourceDescription;
        public string ProductId;
        public string RewardId;
    }

    [CreateAssetMenu(menuName = "MineArena/Character Skin Catalog")]
    public sealed class SkinCatalog : ScriptableObject
    {
        public BuildingConfig Building;
        public List<CharacterSkin> Skins = new();
        public CharacterSkin Find(string id) => Skins.FirstOrDefault(s => s != null && s.Id == id);
        public static SkinCatalog Load() => Resources.Load<SkinCatalog>("UI/SkinCatalog");
    }
}
