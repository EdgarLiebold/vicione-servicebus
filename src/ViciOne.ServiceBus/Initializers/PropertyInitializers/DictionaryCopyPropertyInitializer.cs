using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Initializers.PropertyInitializers;

/// <summary>
/// Gets the dictionary entry for the property (if present), and sets the message property to the value
/// </summary>
/// <typeparam name="TMessage"></typeparam>
/// <typeparam name="TInput"></typeparam>
/// <typeparam name="TProperty"></typeparam>
public class DictionaryCopyPropertyInitializer<TMessage, TInput, TProperty> :
    IPropertyInitializer<TMessage, TInput>
    where TMessage : class
    where TInput : class, IDictionary<string, TProperty>
{
    readonly string _key;
    readonly IWriteProperty<TMessage, TProperty> _messageProperty;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="propertyInfo">The property info value.</param>
    /// <param name="key">The key value.</param>
    public DictionaryCopyPropertyInitializer(PropertyInfo propertyInfo, string key)
    {
        if (propertyInfo == null)
            throw new ArgumentNullException(nameof(propertyInfo));

        _key = key;
        _messageProperty = WritePropertyCache<TMessage>.GetProperty<TProperty>(propertyInfo);
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task ApplyAsync(InitializeContext<TMessage, TInput> context, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); if (context.HasInput && context.Input.TryGetValue(_key, out var value))
            _messageProperty.Set(context.Message, value);

        return Task.CompletedTask;
    }
}
