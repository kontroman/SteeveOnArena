using System.Collections.Generic;
using UnityEngine;

namespace Devotion.Equipment
{ 
    [CreateAssetMenu(fileName = "New Item", menuName = "Items/Create new ItemsConfig", order = 50)]
    public class EquipmentItemsConfig : ScriptableObject
    {
        [SerializeField] public List<EquipmentItemConfig> Swords = new();
        [SerializeField] public List<EquipmentItemConfig> Armor = new();

        public Dictionary<List<EquipmentItemConfig>, ItemTypes> Items = new();        

        private void Awake()
        {
            Creat();
        }

        private void Creat()
        {
            Items.Add(Swords, Swords[0].Type);
            Items.Add(Armor, Armor[0].Type);
        }
    }
}
