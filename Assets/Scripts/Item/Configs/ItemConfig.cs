using Achievements;
using System.Collections.Generic;
using MineArena.Buildings;
using MineArena.Commands;
using UnityEngine;

namespace MineArena.Items
{
    public abstract class ItemConfig : ScriptableObject, IAchievementTarget
    {
        [SerializeField] private string _name;
        [SerializeField] private GameObject _prefab;
        [SerializeField] private Sprite _icon;
        [SerializeField] private bool _usable;
        [SerializeField] private bool _stackable;
        [SerializeField] private ICommand _command;
        [SerializeField] private bool _blockStyleIcon;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private string _descriptionLocalizationKey;
        [SerializeField] private List<ResourceRequired> _craftCosts = new();

        public string Name => _name;
        public GameObject Prefab => _prefab;
        public Sprite Icon => _icon;
        public bool Usable => _usable;
        public bool Stackable => _stackable;
        public ICommand Command => _command;
        public bool BlockStyleIcon => _blockStyleIcon;
        public string Description => _description;
        public string DescriptionLocalizationKey => _descriptionLocalizationKey;
        public IReadOnlyList<ResourceRequired> CraftCosts => _craftCosts;
    }
}
