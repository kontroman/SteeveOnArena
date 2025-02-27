using UnityEngine;
using System.Threading.Tasks;
using System;
using Divotion.Game.Health;

namespace Devotion.Commands
{
    [CreateAssetMenu(fileName = "New UseItemCommand", menuName = "Commands/HealCommand")]
    public class HealCommand : BaseCommand
    {
        [SerializeField] private int _healthIncreaseAmount;

        public override async Task Execute(Action callback)
        {
            //TODO: make heal after creating health system

            Debug.Log($"Executing command to increase health by {_healthIncreaseAmount}");
        }

        public override async Task Execute(Component component)
        {
            if (component.TryGetComponent(out Health health))
                health.ChangeValue(_healthIncreaseAmount);
        }
    }
}
