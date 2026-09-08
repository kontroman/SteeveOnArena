using Devotion.SDK.Controllers;
using MineArena.Game.Health;
using MineArena.Items;
using MineArena.Managers;
using UnityEngine;

namespace MineArena.PlayerSystem
{
    public class PotionEffects : MonoBehaviour
    {
        private float _speedUntil, _speedBonus, _regenUntil, _regenRate, _nextDrink;
        private float _lastRegenTick;
        public float MovementMultiplier => Time.time < _speedUntil ? 1f + _speedBonus : 1f;
        public static PotionConfig SelectedPotion
        {
            get
            {
                var progress = GameRoot.PlayerProgress?.InventoryProgress;
                return progress == null ? null : GameRoot.GameConfig.ItemDatabase.GetItemConfig(progress.GetQuickSlotItemId(progress.SelectedQuickSlotIndex)) as PotionConfig;
            }
        }

        public static bool TryDrinkSelected()
        {
            var player = MineArena.Controllers.Player.Instance;
            var potion = SelectedPotion;
            if (player == null || potion == null) return false;
            var effects = player.GetComponent<PotionEffects>() ?? player.gameObject.AddComponent<PotionEffects>();
            return effects.TryDrink(potion);
        }

        public bool TryDrink(PotionConfig potion)
        {
            var health = GetComponent<Health>();
            if (potion == null || health == null || health.CurrentValue <= 0 || Time.time < _nextDrink) return false;
            if (potion.Effect == PotionEffect.Healing && health.CurrentValue >= health.MaxValue) return false;
            var inventory = GameRoot.GetManager<InventoryManager>();
            if (inventory == null || !inventory.TrySpendExact(potion, 1)) return false;
            _nextDrink = Time.time + 1f;
            switch (potion.Effect)
            {
                case PotionEffect.Healing: health.ChangeValue(potion.Strength); break;
                case PotionEffect.Regeneration:
                    _regenRate = potion.Strength; _lastRegenTick = Time.time; _regenUntil = Time.time + potion.Duration; break;
                case PotionEffect.Speed:
                    _speedBonus = potion.Strength; _speedUntil = Time.time + potion.Duration; break;
            }
            return true;
        }

        private void Update()
        {
            var health = GetComponent<Health>();
            if (health == null || health.CurrentValue <= 0)
            {
                _regenUntil = _speedUntil = 0;
                return;
            }
            float end = Mathf.Min(Time.time, _regenUntil);
            if (end > _lastRegenTick) health.ChangeValue(_regenRate * (end - _lastRegenTick));
            _lastRegenTick = Time.time;
        }
    }
}
