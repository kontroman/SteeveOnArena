using System;
using MineArena.Interfaces;
using MineArena.Items;
using MineArena.Messages;
using UnityEngine;

namespace MineArena.PlayerSystem
{
    public class PlayerEquipment : MonoBehaviour, IDefenseProvider
    {
        [Header("Animator")]
        [SerializeField] private PlayerAnimatorController _animatorController;

        [Header("Hand items (already in rig)")]
        [SerializeField] private GameObject _swordInHand;
        [SerializeField] private GameObject _pickaxeInHand;

        [Header("Attack configs")]
        [SerializeField] private AttackConfig _defaultSwordAttack;
        [SerializeField] private AttackConfig _swordAttack;

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
            _animator = _animatorController ?? GetComponent<IPlayerAnimator>();

            UpdateHandAnimatorState();
            RefreshArmorVisuals();
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

        public void EquipArmor(ArmorConfig armor)
        {
            if (armor == null) return;

            switch (armor.Slot)
            {
                case ArmorSlot.Helmet:
                    _helmet = armor;
                    ApplyArmorVisual(_helmetVisual, _helmet);
                    break;
                case ArmorSlot.Chest:
                    _chest = armor;
                    ApplyArmorVisual(_chestVisual, _chest);
                    break;
                case ArmorSlot.Leggings:
                    _leggings = armor;
                    ApplyArmorVisual(_leggingsVisual, _leggings);
                    break;
                case ArmorSlot.Boots:
                    _boots = armor;
                    ApplyArmorVisual(_bootsVisual, _boots);
                    break;
            }
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
        public float GetMiningDuration() => _pickaxe?.MiningDuration ?? 3.33f;
        public int GetMiningLoops() => Mathf.Max(1, _pickaxe?.MiningLoops ?? 2);

        public HandItemType LastActiveHandItem => _lastActiveHandItem;

#endregion

#region Animator / Visuals

        public void SetActiveHandItem(HandItemType type)
        {
            if (type == HandItemType.None)
                type = _lastActiveHandItem;

            if (type != HandItemType.None)
                _lastActiveHandItem = type;

            if (_swordInHand != null)
                _swordInHand.SetActive(type == HandItemType.Sword);

            if (_pickaxeInHand != null)
                _pickaxeInHand.SetActive(type == HandItemType.Pickaxe);

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
            }
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
