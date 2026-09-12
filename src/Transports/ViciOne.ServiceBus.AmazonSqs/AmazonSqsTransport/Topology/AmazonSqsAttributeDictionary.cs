using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.AmazonSqs.Topology;

/// <summary>Creates owned Amazon attribute dictionaries with provider-consistent key comparison.</summary>
internal static class AmazonSqsAttributeDictionary
{
    /// <summary>Copies subscription attributes into a dictionary that cannot contain case-variant duplicate keys.</summary>
    /// <param name="attributes">The attributes to copy, or <see langword="null" /> for an empty dictionary.</param>
    /// <returns>An independently mutable dictionary with ordinal case-insensitive keys.</returns>
    public static Dictionary<string, object> CopySubscriptionAttributes(IDictionary<string, object>? attributes)
    {
        var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        if (attributes is null)
            return result;

        foreach (KeyValuePair<string, object> attribute in attributes)
            result.Add(CanonicalizeSubscriptionAttributeName(attribute.Key), attribute.Value);

        return result;
    }

    /// <summary>Returns the provider-defined spelling for a known subscription attribute name.</summary>
    /// <param name="name">The attribute name.</param>
    /// <returns>The canonical provider name when known; otherwise, the supplied name.</returns>
    public static string CanonicalizeSubscriptionAttributeName(string name) =>
        string.Equals(name, "RawMessageDelivery", StringComparison.OrdinalIgnoreCase)
            ? "RawMessageDelivery"
            : name;
}
