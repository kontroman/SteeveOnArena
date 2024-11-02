using System.Collections.Generic;
using UnityEngine;

namespace Devotion.Drop
{
    public class Dropable : MonoBehaviour
    {
        [Header("List drops")]
        [SerializeField] private List<Drop> _drops;

        [Header("Drop only or more ")]
        [SerializeField] private bool _isOneDrop;

        private int _maxChanceDrop = 101;
        private int _minChanceDrop = 0;
        private int _currentChanceDrop;
        private List<Drop> _currentDrops;

        private void Start()
        {
            _currentDrops = new List<Drop>();
            _currentChanceDrop = Random.Range(_minChanceDrop, _maxChanceDrop);
            Debug.Log(gameObject.name + "  " + _currentChanceDrop);
        }

        private void OnDestroy()
        {
            if (_isOneDrop)
                DropVers1();
            else
                DropVers2();
        }

        // only one item drops
        private void DropVers1()
        {
            for (int i = 0; i < _drops.Count; i++)
            {
                if (_drops[i].LowerBound <= _currentChanceDrop && _currentChanceDrop <= _drops[i].UpperBound)
                {
                    var obj = Instantiate(_drops[i].Item.Prefab);
                    obj.transform.position = transform.position;
                    break;
                }
            }
        }

        // method allows to drop an item if the drop chance falls into several ranges
        private void DropVers2()
        {
            for (int i = 0; i < _drops.Count; i++)
            {
                if (_drops[i].LowerBound <= _currentChanceDrop && _currentChanceDrop <= _drops[i].UpperBound)
                {
                    _currentDrops.Add(_drops[i]);
                }
            }

            _currentChanceDrop = Random.Range(0, _currentDrops.Count + 1);
            Debug.LogError(gameObject.name + "  " + _currentChanceDrop);

            for (int i = 0; i < _currentDrops.Count; i++)
            {
                if (i == _currentChanceDrop)
                {
                    var obj = Instantiate(_currentDrops[i].Item.Prefab);
                    obj.transform.position = transform.position;
                    break;
                }
            }
        }
    }
}
