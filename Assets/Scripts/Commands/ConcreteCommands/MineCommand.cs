using UnityEngine;
using Devotion.Controllers;
using Devotion.PlayerSystem;
using Devotion.Managers;
using System;
using System.Threading.Tasks;

namespace Devotion.Commands
{
    [CreateAssetMenu(fileName = "New UseItemCommand", menuName = "Commands/MineCommand")]
    public class MineCommand : BaseCommand
    {
        public override async Task Execute(Action callback)
        {
            PlayerMovement pm = Player.Instance.GetComponentFromList<PlayerMovement>();
            Transform ore = GameRoot.Instance.GetManager<InteractionManager>().CurrentTargetTransform;
            Animator pa = Player.Instance.GetComponentFromList<Animator>();

            pm.SetMovement(false);
            pm.RotatePlayerToTarget(ore);

            pa.SetBool("isRunning", false);
            pa.SetTrigger("Mining");

            await Task.Delay(TimeSpan.FromSeconds(3.33f));

            pm.SetMovement(true);
            pa.ResetTrigger("Mining");

            callback?.Invoke();
        }
    }
}
