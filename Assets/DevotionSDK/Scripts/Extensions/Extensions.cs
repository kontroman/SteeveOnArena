using MineArena.Helpers;
using System;
using UnityEngine;
using UnityEngine.UI;

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

        public static void SetAlpha(this Image image, float alpha)
        {
            if (image == null)
                return;

            var color = image.color;
            color.a = Mathf.Clamp01(alpha);
            image.color = color;
        }

        public static void SetAlpha(this SpriteRenderer spriteRenderer, float alpha)
        {
            if (spriteRenderer == null)
                return;

            var color = spriteRenderer.color;
            color.a = Mathf.Clamp01(alpha);
            spriteRenderer.color = color;
        }
    }
}