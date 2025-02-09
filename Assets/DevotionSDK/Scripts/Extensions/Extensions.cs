using Devotion.Helpers;
using System;

namespace Devotion.SDK.Extensions
{
    public static class Extensions
    {
        public static T GetEnum<T>(this string enumName, bool ignoreCast = true) where T : Enum
        {
            if (enumName == "null" || enumName.IsNullOrEmpty())
                return default;

            return (T)Enum.Parse(typeof(T), enumName, ignoreCast);
        }
    }
}