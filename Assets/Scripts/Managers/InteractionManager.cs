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

        public bool HasMobileInteraction => _currentInteractable != null || _cancelMining != null;
        public bool IsMining => _cancelMining != null;
        public bool IsMineableTarget => _currentInteractable != null && _currentInteractable.IsMineable;

        private void Update()
        {
            if (_cancelMining != null)
            {
                bool attack = MineArena.UI.MobileGameInput.Attack &&
                    (MineArena.UI.MobileGameInput.Enabled ||
                    (UnityEngine.EventSystems.EventSystem.current == null ||
                     !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()));
                if (_miningTarget == null || MineArena.PlayerSystem.PlayerMovement.IsPlayerDead ||
                    MineArena.UI.MobileGameInput.Blocked || MineArena.UI.MobileGameInput.Interact ||
                    Mathf.Abs(MineArena.UI.MobileGameInput.Movement.x) > 0.1f ||
                    Mathf.Abs(MineArena.UI.MobileGameInput.Movement.y) > 0.1f || MineArena.UI.MobileGameInput.Jump || attack)
                    _cancelMining.Invoke();
                return;
            }
            if (MineArena.UI.MobileGameInput.Blocked || Player.Instance == null) return;
            UpdateClosestObject();

            if (_currentInteractable != null && MineArena.UI.MobileGameInput.Interact)
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
                if (interactable == null) continue;
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
