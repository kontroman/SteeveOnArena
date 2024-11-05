using UnityEngine;
using System.Threading.Tasks;
using System;

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
    }
}
