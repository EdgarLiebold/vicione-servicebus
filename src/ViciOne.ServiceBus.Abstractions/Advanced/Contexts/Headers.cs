using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides the transport metadata attached to a message independently of its body.</summary>
public interface Headers :
    IEnumerable<HeaderValue>
{
    /// <summary>Enumerates all header names and their raw values.</summary>
    /// <returns>The headers in this collection.</returns>
    IEnumerable<KeyValuePair<string, object>> GetAll();

    /// <summary>Tries to obtain the raw value associated with a header name.</summary>
    /// <param name="key">The header name.</param>
    /// <param name="value">The value when the header exists.</param>
    /// <returns><see langword="true" /> when the header exists; otherwise, <see langword="false" />.</returns>
    bool TryGetHeader(string key, [NotNullWhen(true)] out object? value);

    /// <summary>Gets a reference-type header value, or a fallback when the header cannot be converted.</summary>
    /// <typeparam name="TValue">The requested header value type.</typeparam>
    /// <param name="key">The header name.</param>
    /// <param name="defaultValue">The value returned when the header is absent or incompatible.</param>
    /// <returns>The converted header value, or <paramref name="defaultValue" />.</returns>
    TValue? Get<TValue>(string key, TValue? defaultValue = default)
        where TValue : class;

    /// <summary>Gets a value-type header value, or a fallback when the header cannot be converted.</summary>
    /// <typeparam name="TValue">The requested header value type.</typeparam>
    /// <param name="key">The header name.</param>
    /// <param name="defaultValue">The value returned when the header is absent or incompatible.</param>
    /// <returns>The converted header value, or <paramref name="defaultValue" />.</returns>
    TValue? Get<TValue>(string key, TValue? defaultValue = default)
        where TValue : struct;
}
