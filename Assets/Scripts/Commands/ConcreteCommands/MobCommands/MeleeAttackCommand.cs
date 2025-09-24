using MineArena.Game.Health;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading.Tasks;
using MineArena.Structs;
using MineArena.Controllers;
using MineArena.Interfaces;

namespace MineArena.Commands
{
    public class MeleeAttackCommand : BaseCommand
    {
        public override Task Execute(object data)
        {
            var damageCommand = ScriptableObject.CreateInstance<DamageCommand>();

            damageCommand.Execute(data);

            return Task.CompletedTask;
        }
    }
}
