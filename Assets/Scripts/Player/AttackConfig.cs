using UnityEngine;

namespace Devotion.PlayerSystem
{
    [CreateAssetMenu(menuName = "Combat/Attack Config")]
    public class AttackConfig : ScriptableObject
    {
        public float BaseDamage = 10f;
        public float Radius = 1.5f;
        public float Angle = 90f;
        public float Cooldown = 0.5f;
        public float AnimationDelay = 0.1f;
        public LayerMask AttackableLayers;
        public GameObject ImpactVFX;
        public AudioClip ImpactSound;
    }
}