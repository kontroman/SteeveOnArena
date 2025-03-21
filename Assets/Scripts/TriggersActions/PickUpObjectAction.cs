using Devotion.Interfaces;
using Devotion.Items;
using UnityEngine;

namespace Devotion.TriggersActions
{
    [CreateAssetMenu(fileName = "PickUpObjectAction", menuName = "Trigger Actions/Pick Up Object")]
    public class PickUpObjectAction : TriggerAction
    {
        public override void Execute(GameObject go)
        {
            if(go.TryGetComponent<ItemInteractor>(out var itemInteractor))
            {
                itemInteractor.Interact();
            }
        }
    }
}