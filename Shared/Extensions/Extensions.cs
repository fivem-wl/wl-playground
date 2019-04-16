using System;
using System.Collections.Generic;
using System.Text;

namespace Extensions
{
    public static class Extensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default)
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary));

            if (key == null)
                throw new ArgumentNullException(nameof(key));

            return dictionary.TryGetValue(key, out var value) ? value : defaultValue;
        }
    }
}
