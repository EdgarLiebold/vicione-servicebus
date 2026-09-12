using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Initializers.PropertyProviders;

/// <summary>Reads a named value from a dictionary input.</summary>
/// <typeparam name="TInput">The input type.</typeparam>
/// <typeparam name="TProperty">The property type.</typeparam>
internal sealed class InputDictionaryPropertyProvider<TInput, TProperty> :
    IPropertyProvider<TInput, TProperty>
    where TInput : class, IDictionary<string, TProperty>
{
    readonly string _key;

    /// <summary>Creates a provider for one dictionary key.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    public InputDictionaryPropertyProvider(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        _key = key;
    }

    /// <inheritdoc />
    public Task<TProperty?> GetPropertyAsync<T>(InitializeContext<T, TInput> context, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<TProperty?>(cancellationToken);

        if (context.HasInput && context.Input.TryGetValue(_key, out var value))
            return Task.FromResult<TProperty?>(value);

        return TaskResults.DefaultAsync<TProperty>(cancellationToken: cancellationToken);
    }
}
