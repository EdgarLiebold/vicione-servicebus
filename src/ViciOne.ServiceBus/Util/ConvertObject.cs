using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace ViciOne.ServiceBus.Util;

/// <summary>Projects public readable properties into dictionaries, lowercasing the first key character when uppercase.</summary>
public static class ConvertObject
{
    /// <summary>Converts this value to dictionary.</summary>
    /// <param name="values">The values.</param>
    /// <returns>The converted dictionary.</returns>
    public static Dictionary<string, object> ToDictionary(object? values)
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        var dictionary = new Dictionary<string, object>();

        IEnumerable<PropertyInfo> properties = MessageTypeCache
            .GetProperties(values.GetType())
            .Where(x => x.CanRead && x.GetMethod is { IsPublic: true });

        foreach (var property in properties)
            AddPropertyToDictionary(property, values, dictionary);

        return dictionary;
    }

    static void AddPropertyToDictionary(PropertyInfo property, object source, IDictionary<string, object> dictionary)
    {
        var value = property.GetValue(source);

        var key = property.Name;
        if (char.IsUpper(key[0]))
        {
            var chars = key.ToCharArray();
            chars[0] = char.ToLower(chars[0], CultureInfo.InvariantCulture);

            key = new string(chars);
        }

        dictionary.Add(key, value!);
    }
}
