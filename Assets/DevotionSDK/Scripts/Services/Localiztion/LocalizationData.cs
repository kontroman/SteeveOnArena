using System.Collections.Generic;
using System;

namespace Devotion.SDK.Services.Localization
{
    [Serializable]
    public class LocalizationData
    {
        public List<LocalizationItem> _items = new List<LocalizationItem>();

        public Dictionary<string, string> ToDictionary()
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();

            if (_items != null)
            {
                foreach (var item in _items)
                {
                    dictionary[item.Key] = item.Value;
                }
            }

            return dictionary;
        }
    }
}