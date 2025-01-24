using Devotion.Messages.MessageService;
using Devotion.Messages;
using UnityEngine;

public class TestMessageReciever : MonoBehaviour,
    IMessageSubscriber<Game.GameStarted>
{
    public void OnEnable()
    {
        MessageService.Subscribe(this);
    }

    public void OnDisable()
    {
        MessageService.Unsubscribe(this);
    }

    public void OnMessage(Game.GameStarted message)
    {
        Debug.LogError(message.Model.ToString());
    }
}
