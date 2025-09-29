using System.Collections.Generic;
using MineArena.Items;
using MineArena.Messages.MessageService;
using MineArena.PlayerSystem;
using MineArena.UI.FortuneWheel;
using Quests;
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

    public static partial class QuestMessages
    {
        public sealed class QuestTargetTaken : BaseMessage<QuestTargetTaken, (IQuestTarget, int)>
        {
        }

        public sealed class PrizeTake : BaseMessage<PrizeTake, Quest>
        {
        }

        public sealed class QuestCompleted : BaseMessage<QuestCompleted, Quest>
        {
        }

        public sealed class QuestBegun : BaseMessage<QuestBegun, Quest>
        {
        }
    }
}