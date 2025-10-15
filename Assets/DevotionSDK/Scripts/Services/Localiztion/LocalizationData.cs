using System.Collections.Generic;
using System;

namespace Devotion.SDK.Services.Localization
{
    [Serializable]
    public class LocalizationData
    {
        public List<LocalizationItem> _items = new List<LocalizationItem>();
        public List<LocalizationItem> items = new List<LocalizationItem>();

        public Dictionary<string, string> ToDictionary()
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            var source = items != null && items.Count > 0 ? items : _items;

            if (source != null)
            {
                foreach (var item in source)
                {
                    var key = item?.ResolveKey();
                    if (string.IsNullOrEmpty(key))
                    {
                        continue;
                    }

                    dictionary[key] = item.ResolveValue() ?? string.Empty;
                }
            }

            return dictionary;
        }
    }
}
