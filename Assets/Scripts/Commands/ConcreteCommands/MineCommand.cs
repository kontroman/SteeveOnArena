using UnityEngine;
using MineArena.Controllers;
using Devotion.SDK.Controllers;
using MineArena.PlayerSystem;
using MineArena.Managers;
using System;
using System.Threading.Tasks;

namespace MineArena.Commands
{
    [CreateAssetMenu(fileName = "New UseItemCommand", menuName = "Commands/MineCommand")]
    public class MineCommand : BaseCommand
    {
        public override async Task Execute(Action callback)
        {
            PlayerMovement pm = Player.Instance.GetComponentFromList<PlayerMovement>();
            PlayerAttack patc = Player.Instance.GetComponentFromList <PlayerAttack>();
            RotationController rc = Player.Instance.GetComponentFromList<RotationController>();
            Transform ore = GameRoot.GetManager<InteractionManager>().CurrentTargetTransform;
            Animator pa = Player.Instance.GetComponentFromList<Animator>();

            pm.SetMovement(false);
            patc.SetComponentEnable(false);
            rc.RotatePlayerToTarget(ore);

            pa.SetBool("isRunning", false);
            pa.SetTrigger("Mining");

            await Task.Delay(TimeSpan.FromSeconds(3.33f));

            pm.SetMovement(true);
            pa.ResetTrigger("Mining");
            patc.SetComponentEnable(true);
            callback?.Invoke();
        }
    }
}
