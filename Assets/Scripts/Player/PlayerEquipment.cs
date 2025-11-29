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
        [SerializeField] private Animator _animator;
        [SerializeField] private string _handItemParameter = "HandItemState";

        [Header("Hand items (already in rig)")]
        [SerializeField] private GameObject _swordInHand;
        [SerializeField] private GameObject _pickaxeInHand;

        [Header("Attack configs")]
        [SerializeField] private AttackConfig _defaultSwordAttack;

        [Header("Equipped items")]
        [SerializeField] private SwordConfig _sword;
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
        private int _handItemParamHash;

        private const float ArmorReductionScale = 100f;

        private void Awake()
        {
            if (_animator == null)
                _animator = GetComponent<Animator>();

            _handItemParamHash = Animator.StringToHash(_handItemParameter);

            UpdateHandAnimatorState();
            RefreshArmorVisuals();
        }

#region Equip API

        public void EquipSword(SwordConfig config, bool switchToHand = true)
        {
            _sword = config;

            if (switchToHand)
                SetActiveHandItem(HandItemType.Sword);

            BroadcastSwordAttackConfig();
        }

        public void EquipPickaxe(PickaxeConfig config, bool switchToHand = false)
        {
            _pickaxe = config;

            if (switchToHand)
                SetActiveHandItem(HandItemType.Pickaxe);
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

        public SwordConfig Sword => _sword;
        public PickaxeConfig Pickaxe => _pickaxe;

        public AttackConfig GetSwordAttackConfig() => _sword?.AttackProfile ?? _defaultSwordAttack;
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

            UpdateHandAnimatorState();
        }

        private void UpdateHandAnimatorState()
        {
            if (_animator == null)
                return;

            _animator.SetInteger(_handItemParamHash, (int)_lastActiveHandItem);
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
