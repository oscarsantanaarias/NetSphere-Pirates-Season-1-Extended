using System.Collections.Generic;

namespace ProudNet
{
    internal static class CollectionCompat
    {
        internal static TValue GetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key)
        {
            return dictionary.TryGetValue(key, out var value) ? value : default;
        }
    }
}
