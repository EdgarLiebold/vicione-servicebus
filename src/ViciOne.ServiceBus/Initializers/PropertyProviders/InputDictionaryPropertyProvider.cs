using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Initializers.PropertyProviders;

/// <summary>
/// Copies the input property, as-is, for the property value
/// </summary>
/// <typeparam name="TInput"></typeparam>
/// <typeparam name="TProperty"></typeparam>
public class InputDictionaryPropertyProvider<TInput, TProperty> :
    IPropertyProvider<TInput, TProperty>
    where TInput : class, IDictionary<string, TProperty>
{
    readonly string _key;

    public InputDictionaryPropertyProvider(string key)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        _key = key;
    }

    public Task<TProperty?> GetPropertyAsync<T>(InitializeContext<T, TInput> context, CancellationToken cancellationToken = default)
        where T : class
    {
        if (context.HasInput && context.Input.TryGetValue(_key, out var value))
            return Task.FromResult<TProperty?>(value);

        return TaskResults.DefaultAsync<TProperty>(cancellationToken: cancellationToken);
    }
}
