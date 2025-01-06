using Devotion.Commands;
using UnityEngine;

namespace Devotion.Items
{
    public abstract class ItemConfig : ScriptableObject
    {
        [SerializeField] private string _name;
        [SerializeField] private GameObject _prefab;
        [SerializeField] private Sprite _icon;
        [SerializeField] private bool _usable;
        [SerializeField] private ICommand _command;

        public string Name => _name;
        public GameObject Prefab => _prefab;
        public Sprite Icon => _icon;
        public bool Usable => _usable;
        public ICommand Command => _command;
    }
}