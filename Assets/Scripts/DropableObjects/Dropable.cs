using System.Collections.Generic;
using UnityEngine;
using Devotion.Controllers;

namespace Devotion.Drop
{
    public class Dropable : MonoBehaviour
    {
        [Header("List drops")]
        [SerializeField] private List<Equipment.EquipmentItemConfig> _drops1;

        [Header("Drop only one or more items")]
        [SerializeField] private bool _isOneDrop;

        private int _maxChanceDrop = 101;
        private int _minChanceDrop = 0;
        private int _currentChanceDrop;
        private List<Equipment.EquipmentItemConfig> _currentDrops1;

        private void Start()
        {
            _currentDrops1 = new List<Equipment.EquipmentItemConfig>();
            _currentChanceDrop = Random.Range(_minChanceDrop, _maxChanceDrop);
        }

        private void OnDestroy()
        {
            if (_isOneDrop)
                DropSingleItem();
            else
                DropMultipleItems();
        }

        private void DropSingleItem()
        {
            for (int i = 0; i < _drops1.Count; i++)
            {
                if (_drops1[i].LowerBoundChance <= _currentChanceDrop && _currentChanceDrop <= _drops1[i].UpperBoundChance)
                {
                    var obj = Instantiate(_drops1[i].Prefab);
                    obj.transform.position = transform.position;
                    GameRoot.Instance.GetManager<Managers.SoundManager>().PlayEffect(SoundTags.DropResourse);
                    break;
                }
            }
        }

        private void DropMultipleItems()
        {
            for (int i = 0; i < _drops1.Count; i++)
            {
                if (_drops1[i].LowerBoundChance <= _currentChanceDrop && _currentChanceDrop <= _drops1[i].UpperBoundChance)
                {
                    _currentDrops1.Add(_drops1[i]);
                }
            }

            _currentChanceDrop = Random.Range(0, _currentDrops1.Count);

            for (int i = 0; i < _currentDrops1.Count; i++)
            {
                if (i == _currentChanceDrop)
                {
                    var obj = Instantiate(_currentDrops1[i].Prefab);
                    obj.transform.position = transform.position;
                    GameRoot.Instance.GetManager<Managers.SoundManager>().PlayEffect(SoundTags.DropResourse);
                    break;
                }
            }
        }
    }
}
