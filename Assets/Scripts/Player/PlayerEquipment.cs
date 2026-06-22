using System;
using Devotion.SDK.Controllers;
using MineArena.Interfaces;
using MineArena.Items;
using MineArena.Messages;
using MineArena.Messages.MessageService;
using UnityEngine;

namespace MineArena.PlayerSystem
{
    public class PlayerEquipment : MonoBehaviour, IDefenseProvider,
        IMessageSubscriber<Devotion.SDK.Messages.Player.PlayerProgressLoaded>
    {
        public event Action<ArmorSlot, ArmorConfig> ArmorChanged;

        [Header("Animator")]
        [SerializeField] private PlayerAnimatorController _animatorController;

        [Header("Hand items (already in rig)")]
        [SerializeField] private GameObject _swordInHand;
        [SerializeField] private GameObject _pickaxeInHand;
        [SerializeField] private GameObject _bowInHand;

        [Header("Attack configs")]
        [SerializeField] private AttackConfig _defaultSwordAttack;
        [SerializeField] private AttackConfig _swordAttack;
        [SerializeField] private AttackConfig _defaultBowAttack;
        [SerializeField] private AttackConfig _bowAttack;

        [Header("Equipped items")]
        [SerializeField] private PickaxeConfig _pickaxe;
        [SerializeField] private ArmorConfig _helmet;
        [SerializeField] private ArmorConfig _chest;
        [SerializeField] private ArmorConfig _leggings;
        [SerializeField] private ArmorConfig _boots;

        [Header("Armor visuals (existing on character)")]
        [SerializeField] private ArmorVisual _helmetVisual;
        [SerializeField] private ArmorVisual _chestVisual;
        [SerializeField] private ArmorVisual _leggingsVisual;
        [SerializeField] private ArmorVisual _bootsVisual;

        private HandItemType _lastActiveHandItem = HandItemType.None;
        private IPlayerAnimator _animator;

        private const float ArmorReductionScale = 100f;

        private void Awake()
        {
            MessageService.Subscribe(this);

            _animator = _animatorController ?? GetComponent<IPlayerAnimator>();
            ResolveHandItemReferences();
            LoadArmorFromProgress();

            UpdateHandAnimatorState();
            RefreshArmorVisuals();
        }

        private void OnEnable()
        {
            LoadArmorFromProgress();
            RefreshArmorVisuals();
        }

        private void OnDestroy()
        {
            MessageService.Unsubscribe(this);
        }

#region Equip API

        public void EquipSword(AttackConfig attackConfig, bool switchToHand = true)
        {
            _swordAttack = attackConfig;

            if (switchToHand)
                SetActiveHandItem(HandItemType.Sword);

            BroadcastSwordAttackConfig();

            UpdateHandItemMaterial(HandItemType.Sword);
        }

        public void EquipPickaxe(PickaxeConfig config, bool switchToHand = false)
        {
            _pickaxe = config;

            if (switchToHand)
                SetActiveHandItem(HandItemType.Pickaxe);

            UpdateHandItemMaterial(HandItemType.Pickaxe);
        }

        public void EquipBow(AttackConfig attackConfig, bool switchToHand = true)
        {
            _bowAttack = attackConfig;

            if (switchToHand)
                SetActiveHandItem(HandItemType.Bow);

            UpdateHandItemMaterial(HandItemType.Bow);
        }

        public void EquipArmor(ArmorConfig armor)
        {
            if (armor == null) return;

            switch (armor.Slot)
            {
                case ArmorSlot.Helmet:
                    _helmet = armor;
                    ApplyArmorVisual(_helmetVisual, _helmet);
                    ArmorChanged?.Invoke(ArmorSlot.Helmet, _helmet);
                    break;
                case ArmorSlot.Chest:
                    _chest = armor;
                    ApplyArmorVisual(_chestVisual, _chest);
                    ArmorChanged?.Invoke(ArmorSlot.Chest, _chest);
                    break;
                case ArmorSlot.Leggings:
                    _leggings = armor;
                    ApplyArmorVisual(_leggingsVisual, _leggings);
                    ArmorChanged?.Invoke(ArmorSlot.Leggings, _leggings);
                    break;
                case ArmorSlot.Boots:
                    _boots = armor;
                    ApplyArmorVisual(_bootsVisual, _boots);
                    ArmorChanged?.Invoke(ArmorSlot.Boots, _boots);
                    break;
            }

            SaveArmorToProgress(armor.Slot, armor);
        }

        public ArmorConfig UnequipArmor(ArmorSlot slot)
        {
            ArmorConfig removedArmor = null;

            switch (slot)
            {
                case ArmorSlot.Helmet:
                    removedArmor = _helmet;
                    _helmet = null;
                    ApplyArmorVisual(_helmetVisual, _helmet);
                    break;
                case ArmorSlot.Chest:
                    removedArmor = _chest;
                    _chest = null;
                    ApplyArmorVisual(_chestVisual, _chest);
                    break;
                case ArmorSlot.Leggings:
                    removedArmor = _leggings;
                    _leggings = null;
                    ApplyArmorVisual(_leggingsVisual, _leggings);
                    break;
                case ArmorSlot.Boots:
                    removedArmor = _boots;
                    _boots = null;
                    ApplyArmorVisual(_bootsVisual, _boots);
                    break;
            }

            if (removedArmor != null)
            {
                SaveArmorToProgress(slot, null);
                ArmorChanged?.Invoke(slot, null);
            }

            return removedArmor;
        }

#endregion

#region Query API

        public ArmorConfig GetEquippedArmor(ArmorSlot slot)
        {
            return slot switch
            {
                ArmorSlot.Helmet => _helmet,
                ArmorSlot.Chest => _chest,
                ArmorSlot.Leggings => _leggings,
                ArmorSlot.Boots => _boots,
                _ => null
            };
        }

        public PickaxeConfig Pickaxe => _pickaxe;

        public AttackConfig GetSwordAttackConfig() => _swordAttack ?? _defaultSwordAttack;
        public AttackConfig GetBowAttackConfig() => _bowAttack ?? _defaultBowAttack;
        public float GetMiningDuration() => _pickaxe?.MiningDuration ?? 3.33f;
        public int GetMiningLoops() => Mathf.Max(1, _pickaxe?.MiningLoops ?? 2);

        public HandItemType LastActiveHandItem => _lastActiveHandItem;

#endregion

#region Animator / Visuals

        public void SetActiveHandItem(HandItemType type)
        {
            _lastActiveHandItem = type;

            if (_swordInHand != null)
                _swordInHand.SetActive(type == HandItemType.Sword);

            if (_pickaxeInHand != null)
                _pickaxeInHand.SetActive(type == HandItemType.Pickaxe);

            if (_bowInHand != null)
                _bowInHand.SetActive(type == HandItemType.Bow);

            UpdateHandItemMaterial(type);
            UpdateHandAnimatorState();
        }

        private void UpdateHandAnimatorState()
        {
            if (_animator == null)
                return;

            _animator.SetHandItemState(_lastActiveHandItem);
        }

        private void UpdateHandItemMaterial(HandItemType type)
        {
            switch (type)
            {
                case HandItemType.Sword:
                    var attack = GetSwordAttackConfig();
                    ApplyMaterialToItem(_swordInHand, attack?.Material);
                    break;
                case HandItemType.Pickaxe:
                    ApplyMaterialToItem(_pickaxeInHand, _pickaxe?.Material);
                    break;
                case HandItemType.Bow:
                    ApplyMaterialToItem(_bowInHand, _bowAttack?.Material ?? _defaultBowAttack?.Material);
                    break;
            }
        }

        private void ResolveHandItemReferences()
        {
            if (_bowInHand == null)
                _bowInHand = FindChildGameObject("bow");
        }

        private GameObject FindChildGameObject(string childName)
        {
            if (string.IsNullOrWhiteSpace(childName))
                return null;

            var children = GetComponentsInChildren<Transform>(true);
            foreach (var child in children)
            {
                if (child != null && string.Equals(child.name, childName, StringComparison.OrdinalIgnoreCase))
                    return child.gameObject;
            }

            return null;
        }

        private static void ApplyMaterialToItem(GameObject item, Material material)
        {
            if (item == null || material == null)
                return;

            var renderers = item.GetComponentsInChildren<Renderer>(includeInactive: true);
            foreach (var renderer in renderers)
            {
                if (renderer == null)
                    continue;

                var mats = renderer.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    mats[i] = material;
                }
                renderer.sharedMaterials = mats;
            }
        }

#endregion

#region Damage reduction

