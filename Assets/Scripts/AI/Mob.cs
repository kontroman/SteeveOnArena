using UnityEngine;
using MineArena.ObjectPools;
using MineArena.Controllers;
using MineArena.Interfaces;
using MineArena.Structs;
using MineArena.Game.Health;

namespace MineArena.AI
{
    public class Mob : MonoBehaviour
    {
        private Transform _playerTransform;
        private MobTypes _type;

        [SerializeField] private MobCombat _mobCombat;
        [SerializeField] private MobMovement _mobMovement;
        [SerializeField] private MobHealth _mobHealth;
        [SerializeField] private MobAnimationController _mobAnimation;
        [SerializeField] private MobPreset _preset;

        public void Start()
        {
            _playerTransform = Player.Instance.GetComponentFromList<Transform>();
            SetPresetParameters(_preset);
        }

        public void SetPresetParameters(MobPreset preset)
        {
            _preset = preset;
            _type = preset.MobType;
            _mobCombat.SetParameters(preset);
            _mobMovement.SetParameters(preset);
            _mobHealth.SetParameters(preset);
            _mobAnimation?.SetParameters(preset);
        }
    }
}
