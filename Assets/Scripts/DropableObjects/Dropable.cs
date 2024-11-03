using Devotion.Items;
using System.Collections.Generic;
using UnityEngine;

namespace Devotion.Drop
{
    public class Dropable : MonoBehaviour
    {
        [Header("List drops")]
        [SerializeField] private List<Items.Item> _drops;

        [Header("Drop only one or more items")]
        [SerializeField] private bool _isOneDrop;

        private int _maxChanceDrop = 101;
        private int _minChanceDrop = 0;
        private int _currentChanceDrop;
        private List<Items.Item> _currentDrops;

        private void Start()
        {
            _currentDrops = new List<Items.Item>();
            _currentChanceDrop = Random.Range(_minChanceDrop, _maxChanceDrop);
        }

        private void OnDestroy()
        {
            if (_isOneDrop)
                DropSingleItem();
            else
                DropMultipleItems();
        }

        // only one item drops
        private void DropSingleItem()
        {
            for (int i = 0; i < _drops.Count; i++)
            {
                if (_drops[i].LowerBound <= _currentChanceDrop && _currentChanceDrop <= _drops[i].UpperBound)
                {
                    var obj = Instantiate(_drops[i].Prefab);
                    obj.transform.position = transform.position;
                    break;
                }
            }
        }

        // method allows to drop an item if the drop chance falls into several ranges
        private void DropMultipleItems()
        {
            for (int i = 0; i < _drops.Count; i++)
            {
                if (_drops[i].LowerBound <= _currentChanceDrop && _currentChanceDrop <= _drops[i].UpperBound)
                {
                    _currentDrops.Add(_drops[i]);
                }
            }

            _currentChanceDrop = Random.Range(0, _currentDrops.Count + 1);

            for (int i = 0; i < _currentDrops.Count; i++)
            {
                if (i == _currentChanceDrop)
                {
                    var obj = Instantiate(_currentDrops[i].Prefab);
                    obj.transform.position = transform.position;
                    break;
                }
            }
        }
    }
}