        public float ModifyIncomingDamage(float baseDamage)
        {
            if (baseDamage <= 0f)
                return 0f;

            float totalArmor = (_helmet?.Resist ?? 0)
                               + (_chest?.Resist ?? 0)
                               + (_leggings?.Resist ?? 0)
                               + (_boots?.Resist ?? 0);

            float reductionFactor = Mathf.Clamp01(totalArmor / ArmorReductionScale);
            float reducedDamage = baseDamage * (1f - reductionFactor);

            return Mathf.Max(0f, reducedDamage);
        }

#endregion

        private void BroadcastSwordAttackConfig()
        {
            var attack = GetSwordAttackConfig();
            if (attack != null)
                GameMessages.NewSwordEquiped.Publish(attack);
        }

        private void RefreshArmorVisuals()
        {
            ApplyArmorVisual(_helmetVisual, _helmet);
            ApplyArmorVisual(_chestVisual, _chest);
            ApplyArmorVisual(_leggingsVisual, _leggings);
            ApplyArmorVisual(_bootsVisual, _boots);
        }

        public void OnMessage(Devotion.SDK.Messages.Player.PlayerProgressLoaded message)
        {
            LoadArmorFromProgress();
            RefreshArmorVisuals();
            NotifyAllArmorChanged();
        }

