using Devotion.Messages.MessageService;
using UnityEngine;

namespace Devotion.Messages
{
    public static partial class Game
    {
        public sealed class GameStarted: BaseMessage<GameStarted, string>
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
}