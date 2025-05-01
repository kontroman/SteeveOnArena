using MineArena.Interfaces;
using UnityEngine;

namespace MineArena.TriggersActions
{
    public abstract class TriggerAction : ScriptableObject, ITriggerAction
    {
        public abstract void Execute(GameObject target);
    }
}