using UnityEngine;

namespace Devotion.Interfaces
{
    public interface ITriggerAction
    {
        void Execute(GameObject target);
    }
    public abstract class TriggerAction : ScriptableObject, ITriggerAction
    {
        public abstract void Execute(GameObject target);
    }
}