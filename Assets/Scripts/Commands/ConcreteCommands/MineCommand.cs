using UnityEngine;
using Devotion.SDK.Controllers;
using MineArena.Controllers;
using MineArena.PlayerSystem;
using System.Threading.Tasks;
using DG.Tweening;
using MineArena.Items;
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

            var pa = Player.Instance.GetComponentFromList<PlayerAnimatorController>() ??
                     Player.Instance.GetComponent<IPlayerAnimator>();

            PlayerEquipment equipment = Player.Instance.GetComponentFromList<PlayerEquipment>();

            var manager = GameRoot.GetManager<InteractionManager>();
            var previousHand = equipment != null ? equipment.LastActiveHandItem : HandItemType.Sword;
            var originalScale = ore.localScale;
            bool cancelled = false;
            bool cleanedUp = false;
            Tween shake = null;
            Tween turn = null;

            void Cleanup()
            {
                if (cleanedUp) return;
                cleanedUp = true;
                shake?.Kill();
                turn?.Kill();
                if (ore != null) ore.localScale = originalScale;
                manager?.EndMining();
                if (pm != null) pm.SetMovement(true);
                if (pa != null) pa.ResetMiningAnimation();
                if (patc != null) patc.SetComponentEnable(true);
                if (equipment != null) equipment.SetActiveHandItem(previousHand);
            }

            void Cancel()
            {
                cancelled = true;
                Cleanup();
                if (interactable != null) interactable.CancelInteraction();
            }

            try
            {
                manager.BeginMining(interactable, Cancel);
                pm.SetMovement(false);
                patc.SetComponentEnable(false);
                var direction = ore.position - pm.transform.position;
                direction.y = 0;
                if (direction.sqrMagnitude > 0.001f)
                    turn = pm.transform.DORotateQuaternion(Quaternion.LookRotation(direction), 0.5f);
                pa?.SetRunning(false);
                equipment?.SetActiveHandItem(HandItemType.Pickaxe);
                interactable.SetMiningPrompt(true);

                float miningDuration = Mathf.Max(0.01f, equipment?.GetMiningDuration() ?? 3.33f);
                int miningLoops = equipment?.GetMiningLoops() ?? 2;
                for (int i = 0; i < miningLoops && !cancelled; i++)
                {
                    pa?.PlayMiningAnimation(_miningStateName, _miningLayer);
                    float started = Time.time;
                    bool hit = false;
                    while (Time.time - started < miningDuration && !cancelled)
                    {
                        // Resume on Unity's main thread; there are no delayed hits left after cancellation.
                        await Task.Yield();
                        if (cancelled) break;
                        if (ore == null || pm == null || !pm.isActiveAndEnabled || PlayerMovement.IsPlayerDead)
                        {
                            Cancel();
                            break;
                        }
                        if (!hit && Time.time - started >= Mathf.Min(0.7f, miningDuration * 0.7f))
                        {
                            hit = true;
                            PlayDigVfx(ore);
                            GameRoot.GetManager<AudioManager>()?.PlayEffect(Constants.AudioNames.MiningHit);
                            shake?.Kill();
                            ore.localScale = originalScale;
                            shake = ore.DOShakeScale(0.25f, 0.25f, 8, 90);
                        }
                    }
                }
                Cleanup();
                if (!cancelled && interactable != null) interactable.CompleteInteraction();
            }
            catch
            {
                Cancel();
                throw;
            }
            finally
            {
                Cleanup();
            }
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
