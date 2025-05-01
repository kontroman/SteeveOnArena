using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MineArena.Helpers
{
    public static class ReflectionHelper
    {
        public static IEnumerable<FieldInfo> GetAllFields(Type t, BindingFlags flags)
        {
            if (t == null)
                return null;

            var result = new List<FieldInfo>();
            while (t != null && t != typeof(System.Object))
            {
                var fields = t.GetFields(flags);
                if (!fields.IsNullOrEmpty())
                    result.AddRange(fields);

                t = t.BaseType;
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T CreateDelegate<T>(Type classType, string methodName) where T : Delegate
        {
            return Delegate.CreateDelegate(typeof(T), classType, methodName) as T;
        }

        public static IEnumerable<Type> GetInheritedClasses<TType>()
        {
            return Assembly.GetAssembly(typeof(TType)).GetTypes()
                .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(TType)))
                .OrderBy(x => x.Name);
        }

        public static IEnumerable<Type> GetInterfaceImplementationClasses<TInterface>()
        {
            return Assembly.GetAssembly(typeof(TInterface)).GetTypes()
                .Where(myType => myType.IsClass && !myType.IsAbstract && typeof(TInterface).IsAssignableFrom(myType))
                .OrderBy(x => x.Name);
        }

        public static Type GetInheritedType<TBaseType>(string type)
        {
            var baseType = typeof(TBaseType);
            return Assembly.GetAssembly(baseType).GetTypes()
                .First(myType => myType.IsClass
                                 && !myType.IsAbstract
                                 && myType.IsSubclassOf(baseType)
                                 && myType.Name == type);
        }

        public static T CreateDelegate<T>(string className, string methodName) where T : Delegate
        {
            var methodInfo = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(x => x.DefinedTypes)
                .Where(x => x.Name == className)
                .SelectMany(x => x.DeclaredMethods)
                .FirstOrDefault(x => x.Name == methodName);

            if (methodInfo == null)
                return null;

            return methodInfo.CreateDelegate(typeof(T)) as T;
        }
    }
}