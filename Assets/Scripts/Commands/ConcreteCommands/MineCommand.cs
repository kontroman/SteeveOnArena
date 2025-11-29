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
        [SerializeField] private string _miningStateName = "PlayerMiningAnimation";
        [SerializeField] private int _miningLayer = 0;

        public override async Task Execute(Action callback)
        {
            PlayerMovement pm = Player.Instance.GetComponentFromList<PlayerMovement>();
            PlayerAttack patc = Player.Instance.GetComponentFromList <PlayerAttack>();
            RotationController rc = Player.Instance.GetComponentFromList<RotationController>();
            Transform ore = GameRoot.GetManager<InteractionManager>().CurrentTargetTransform;
            Animator pa = Player.Instance.GetComponentFromList<Animator>();
            PlayerEquipment equipment = Player.Instance.GetComponentFromList<PlayerEquipment>();

            pm.SetMovement(false);
            patc.SetComponentEnable(false);
            rc.RotatePlayerToTarget(ore);

            pa.SetBool("isRunning", false);
            equipment?.SetActiveHandItem(HandItemType.Pickaxe);

            float miningDuration = equipment?.GetMiningDuration() ?? 3.33f;
            int miningLoops = equipment?.GetMiningLoops() ?? 2;

            for (int i = 0; i < miningLoops; i++)
            {
                if (!string.IsNullOrWhiteSpace(_miningStateName))
                    pa.Play(_miningStateName, _miningLayer, 0f);
                else
                    pa.SetTrigger("Mining");

                await Task.Delay(TimeSpan.FromSeconds(miningDuration));
            }

            pm.SetMovement(true);
            pa.ResetTrigger("Mining");
            patc.SetComponentEnable(true);
            callback?.Invoke();
        }
    }
}
