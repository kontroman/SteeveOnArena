using UnityEngine;
using Devotion.Resourse;
using Sirenix.OdinInspector.Editor;

namespace Devotion.Item
{
    [CreateAssetMenu(fileName = "New Item", menuName = "Item/Create new item", order = 51)]
    public class Item : ScriptableObject
    {
        [SerializeField] private string _name;
        [SerializeField] private GameObject _item;
        [SerializeField] private Resource _resource;
        [SerializeField] private int _amountResources;
        [SerializeField] private bool _isAddInventory = true;

        public string Name => _name;
        public GameObject GameObject => _item;
        public Resource Resource => _resource;
        public int AmountResources => _amountResources;

        
    }
}
