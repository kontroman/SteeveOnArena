using System.Collections.Generic;
using UnityEngine;

namespace Devotion.Equipment
{ 
    [CreateAssetMenu(fileName = "New Item", menuName = "Items/Create new ItemsConfig", order = 50)]
    public class EquipmentItemsConfig : ScriptableObject
    {
        public List<EquipmentItemConfig> Swords = new();
        public List<EquipmentItemConfig> Armor = new();
    }
}
