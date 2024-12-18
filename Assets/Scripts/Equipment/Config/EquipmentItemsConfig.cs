using System.Collections.Generic;
using UnityEngine;

namespace Devotion.Equipment
{ 
    [CreateAssetMenu(fileName = "New Item", menuName = "Items/Create new ItemsConfig", order = 50)]
    public class EquipmentItemsConfig : ScriptableObject
    {
        [SerializeField] public List<EquipmentItemConfig> Swords = new();
        [SerializeField] public List<EquipmentItemConfig> Armor = new();
        [SerializeField] public List<EquipmentItemConfig> Ore = new();
        [SerializeField] public List<EquipmentItemConfig> Coin = new();

        public Dictionary<List<EquipmentItemConfig>, ItemTypes> Items = new();        

        private void Awake()
        {
            Creat();
        }

        private void Creat()
        {
            Items.Add(Swords, Swords[0].Type);
            Items.Add(Armor, Armor[0].Type);
            Items.Add(Ore, Ore[0].Type);
            Items.Add(Coin, Coin[0].Type);
        }
    }
}
