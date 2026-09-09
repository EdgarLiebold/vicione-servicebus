using System.Collections.Generic;
using System.Threading;

namespace ViciOne.ServiceBus.Operations;

/// <summary>Collects named diagnostic values and child scopes for a component snapshot.</summary>
public interface ProbeContext
{
    /// <summary>Gets the token that cancels collection of the diagnostic snapshot.</summary>
    CancellationToken CancellationToken { get; }

    /// <summary>Sets a string value or removes the value when <paramref name="value"/> is null or empty.</summary>
    /// <param name="key">The non-empty diagnostic key.</param>
    /// <param name="value">The value to store, or null or empty to remove the key.</param>
    void Add(string key, string? value);

    /// <summary>Sets a value or removes it when <paramref name="value"/> is null or an empty string.</summary>
    /// <param name="key">The non-empty diagnostic key.</param>
    /// <param name="value">The value to store, or null or an empty string to remove the key.</param>
    void Add(string key, object? value);

    /// <summary>Copies the readable properties of an object into the current scope.</summary>
    /// <param name="values">The object whose readable properties provide diagnostic values.</param>
    void Set(object values);

    /// <summary>Copies the supplied key/value pairs into the current scope.</summary>
    /// <param name="values">The diagnostic values to copy.</param>
    void Set(IEnumerable<KeyValuePair<string, object?>> values);

    /// <summary>Creates and appends a child scope under a diagnostic key.</summary>
    /// <param name="key">The non-empty key that groups scopes of the same kind.</param>
    /// <returns>A context for the new child scope.</returns>
    ProbeContext CreateScope(string key);
}
