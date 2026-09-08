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
        [SerializeField, Tooltip("Optional player-facing name. Name remains the inventory ID.")]
        private string _displayName;
        [SerializeField] private GameObject _prefab;
        [SerializeField] private Sprite _icon;
        [SerializeField] private bool _usable;
        [SerializeField] private bool _stackable;
        [SerializeField] private ICommand _command;
        [SerializeField] private bool _blockStyleIcon;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private string _descriptionLocalizationKey;
        [SerializeField] private List<ResourceRequired> _craftCosts = new();
        [SerializeField, Min(1)] private int _craftAmount = 1;
        [SerializeField, Min(0.1f)] private float _craftSeconds = 7f;

        public string Name => _name;
        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? _name : _displayName;
        public GameObject Prefab => _prefab;
        public Sprite Icon => _icon;
        public bool Usable => _usable;
        public bool Stackable => _stackable;
        public ICommand Command => _command;
        public bool BlockStyleIcon => _blockStyleIcon;
        public string Description => _description;
        public string DescriptionLocalizationKey => _descriptionLocalizationKey;
        public IReadOnlyList<ResourceRequired> CraftCosts => _craftCosts;
        public int CraftAmount => Stackable ? Mathf.Max(1, _craftAmount) : 1;
        public float CraftSeconds => Mathf.Max(0.1f, _craftSeconds);
    }
}
