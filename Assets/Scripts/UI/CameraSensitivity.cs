using UnityEngine;

namespace MineArena.UI
{
    public static class CameraSensitivity
    {
        public const string PreferenceKey = "UI.ZoomSensitivity";
        public const float Minimum = 0.25f, Maximum = 3f;
        public static float Value => Mathf.Clamp(PlayerPrefs.GetFloat(PreferenceKey, 1f), Minimum, Maximum);
        public static float Apply(float scroll, float step) => scroll * step * Value;
    }
}
