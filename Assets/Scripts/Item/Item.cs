using UnityEngine;
using Sirenix.OdinInspector;

namespace Devotion.Items
{
    [CreateAssetMenu(fileName = "New Item", menuName = "Item/Create new item", order = 51)]
    public class Item : ScriptableObject
    {
        [SerializeField] private string _name;
        [SerializeField] private int _amountResources;
        [SerializeField] private GameObject _prefab;
        [SerializeField] private bool _isAddInventory = true;

        [SerializeField] public BaseCommand command;

        [HideLabel, MinMaxSlider(0, 100, true)]
        [SerializeField] private Vector2 _minMaxSlider;

        public float LowerBound { get { return _minMaxSlider.x; } }
        public float UpperBound { get { return _minMaxSlider.y; } }
        public string Name => _name;
        public GameObject Prefab => _prefab;
        public int AmountResources => _amountResources;
        public bool IsAddInventory => _isAddInventory;

        public delegate void Activation();
    }
}
