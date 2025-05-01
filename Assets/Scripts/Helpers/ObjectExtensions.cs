using System;
using System.Runtime.CompilerServices;
using Object = UnityEngine.Object;

namespace MineArena.Helpers
{
    public static class ObjectExtensions
    {
        private static readonly Func<Object, bool> IsNativeObjectAlive;

        static ObjectExtensions()
        {
            IsNativeObjectAlive = ReflectionHelper.CreateDelegate<Func<Object, bool>>(typeof(Object), nameof(IsNativeObjectAlive));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNullOrDead(this object self)
        {
            return !self.IsAlive();
        }

        public static bool IsAlive(this object self)
        {
            switch (self)
            {
                case null:
                    return false;
                case Object derivedObject:
                    return IsNativeObjectAlive(derivedObject);
                default:
                    return true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Cast<T>(this object self)
        {
            return (T)self;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryCast<T>(this object self, out T value) where T : class
        {
            value = self as T;
            return value != default;
        }
    }
}