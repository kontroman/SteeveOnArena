using UnityEngine;
using MineArena.Controllers;
using MineArena.PlayerSystem;
using System;
using System.Threading.Tasks;
using DG.Tweening;
using MineArena.Items;
using Devotion.SDK.Helpers;

namespace MineArena.Commands
{
    [CreateAssetMenu(fileName = "New UseItemCommand", menuName = "Commands/MineCommand")]
    public class MineCommand : BaseCommand
    {
        [SerializeField] private string _miningStateName = "PlayerMiningAnimation";
        [SerializeField] private int _miningLayer = 0;

        public override async Task Execute(Component component)
        {
            var interactable = component as InteractableObject;
            Transform ore = interactable.transform;

            PlayerMovement pm = Player.Instance.GetComponentFromList<PlayerMovement>();
            PlayerAttack patc = Player.Instance.GetComponentFromList<PlayerAttack>();
            RotationController rc = Player.Instance.GetComponentFromList<RotationController>();

            var pa = Player.Instance.GetComponentFromList<PlayerAnimatorController>() ??
                     Player.Instance.GetComponent<IPlayerAnimator>();

            PlayerEquipment equipment = Player.Instance.GetComponentFromList<PlayerEquipment>();

            pm.SetMovement(false);
            patc.SetComponentEnable(false);
            rc.RotatePlayerToTarget(ore);

            pa?.SetRunning(false);
            equipment?.SetActiveHandItem(HandItemType.Pickaxe);

            float miningDuration = equipment?.GetMiningDuration() ?? 3.33f;
            int miningLoops = equipment?.GetMiningLoops() ?? 2;

            for (int i = 0; i < miningLoops; i++)
            {
                pa?.PlayMiningAnimation(_miningStateName, _miningLayer);

                CoroutineHelper.Delay(0.7f, () =>
                {
                    ore.DOShakeScale(0.25f, 0.25f, 8, 90);
                });

                await Task.Delay(TimeSpan.FromSeconds(miningDuration));
            }

            pm.SetMovement(true);
            pa?.ResetMiningAnimation();
            patc.SetComponentEnable(true);

            interactable.CompleteInteraction();
        }
    }
}
