using Devotion.SDK.Controllers;
using MineArena.Managers;
using UnityEngine;
using MineArena.Commands;
using Devotion.SDK.Helpers;
using MineArena.Drop;
using MineArena.VFX;

namespace MineArena.Items
{
    [RequireComponent(typeof(BillboardCanvas))]
    public class InteractableObject : MonoBehaviour
    {
        [SerializeField] private BaseCommand _command;
        [SerializeField] private float _interactionRange = 3.0f;

        [SerializeField] private bool _destroyOnEnd = true;
        [SerializeField] private VfxId _completeInteractionVfxId = VfxId.EndDig;
        [SerializeField] private Vector3 _completeInteractionVfxOffset = new Vector3(0f, 0.5f, 0f);

        private bool _used;

        private BillboardCanvas _canvas;

        private Dropable _dropable;

        private void Awake()
        {
            _canvas = GetComponent<BillboardCanvas>();
            _dropable = GetComponent<Dropable>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.IsPlayer() && !_used)
            {
                GameRoot.GetManager<InteractionManager>().RegisterObject(this);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.IsPlayer())
            {
                GameRoot.GetManager<InteractionManager>().UnregisterObject(this);

                //HideInteractionPrompt(); 
            }
        }

        private void OnDestroy()
        {
            GameRoot.GetManager<InteractionManager>().UnregisterObject(this);
        }

        public void ShowInteractionPrompt()
        {
            _canvas.ShowUI();
        }

        public void HideInteractionPrompt()
        {
            _canvas.HideUI();
        }

        public async void ExecuteCommand()
        {
            if (_used) return;

            _used = true;

            await _command.Execute(this);
        }

        public void CompleteInteraction()
        {
            GameRoot.GetManager<InteractionManager>().UnregisterObject(this);
            HideInteractionPrompt();

            if (_destroyOnEnd)
            {
                PlayCompleteInteractionVfx();
                _dropable?.DropItems();
                Destroy(gameObject);
            }
        }

        private void PlayCompleteInteractionVfx()
        {
            if (_completeInteractionVfxId == VfxId.None)
                return;

            var vfxManager = GameRoot.GetManager<VFXManager>();
            if (vfxManager == null)
                return;

            vfxManager.Play(_completeInteractionVfxId, GetEffectPosition() + _completeInteractionVfxOffset, Quaternion.identity);
        }

        private Vector3 GetEffectPosition()
        {
            if (TryGetComponent<Renderer>(out var renderer))
                return renderer.bounds.center;

            renderer = GetComponentInChildren<Renderer>();
            if (renderer != null)
                return renderer.bounds.center;

            if (TryGetComponent<Collider>(out var collider))
                return collider.bounds.center;

            collider = GetComponentInChildren<Collider>();
            return collider != null ? collider.bounds.center : transform.position;
        }
    }
}
