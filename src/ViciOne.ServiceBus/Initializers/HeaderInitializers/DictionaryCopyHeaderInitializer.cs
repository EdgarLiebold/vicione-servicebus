using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Initializers.HeaderInitializers;

/// <summary>Initializes dictionary copy header values.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <typeparam name="TInput">The input type.</typeparam>
/// <typeparam name="THeader">The header type.</typeparam>
public class DictionaryCopyHeaderInitializer<TMessage, TInput, THeader> :
    IHeaderInitializer<TMessage, TInput>
    where TMessage : class
    where TInput : class, IDictionary<string, THeader>
{
    readonly IWriteProperty<SendContext, THeader> _headerProperty;
    readonly string _key;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="propertyInfo">The property info.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    public DictionaryCopyHeaderInitializer(PropertyInfo propertyInfo, string key)
    {
        if (propertyInfo == null)
            throw new ArgumentNullException(nameof(propertyInfo));

        _key = key;
        _headerProperty = WritePropertyCache<SendContext>.GetProperty<THeader>(propertyInfo);
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="sendContext">The send context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ApplyAsync(InitializeContext<TMessage, TInput> context, SendContext sendContext, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); if (context.HasInput && context.Input.TryGetValue(_key, out var value))
            _headerProperty.Set(sendContext, value);

        return Task.CompletedTask;
    }
}
