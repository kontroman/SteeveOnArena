using Devotion.Helpers;
using Devotion.Messages.MessageService;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Devotion.Messages.MessageService
{
    public abstract class BaseMessage<T> : IMessage
            where T : BaseMessage<T>, IMessage, new()
    {
        private static Stack<T> _messages = null;

        protected static Stack<T> Messages => _messages ?? (_messages = new Stack<T>());

        public static void Publish()
        {
            var message = GetMessage();
            MessageService.Publish(message);
            Messages.Push(message);
        }

        protected static T GetMessage()
        {
            var message = Messages.Count > 0 ? Messages.Pop() : new T();
            return message;
        }
    }

    public abstract class BaseMessage<T, TModel> : BaseMessage<T>
        where T : BaseMessage<T, TModel>, IMessage, new()
    {
        public TModel Model { get; private set; }

        public static void Publish(in TModel model)
        {
            var message = GetMessage();
            message.Model = model;

            MessageService.Publish(message);

            var messageType = typeof(T).FullName;

            var gameObjectName = "";
            if (!model.IsNullOrDead()
                && model.GetType().IsClass
                && (model is UnityEngine.GameObject))
            {
                gameObjectName = (model as UnityEngine.GameObject).name;
            }

            message.Model = default;
            Messages.Push(message);
        }
    }
}