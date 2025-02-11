using System;
using UnityEngine;

namespace Devotion.SDK.Helpers
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class EnumAsStringAttribute : PropertyAttribute
    {
        public readonly Type enumType;

        public EnumAsStringAttribute(Type enumType) => this.enumType = enumType;
    }
}