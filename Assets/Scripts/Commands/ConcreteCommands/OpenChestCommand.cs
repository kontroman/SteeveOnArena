using Devotion.SDK.Controllers;
using Devotion.SDK.Helpers;
using MineArena.Controllers;
using MineArena.InteractableObjects;
using MineArena.Items;
using MineArena.Managers;
using MineArena.PlayerSystem;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace MineArena.Commands
{
    [CreateAssetMenu(fileName = "New OpenChestCommand", menuName = "Commands/OpenChestCommand")]
    public class OpenChestCommand : BaseCommand
    {
        public override Task Execute(Component component) => Open(component.GetComponent<WorldChest>(),
            () => component.GetComponent<InteractableObject>()?.CompleteInteraction());

        public override Task Execute(Action callback) => Open(
            GameRoot.GetManager<InteractionManager>().CurrentTargetTransform.GetComponent<WorldChest>(), callback);

        private async Task Open(WorldChest chest, Action callback)
        {
            if (chest == null || !chest.TryBeginOpening()) return;
            var player = Player.Instance;
            var movement = player.GetComponentFromList<PlayerMovement>();
            var attack = player.GetComponentFromList<PlayerAttack>();
            var rotation = player.GetComponentFromList<RotationController>();
            var animator = player.GetComponentFromList<PlayerAnimatorController>() ?? player.GetComponent<IPlayerAnimator>();
            try
            {
                movement.SetMovement(false);
                attack.SetComponentEnable(false);
                rotation.RotatePlayerToTarget(chest.transform);
                animator?.SetRunning(false);
                animator?.TriggerChestOpening();
                await CoroutineHelper.DelayAsync(0.8f);
                if (chest == null) return;
                chest.GetComponent<Animator>()?.SetTrigger("Execute");
                await CoroutineHelper.DelayAsync(2.7f);
                if (chest == null || !chest.TryCollect()) return;
                var prize = chest.Prize;
                callback?.Invoke();
                MineArena.Messages.GameMessages.WorldChestOpened.Publish(prize);
            }
            finally
            {
                if (chest != null) chest.CancelOpening();
                if (player != null)
                {
                    movement.SetMovement(true);
                    animator?.ResetChestOpening();
                    attack.SetComponentEnable(true);
                }
            }
        }
    }
}
