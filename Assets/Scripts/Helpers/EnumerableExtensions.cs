using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Devotion.Helpers
{
    public static class EnumerableExtensions
    {
        public static bool IsNullOrEmpty(this IEnumerable self)
        {
            if (self is null)
                return true;

            if (self is ICollection collection)
                return collection.Count == 0;

            return !self.GetEnumerator().MoveNext();
        }

        public static Dictionary<string, object> Add(this Dictionary<string, object> mainDic, Dictionary<string, object> add)
        {
            if (mainDic.IsNullOrEmpty())
                mainDic = new Dictionary<string, object>();
            if (add.IsNullOrEmpty())
                return mainDic;

            foreach (var param in add.Where(param => !mainDic.ContainsKey(param.Key)))
                mainDic.Add(param.Key, param.Value);

            return mainDic;
        }
    }
}