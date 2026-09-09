using MineArena.Items;
using Devotion.SDK.Managers;
using System.Collections.Generic;
using UnityEngine;
using MineArena.Controllers;

namespace MineArena.Managers
{
    [DefaultExecutionOrder(-100)]
    public class InteractionManager : BaseManager
    {
        private List<InteractableObject> _nearbyObjects = new List<InteractableObject>();
        private InteractableObject _currentInteractable;
        private InteractableObject _miningTarget;
        private System.Action _cancelMining;

        public void BeginMining(InteractableObject target, System.Action cancel)
        {
            _miningTarget = target;
            _cancelMining = cancel;
        }

        public void EndMining()
        {
            _miningTarget = null;
            _cancelMining = null;
        }

        private void OnDisable() => _cancelMining?.Invoke();

        public Transform CurrentTargetTransform { get { return _currentInteractable.gameObject.transform; } }

        private void Update()
        {
            if (_cancelMining != null)
            {
                bool attack = Input.GetMouseButtonDown(0) &&
                    (UnityEngine.EventSystems.EventSystem.current == null ||
                     !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject());
                if (_miningTarget == null || MineArena.PlayerSystem.PlayerMovement.IsPlayerDead ||
                    Time.timeScale == 0 || TutorialService.BlocksInput || Input.GetKeyDown(KeyCode.E) ||
                    Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f ||
                    Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.1f || Input.GetButtonDown("Jump") || attack)
                    _cancelMining.Invoke();
                return;
            }
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
