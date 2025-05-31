using Devotion.SDK.Controllers;
using MineArena.Managers;
using UnityEngine;
using MineArena.Commands;
using Devotion.SDK.Helpers;

namespace MineArena.Items
{
    [RequireComponent(typeof(BillboardCanvas))]
    public class InteractableObject : MonoBehaviour
    {
        [SerializeField] private BaseCommand _command;
        [SerializeField] private float _interactionRange = 3.0f;

        private BillboardCanvas _canvas;

        private void Awake()
        {
            _canvas = GetComponent<BillboardCanvas>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.IsPlayer())
            {
                GameRoot.Instance.GetManager<InteractionManager>().RegisterObject(this);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.IsPlayer())
            {
                GameRoot.Instance.GetManager<InteractionManager>().UnregisterObject(this);

                //HideInteractionPrompt();
            }
        }

        private void OnDestroy()
        {
            GameRoot.Instance.GetManager<InteractionManager>().UnregisterObject(this);
        }

        public void ShowInteractionPrompt()
        {
            _canvas.ShowUI();
        }

        public void HideInteractionPrompt()
        {
            _canvas.HideUI();
        }

        public void ExecuteCommand()
        {
            _command?.Execute(() => { Destroy(gameObject); });
        }
    }
}