using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Internals;

internal static class DictionaryExtensions
{
    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> valueFactory)
    {
        if (dictionary.TryGetValue(key, out var value))
            return value;

        value = valueFactory(key);
        dictionary.Add(key, value);

        return value;
    }

    public static IDictionary<string, TValue> MergeLeft<TValue>(this IDictionary<string, TValue> source, params IDictionary<string, TValue>[] others)
    {
        var result = new Dictionary<string, TValue>(source.Count, StringComparer.OrdinalIgnoreCase);

        void UpdateDictionaryWithElements(IDictionary<string, TValue> dictionary)
        {
            foreach (KeyValuePair<string, TValue> element in dictionary)
            {
                if (result.ContainsKey(element.Key))
                {
                    if (element.Value != null)
                        result[element.Key] = element.Value;
                }
                else
                    result[element.Key] = element.Value;
            }
        }

        UpdateDictionaryWithElements(source);

        foreach (IDictionary<string, TValue> dictionary in others)
            UpdateDictionaryWithElements(dictionary);

        return result;
    }

}
