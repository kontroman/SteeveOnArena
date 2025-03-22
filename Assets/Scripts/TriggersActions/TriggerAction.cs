using Devotion.Interfaces;
using UnityEngine;

namespace Devotion.TriggersActions
{
    public abstract class TriggerAction : ScriptableObject, ITriggerAction
    {
        public abstract void Execute(GameObject target);
    }
}