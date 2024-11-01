using System.Collections.Generic;
using UnityEngine;

namespace Devotion.Drop
{
    public class Dropable : MonoBehaviour
    {
        [SerializeField] private List<Items.Item> _items;
        [SerializeField] private List<int> _chance;

        private Dictionary<Items.Item, int> _dictionaryItems = new();
        private int _maxChanceDrop = 51;
        private int _minChanceDrop = 0;
        private int _currentChanceDrop;

        private void Awake()
        {
            for (int i = 0; i < _items.Count && i < _chance.Count; i++)
            {
                _dictionaryItems.Add(_items[i], _chance[i]);
            }
        }

        private void Start()
        {
            _currentChanceDrop = Random.Range(_minChanceDrop, _maxChanceDrop);
            Debug.Log(_currentChanceDrop);
        }

        private void OnDestroy()
        {
            Drop();
        }

        public void Drop()
        {
            foreach (var item in _dictionaryItems)
            {
                if (item.Value >= _currentChanceDrop)
                {
                    Debug.Log("drop item");
                    var obj = Instantiate(item.Key.Prefab);
                    obj.transform.position = transform.position;
                    break;
                }
            }
        }
    }
}
