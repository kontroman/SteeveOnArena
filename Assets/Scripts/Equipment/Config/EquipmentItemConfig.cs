using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Devotion.Equipment
{
    public class EquipmentItemConfig : ScriptableObject
    {
        [SerializeField] private int _level;
        [SerializeField] private int _price;
        [SerializeField] private GameObject _prefab;
        [SerializeField] private Image _icon;
        [SerializeField] private bool _isAddInventory = true;

        [Header("DropChance")]
        [HideLabel, MinMaxSlider(0, 100, true)]
        [SerializeField] private Vector2 _minMaxSlider;
        [SerializeField] private int _numberLayerGround = 3;

        [SerializeField] public BaseCommand command;

        public int LowerBoundChance { get { return (int)_minMaxSlider.x; } }
        public int UpperBoundChance { get { return (int)_minMaxSlider.y; } }
        public int Level => _level;
        public int Price => _price;
        public GameObject Prefab => _prefab;
        public Image Icon => _icon;
        public bool IsAddInventory => _isAddInventory;

        public delegate void Activation();
    }
}
