using UnityEngine;

namespace Devotion.Drop
{
    public class Dropable : MonoBehaviour
    {
        [SerializeField] private GameObject _item;
        [SerializeField] private int _chanceDrop;

        private int _maxChanceDrop = 101;
        private int _minChanceDrop = 0;
        private int _currentChanceDrop;

        private void OnDestroy()
        {
            Drop();
        }

        public void Drop()
        {
            if (CheckDropChance())
            {
                Debug.Log("drop item");
                var obj = Instantiate(_item);
                obj.transform.position = transform.position;
            }
        }

        private bool CheckDropChance()
        {
            _currentChanceDrop = Random.Range(_minChanceDrop, _maxChanceDrop);
            Debug.Log(_currentChanceDrop);

            return (_currentChanceDrop <= _chanceDrop);
        }
    }
}
