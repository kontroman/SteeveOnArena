using UnityEngine;
using Devotion.SDK.Controllers;
using MineArena.Controllers;
using MineArena.PlayerSystem;
using System.Threading.Tasks;
using DG.Tweening;
using MineArena.Items;
using Devotion.SDK.Helpers;
using MineArena.VFX;
using MineArena.Basics;
using MineArena.Managers;

namespace MineArena.Commands
{
    [CreateAssetMenu(fileName = "New UseItemCommand", menuName = "Commands/MineCommand")]
    public class MineCommand : BaseCommand
    {
        [SerializeField] private string _miningStateName = "PlayerMiningAnimation";
        [SerializeField] private int _miningLayer = 0;
        [SerializeField] private VfxId _digVfxId = VfxId.Dig;
        [SerializeField] private Vector3 _digVfxOffset = new Vector3(0f, 0.5f, 0f);

        public override async Task Execute(Component component)
        {
            var interactable = component as InteractableObject;
            if (interactable == null)
                return;

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
                    if (ore == null)
                        return;

                    PlayDigVfx(ore);
                    GameRoot.GetManager<AudioManager>()?.PlayEffect(Constants.AudioNames.MiningHit);
                    ore.DOShakeScale(0.25f, 0.25f, 8, 90);
                });

                await CoroutineHelper.DelayAsync(miningDuration);
            }

            pm.SetMovement(true);
            pa?.ResetMiningAnimation();
            patc.SetComponentEnable(true);

            interactable.CompleteInteraction();
        }

        private void PlayDigVfx(Transform target)
        {
            if (target == null)
                return;

            var vfxManager = GameRoot.GetManager<VFXManager>();
            if (vfxManager == null)
                return;

            vfxManager.Play(_digVfxId, GetEffectPosition(target) + _digVfxOffset, Quaternion.identity);
        }

        private static Vector3 GetEffectPosition(Transform target)
        {
            if (target.TryGetComponent<Renderer>(out var renderer))
                return renderer.bounds.center;

            renderer = target.GetComponentInChildren<Renderer>();
            if (renderer != null)
                return renderer.bounds.center;

            if (target.TryGetComponent<Collider>(out var collider))
                return collider.bounds.center;

            collider = target.GetComponentInChildren<Collider>();
            return collider != null ? collider.bounds.center : target.position;
        }
    }
}
