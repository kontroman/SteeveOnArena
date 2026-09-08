using UnityEngine;

namespace MineArena.Items
{
    public enum PotionEffect { Healing, Regeneration, Speed }

    [CreateAssetMenu(menuName = "Items/Potion")]
    public class PotionConfig : StackableItemConfig
    {
        [SerializeField] private PotionEffect _effect;
        [SerializeField, Min(0.1f)] private float _strength = 40f;
        [SerializeField, Min(0)] private float _duration;
        public PotionEffect Effect => _effect;
        public float Strength => _strength;
        public float Duration => _duration;
    }
}
