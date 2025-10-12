using System.Collections.Generic;
using Achievements;
using MineArena.Items;
using MineArena.Messages.MessageService;
using MineArena.PlayerSystem;
using MineArena.UI.FortuneWheel;
using UnityEngine;

namespace MineArena.Messages
{
    public static partial class Game
    {
        public sealed class GameStarted : BaseMessage<GameStarted, string>
        {
        }

        public sealed class LanguageChanged : BaseMessage<LanguageChanged, SystemLanguage>
        {
        }
    }

    public static partial class UIMessages
    {
        public sealed class OpenWindow : BaseMessage<OpenWindow, string>
        {
        }
    }

    public static partial class GameMessages
    {
        public sealed class NewSwordEquiped : BaseMessage<NewSwordEquiped, AttackConfig>
        {
        }

        public sealed class WorldChestOpened : BaseMessage<WorldChestOpened, ItemPrize>
        {
        }
    }

    public static partial class AchievementMessages
    {
        public sealed class AchievementTargetTaken : BaseMessage<AchievementTargetTaken, (IAchievementTarget, int)>
        {
        }

        public sealed class PrizeTake : BaseMessage<PrizeTake, Achievement>
        {
        }

        public sealed class AchievementCompleted : BaseMessage<AchievementCompleted, Achievement>
        {
        }

        public sealed class AchievementBegun : BaseMessage<AchievementBegun, Achievement>
        {
        }
    }
}