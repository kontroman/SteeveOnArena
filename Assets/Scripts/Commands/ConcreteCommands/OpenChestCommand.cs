using Devotion.SDK.Controllers;
using MineArena.Controllers;
using MineArena.InteractableObjects;
using MineArena.Managers;
using MineArena.PlayerSystem;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace MineArena.Commands
{
    [CreateAssetMenu(fileName = "New UseItemCommand", menuName = "Commands/OpenChestCommand")]
    public class OpenChestCommand : BaseCommand
    {
        public override async Task Execute(Action callback)
        {
            PlayerMovement pm = Player.Instance.GetComponentFromList<PlayerMovement>();
            PlayerAttack patc = Player.Instance.GetComponentFromList<PlayerAttack>();
            RotationController rc = Player.Instance.GetComponentFromList<RotationController>();
            Transform chest = GameRoot.GetManager<InteractionManager>().CurrentTargetTransform;
            var pa = Player.Instance.GetComponentFromList<PlayerAnimatorController>() ??
                     Player.Instance.GetComponent<IPlayerAnimator>();

            pm.SetMovement(false);
            patc.SetComponentEnable(false);
            rc.RotatePlayerToTarget(chest);

            pa?.SetRunning(false);
            pa?.TriggerChestOpening();

            await Task.Delay(TimeSpan.FromSeconds(0.8f));

            chest.GetComponent<Animator>().SetTrigger("Execute");

            await Task.Delay(TimeSpan.FromSeconds(2.7f));

            pm.SetMovement(true);
            pa?.ResetChestOpening();
            patc.SetComponentEnable(true);
            callback?.Invoke();

            var prize = chest.GetComponent<WorldChest>().Prize;
            Messages.GameMessages.WorldChestOpened.Publish(prize);
        }
    }
}
