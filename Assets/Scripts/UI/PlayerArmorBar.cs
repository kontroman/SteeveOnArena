using MineArena.Controllers;
using MineArena.Items;
using MineArena.PlayerSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MineArena.Game.UI
{
    public class PlayerArmorBar : MonoBehaviour
    {
        [SerializeField] private Image _fillImage;
        [SerializeField] private TMP_Text _valueText;
        private PlayerEquipment _equipment;

        private void OnEnable() => RefreshBinding();

        // Also rebind when a scene replaces the player while the HUD survives.
        private void Update() => RefreshBinding();

        private void OnDisable()
        {
            if (_equipment != null) _equipment.ArmorChanged -= OnArmorChanged;
            _equipment = null;
        }

        private void RefreshBinding()
        {
            var player = Player.Instance;
            var equipment = player != null ? player.GetComponent<PlayerEquipment>() : null;
            if (_equipment != equipment)
            {
                if (_equipment != null) _equipment.ArmorChanged -= OnArmorChanged;
                _equipment = equipment;
                if (_equipment != null) _equipment.ArmorChanged += OnArmorChanged;
            }
            RefreshValue();
        }

        private float _displayedReduction = -1f;

        private void OnArmorChanged(ArmorSlot slot, ArmorConfig armor) => RefreshValue();

        private void RefreshValue()
        {
            float reduction = _equipment != null ? _equipment.DamageReduction : 0f;
            if (Mathf.Approximately(reduction, _displayedReduction)) return;
            _displayedReduction = reduction;
            // This is mitigation, not a consumable shield or extra health.
            if (_fillImage != null) _fillImage.fillAmount = reduction;
            if (_valueText != null) _valueText.text = $"{Mathf.RoundToInt(reduction * 100f)}%";
        }
    }
}
