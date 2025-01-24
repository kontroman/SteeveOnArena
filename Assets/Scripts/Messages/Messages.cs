using Devotion.Messages.MessageService;

namespace Devotion.Messages
{
    public static partial class Game
    {
        public sealed class GameStarted: BaseMessage<GameStarted, string>
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