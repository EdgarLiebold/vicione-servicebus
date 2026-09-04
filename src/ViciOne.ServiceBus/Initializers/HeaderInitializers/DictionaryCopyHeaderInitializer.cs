using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Initializers.HeaderInitializers;

/// <summary>
/// Provides a dictionary copy header initializer implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
/// <typeparam name="TInput">The t input type.</typeparam>
/// <typeparam name="THeader">The t header type.</typeparam>
public class DictionaryCopyHeaderInitializer<TMessage, TInput, THeader> :
    IHeaderInitializer<TMessage, TInput>
    where TMessage : class
    where TInput : class, IDictionary<string, THeader>
{
    readonly IWriteProperty<SendContext, THeader> _headerProperty;
    readonly string _key;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="propertyInfo">The property info value.</param>
    /// <param name="key">The key value.</param>
    public DictionaryCopyHeaderInitializer(PropertyInfo propertyInfo, string key)
    {
        if (propertyInfo == null)
            throw new ArgumentNullException(nameof(propertyInfo));

        _key = key;
        _headerProperty = WritePropertyCache<SendContext>.GetProperty<THeader>(propertyInfo);
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="sendContext">The send context value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task ApplyAsync(InitializeContext<TMessage, TInput> context, SendContext sendContext, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); if (context.HasInput && context.Input.TryGetValue(_key, out var value))
            _headerProperty.Set(sendContext, value);

        return Task.CompletedTask;
    }
}
