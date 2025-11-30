using Devotion.SDK.Services.Localization;
using MineArena.Messages;
using MineArena.Messages.MessageService;
using MineArena.Windows.InfoPopup;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Devotion.SDK.Controllers
{
    public class MessagesListener : MonoBehaviour,
        IMessageSubscriber<MineArena.Messages.GameMessages.WorldChestOpened>
    {
        private void OnEnable()
        {
            MessageService.Subscribe(this);           
        }

        private void OnDisable()
        {
            MessageService.Unsubscribe(this);
        }

        public void OnMessage(GameMessages.WorldChestOpened message)
        {
            InfoPopupWindow window = (InfoPopupWindow)GameRoot.UIManager.ShowWindow<InfoPopupWindow>();

            window.Setup(message.Model);
        }
    }
}
