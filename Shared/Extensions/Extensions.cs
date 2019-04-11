using System;
using System.Collections.Generic;
using System.Text;

namespace Extensions
{
    public static class Extensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue value)
          => dict.TryGetValue(key, out value) ? value : default(TValue);
    }
}
