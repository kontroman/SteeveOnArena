using Devotion.Controllers;
using Devotion.Items;
using Devotion.SDK.Managers;
using System.Collections.Generic;
using UnityEngine;

namespace Devotion.Managers
{
    public class InteractionManager : BaseManager
    {
        private List<InteractableObject> _nearbyObjects = new List<InteractableObject>();
        private InteractableObject _currentInteractable;

        public Transform CurrentTargetTransform { get { return _currentInteractable.gameObject.transform; } }

        private void Update()
        {
            UpdateClosestObject();

            if (_currentInteractable != null && Input.GetKeyDown(KeyCode.E))
            {
                _currentInteractable?.HideInteractionPrompt();
                _currentInteractable.ExecuteCommand();
            }
        }

        public void RegisterObject(InteractableObject interactable)
        {
            if (!_nearbyObjects.Contains(interactable))
                _nearbyObjects.Add(interactable);

        }

        public void UnregisterObject(InteractableObject interactable)
        {
            if (_nearbyObjects.Contains(interactable))
                _nearbyObjects.Remove(interactable);
        }

        private void UpdateClosestObject()
        {

            float closestDistance = float.MaxValue;

            InteractableObject closest = null;

            foreach (var interactable in _nearbyObjects)
            {
                float distance = Vector3.Distance(interactable.transform.position, Player.Instance.transform.position);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = interactable;
                }
            }

            if (_currentInteractable != closest)
            {
                _currentInteractable?.HideInteractionPrompt();
                _currentInteractable = closest;
                _currentInteractable?.ShowInteractionPrompt();
            }
        }
    }
}