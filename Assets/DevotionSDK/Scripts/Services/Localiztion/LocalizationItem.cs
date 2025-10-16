using System;

namespace Devotion.SDK.Services.Localization
{
    [Serializable]
    public class LocalizationItem
    {
        public string Key;
        public string Value;

        public string key;
        public string value;

        public string ResolveKey() => !string.IsNullOrEmpty(Key) ? Key : key;
        public string ResolveValue() => !string.IsNullOrEmpty(Value) ? Value : value;
    }
}
