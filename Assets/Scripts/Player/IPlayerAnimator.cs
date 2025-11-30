using UnityEngine;

namespace MineArena.PlayerSystem
{
    public interface IPlayerAnimator
    {
        Animator Animator { get; }

        bool IsRunning();
        void SetRunning(bool isRunning);

        void TriggerAttack();
        void TriggerDamage();
        void TriggerDeath();
        void TriggerVictory();

        void PlayMiningAnimation(string stateName, int layer);
        void ResetMiningAnimation();

        void TriggerChestOpening();
        void ResetChestOpening();

        void SetHandItemState(HandItemType type);
    }
}
