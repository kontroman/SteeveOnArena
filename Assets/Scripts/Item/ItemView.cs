using Devotion.Item;
using UnityEngine;
using UnityEngine.UI;

namespace Devotion.Item
{
    public class ItemView : MonoBehaviour
    {
        [SerializeField] private Item _item;
        [SerializeField] private GameObject _gameObject;
        //[SerializeField] private Text _name;

        private void Start()
        {
            _gameObject = _item.GameObject;
            //_name.text = _item.Name;
        }
    }
}
