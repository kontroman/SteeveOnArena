using System.Collections.Generic;
using System;

namespace Devotion.SDK.Services.Localization
{
    [Serializable]
    public class LocalizationData
    {
        public List<LocalizationItem> items = new List<LocalizationItem>();

        public Dictionary<string, string> ToDictionary()
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();

            if (items != null)
            {
                foreach (var item in items)
                {
                    dictionary[item.key] = item.value;
                }
            }

            return dictionary;
        }
    }
}