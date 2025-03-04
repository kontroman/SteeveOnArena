using System;
using System.Collections.Generic;

namespace Devotion.SDK.Services
{
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public static void Register<T>(T service)
        {
            _services[typeof(T)] = service;
        }

        public static T Resolve<T>()
        {
            if (_services.TryGetValue(typeof(T), out var service))
            {
                return (T)service;
            }

            throw new Exception($"Service of type {typeof(T)} is not registered.");
        }
    }
}
