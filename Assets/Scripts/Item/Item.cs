using UnityEngine;
using Devotion.Resourse;

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

        public string Name => _name;
        public GameObject Prefab => _prefab;
        public int AmountResources => _amountResources;
        public bool IsAddInventory => _isAddInventory;

        public delegate void Activation();
    }
}
