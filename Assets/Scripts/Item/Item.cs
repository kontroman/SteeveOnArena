using UnityEngine;
using Sirenix.OdinInspector;

namespace Devotion.Items
{
    //[CreateAssetMenu(fileName = "New Item", menuName = "Items/Create new item", order = 51)]
    public class Item : ScriptableObject
    {
        [SerializeField] private string _name;
        [SerializeField] private int _amountResources;
        [SerializeField] private GameObject _prefab;
        [SerializeField] private bool _isAddInventory = true;

        [SerializeField] public BaseCommand command;

        [HideLabel, MinMaxSlider(0, 100, true)]
        [SerializeField] private Vector2 _minMaxSlider;
        [SerializeField] private int _numberLayerGround = 3;

        public float LowerBoundChance { get { return _minMaxSlider.x; } }
        public float UpperBoundChance { get { return _minMaxSlider.y; } }
        public string Name => _name;
        public GameObject Prefab => _prefab;
        public int AmountResources => _amountResources;
        public bool IsAddInventory => _isAddInventory;

        public delegate void Activation();
    }
}
