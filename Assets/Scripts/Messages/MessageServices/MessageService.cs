using System.Collections.Generic;
using System.Linq;
using System;
using Devotion.Helpers;

namespace Devotion.Messages.MessageService
{
    public class MessageService : IDisposable
    {
        private readonly IDictionary<Type, List<WeakReference>> _handlers = new Dictionary<Type, List<WeakReference>>();

        private static MessageService _instance;

        private bool _isDisposed;
        private bool _isDestroying;

        public static MessageService Instance => _instance == null ? _instance = new MessageService() : _instance;

        public static void Subscribe(IMessageSubscriber subscriber)
        {
            Instance.InternalSubscribe(subscriber);
        }

        public static void Publish<TEvent>(TEvent e) where TEvent : IMessage
        {
            Instance.InternalPublish(e);
        }

        public static void Unsubscribe(IMessageSubscriber subscriber)
        {
            Instance.InternalUnsubscribe(subscriber);
        }

        private void InternalSubscribe(IMessageSubscriber subscriber)
        {
            var eventTypes = GetTypesForEvent(subscriber);

            foreach (var eventType in eventTypes)
            {
                if (_isDestroying)
                    break;

                if (!_handlers.ContainsKey(eventType))
                    _handlers[eventType] = new List<WeakReference>();

                if (_handlers[eventType].Any(w => w.Target == subscriber))
                    return;

                _handlers[eventType].Add(new WeakReference(subscriber));
            }
        }

        private void InternalPublish<TEvent>(TEvent e) where TEvent : IMessage
        {
            var eventType = e.GetType();

            if (!_handlers.TryGetValue(eventType, out var handlers))
                return;

            handlers.RemoveAll(s => !s.IsAlive);

            foreach (var handler in handlers)
            {
                if (_isDestroying)
                    break;

                InternalCall((IMessageSubscriber)handler.Target, e);
            }
        }

        private void InternalUnsubscribe(IMessageSubscriber subscriber)
        {
            var events = GetTypesForEvent(subscriber);

            if (events.IsNullOrEmpty())
                return;

            foreach (var item in events)
            {
                if (!_handlers.TryGetValue(item, out var handlers))
                    continue;
                handlers.RemoveAll(s => s != null && s.Target != null && s.Target.Equals(subscriber));
            }
        }

        private void InternalCall(object handler, IMessage e)
        {
            var typeOfEvent = e.GetType();
            var targetInterface = handler.GetType().GetInterfaces().FirstOrDefault(i => i.GenericTypeArguments.FirstOrDefault() == typeOfEvent);
            const string targetMethodName = nameof(IMessageSubscriber<IMessage>.OnMessage);
            var targetMethod = targetInterface?.GetMethod(targetMethodName);
            targetMethod?.Invoke(handler, new object[] { e });
        }

        private List<Type> GetTypesForEvent(IMessageSubscriber subscriber)
        {
            var targetInterfaces = subscriber.GetType().GetInterfaces();
            var callingInterfaces = targetInterfaces.Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMessageSubscriber<>));
            return callingInterfaces.Select(i => i.GenericTypeArguments.First()).ToList();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
                return;

            if (disposing)
            {
                _isDestroying = true;
                if (!_handlers.IsNullOrEmpty())
                {
                    _handlers.Clear();
                }
            }

            _isDisposed = true;
        }
    }
}