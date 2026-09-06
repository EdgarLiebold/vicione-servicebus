using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Provides mutable job metadata with explicit merge semantics.</summary>
public interface ISetPropertyCollection :
    IPropertyCollection
{
    /// <summary>Sets, conditionally preserves, or removes a property.</summary>
    /// <param name="key">The case-insensitive property key.</param>
    /// <param name="value">The value to store, or <see langword="null" /> to remove an existing property.</param>
    /// <param name="overwrite"><see langword="true" /> to replace an existing value; <see langword="false" /> to preserve it.</param>
    /// <returns>This property collection.</returns>
    ISetPropertyCollection Set(string key, object? value, bool overwrite = true);

    /// <summary>Merges properties into the collection; null values remove their corresponding keys.</summary>
    /// <param name="properties">The properties to merge.</param>
    /// <param name="overwrite"><see langword="true" /> to replace existing values; <see langword="false" /> to preserve them.</param>
    /// <returns>This property collection.</returns>
    ISetPropertyCollection SetMany(IEnumerable<KeyValuePair<string, object?>> properties, bool overwrite = true);
}
