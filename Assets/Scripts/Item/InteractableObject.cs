using Devotion.Controllers;
using Devotion.Managers;
using UnityEngine;

namespace Devotion.Items
{
    public class InteractableObject : MonoBehaviour
    {
        [SerializeField] private BaseCommand _command;
        [SerializeField] private float _interactionRange = 3.0f;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                GameRoot.Instance.GetManager<InteractionManager>().RegisterObject(this);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                GameRoot.Instance.GetManager<InteractionManager>().UnregisterObject(this);

                HideInteractionPrompt();
            }
        }

        public void ShowInteractionPrompt()
        {
            Debug.Log("Press E to interact with " + gameObject.name);
        }

        public void HideInteractionPrompt()
        {
            Debug.Log("Hide interaction prompt for " + gameObject.name);
        }

        public void ExecuteCommand()
        {
            _command?.Execute();
        }
    }
}