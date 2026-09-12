using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Initializers.PropertyInitializers;

/// <summary>Copies an available string-keyed dictionary entry into a writable message property.</summary>
/// <typeparam name="TMessage">The message contract being populated.</typeparam>
/// <typeparam name="TInput">The string-keyed input dictionary.</typeparam>
/// <typeparam name="TProperty">The dictionary and message property value type.</typeparam>
internal sealed class DictionaryCopyPropertyInitializer<TMessage, TInput, TProperty> :
    IPropertyInitializer<TMessage, TInput>
    where TMessage : class
    where TInput : class, IDictionary<string, TProperty>
{
    readonly string _key;
    readonly IWriteProperty<TMessage, TProperty> _messageProperty;

    /// <summary>Creates a mapping from a named dictionary entry to a message property.</summary>
    /// <param name="propertyInfo">The writable message property.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    public DictionaryCopyPropertyInitializer(PropertyInfo propertyInfo, string key)
    {
        if (propertyInfo == null)
            throw new ArgumentNullException(nameof(propertyInfo));
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        _key = key;
        _messageProperty = WritePropertyCache<TMessage>.GetProperty<TProperty>(propertyInfo);
    }

    /// <summary>Copies the keyed dictionary value when the input contains it.</summary>
    /// <param name="context">The message and input dictionary.</param>
    /// <param name="cancellationToken">The token that cancels property assignment.</param>
    /// <returns>A task that completes after the available value has been assigned.</returns>
    public Task ApplyAsync(InitializeContext<TMessage, TInput> context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);
        if (context.HasInput && context.Input.TryGetValue(_key, out var value))
            _messageProperty.Set(context.Message, value);

        return Task.CompletedTask;
    }
}
