using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Provides case-insensitive, typed access to job metadata.</summary>
public interface IPropertyCollection :
    IReadOnlyDictionary<string, object>
{
    /// <summary>Attempts to return a property without applying type conversion.</summary>
    /// <param name="key">The case-insensitive property key.</param>
    /// <param name="value">Receives the stored value when present.</param>
    /// <returns><see langword="true" /> when the property exists; otherwise, <see langword="false" />.</returns>
    bool TryGet(string key, [NotNullWhen(true)] out object? value);

    /// <summary>Returns a reference-type property or the supplied default when no compatible value exists.</summary>
    /// <typeparam name="TValue">The expected property type.</typeparam>
    /// <param name="key">The case-insensitive property key.</param>
    /// <param name="defaultValue">The value returned when the property is absent or incompatible.</param>
    /// <returns>The converted property value or <paramref name="defaultValue" />.</returns>
    TValue? Get<TValue>(string key, TValue? defaultValue = default)
        where TValue : class;

    /// <summary>Returns a value-type property or the supplied default when no compatible value exists.</summary>
    /// <typeparam name="TValue">The expected property type.</typeparam>
    /// <param name="key">The case-insensitive property key.</param>
    /// <param name="defaultValue">The value returned when the property is absent or incompatible.</param>
    /// <returns>The converted property value or <paramref name="defaultValue" />.</returns>
    TValue? Get<TValue>(string key, TValue? defaultValue = default)
        where TValue : struct;
}
