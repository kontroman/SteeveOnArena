using Devotion.SDK.Controllers;
using MineArena.Controllers;
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
            Animator pa = Player.Instance.GetComponentFromList<Animator>();

            pm.SetMovement(false);
            patc.SetComponentEnable(false);
            rc.RotatePlayerToTarget(chest);

            pa.SetBool("isRunning", false);
            pa.SetTrigger("ChestOpening");

            await Task.Delay(TimeSpan.FromSeconds(0.8f));

            chest.GetComponent<Animator>().SetTrigger("Execute");

            await Task.Delay(TimeSpan.FromSeconds(2.7f));

            pm.SetMovement(true);
            pa.ResetTrigger("ChestOpening");
            patc.SetComponentEnable(true);
            callback?.Invoke();
        }
    }
}