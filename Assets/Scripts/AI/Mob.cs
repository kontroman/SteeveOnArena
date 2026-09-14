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
        public bool TutorialDormant { get; private set; }

        private void OnEnable()
        {
            if (_preset != null) SetPresetParameters(_preset);
            SetTutorialDormant(false);
        }
        public void SetTutorialDormant(bool dormant)
        {
            TutorialDormant = dormant;
            if (_mobMovement != null) _mobMovement.enabled = !dormant;
            if (_mobCombat != null) _mobCombat.enabled = !dormant;
            var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                if (dormant) agent.ResetPath();
                agent.isStopped = dormant;
            }
            if (dormant) _mobAnimation?.ForceIdle();
        }
        private void Update()
        {
            if (TutorialDormant && (!MineArena.Managers.TutorialService.Expedition ||
                MineArena.Managers.TutorialService.Progress.Step != MineArena.Managers.TutorialStep.Mine))
                SetTutorialDormant(false);
        }

        public void SetPresetParameters(MobPreset preset)
        {
            if (preset == null) return;
            _preset = preset;
            _type = preset.MobType;
            var feedback = GetComponent<MobFeedback>() ?? gameObject.AddComponent<MobFeedback>();
            feedback.Configure(_type);
            _mobCombat.SetParameters(preset);
            _mobMovement.SetParameters(preset);
            _mobHealth.SetParameters(preset);
            _mobAnimation?.SetParameters(preset);
        }
    }
}