        private void LoadArmorFromProgress()
        {
            var progress = GameRoot.PlayerProgress?.InventoryProgress;
            if (progress == null)
                return;

            _helmet = ResolveSavedArmor(progress.GetEquippedArmorItemId(ArmorSlot.Helmet.ToString()), ArmorSlot.Helmet);
            _chest = ResolveSavedArmor(progress.GetEquippedArmorItemId(ArmorSlot.Chest.ToString()), ArmorSlot.Chest);
            _leggings = ResolveSavedArmor(progress.GetEquippedArmorItemId(ArmorSlot.Leggings.ToString()), ArmorSlot.Leggings);
            _boots = ResolveSavedArmor(progress.GetEquippedArmorItemId(ArmorSlot.Boots.ToString()), ArmorSlot.Boots);
        }

        private static ArmorConfig ResolveSavedArmor(string itemId, ArmorSlot slot)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return null;

            var armor = GameRoot.GameConfig?.ItemDatabase?.GetItemConfig(itemId) as ArmorConfig;
            if (armor == null || armor.Slot != slot)
                return null;

            return armor;
        }

        private static void SaveArmorToProgress(ArmorSlot slot, ArmorConfig armor)
        {
            GameRoot.PlayerProgress?.InventoryProgress?.SetEquippedArmorItemId(
                slot.ToString(),
                armor != null ? armor.Name : string.Empty
            );
        }

        private void NotifyAllArmorChanged()
        {
            ArmorChanged?.Invoke(ArmorSlot.Helmet, _helmet);
            ArmorChanged?.Invoke(ArmorSlot.Chest, _chest);
            ArmorChanged?.Invoke(ArmorSlot.Leggings, _leggings);
            ArmorChanged?.Invoke(ArmorSlot.Boots, _boots);
        }

        private static void ApplyArmorVisual(ArmorVisual visual, ArmorConfig config)
        {
            bool hasArmor = config != null;

            if (visual.Roots != null)
            {
                foreach (var root in visual.Roots)
                {
                    if (root != null)
                        root.SetActive(hasArmor);
                }
            }

            if (visual.Renderers != null)
            {
                foreach (var renderer in visual.Renderers)
                {
                    if (renderer == null)
                        continue;

                    renderer.enabled = hasArmor;

                    if (hasArmor && config.Material != null)
                    {
                        var mats = renderer.sharedMaterials;
                        for (int i = 0; i < mats.Length; i++)
                        {
                            mats[i] = config.Material;
                        }
                        renderer.sharedMaterials = mats;
                    }
                }
            }
        }

        [Serializable]
        private struct ArmorVisual
        {
            public GameObject[] Roots;
            public Renderer[] Renderers;
        }
    }
}
