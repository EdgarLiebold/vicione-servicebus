using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals.Reflection;

namespace ViciOne.ServiceBus.Initializers.HeaderInitializers;

/// <summary>Copies a dictionary entry into a typed send-context header property.</summary>
/// <typeparam name="TMessage">The initialized message contract.</typeparam>
/// <typeparam name="TInput">The string-keyed input dictionary.</typeparam>
/// <typeparam name="THeader">The header value type.</typeparam>
internal sealed class DictionaryCopyHeaderInitializer<TMessage, TInput, THeader> :
    IHeaderInitializer<TMessage, TInput>
    where TMessage : class
    where TInput : class, IDictionary<string, THeader>
{
    readonly IWriteProperty<SendContext, THeader> _headerProperty;
    readonly string _key;

    /// <summary>Creates a dictionary-to-header mapping.</summary>
    /// <param name="propertyInfo">The writable standard send-header property.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    public DictionaryCopyHeaderInitializer(PropertyInfo propertyInfo, string key)
    {
        if (propertyInfo == null)
            throw new ArgumentNullException(nameof(propertyInfo));
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        _key = key;
        _headerProperty = WritePropertyCache<SendContext>.GetProperty<THeader>(propertyInfo);
    }

    /// <summary>Copies the keyed input value when the input contains that key.</summary>
    /// <param name="context">The initialized message and input dictionary.</param>
    /// <param name="sendContext">The outgoing context whose standard header is assigned.</param>
    /// <param name="cancellationToken">The token that cancels header assignment.</param>
    /// <returns>A task that completes after the available header has been assigned.</returns>
    public Task ApplyAsync(InitializeContext<TMessage, TInput> context, SendContext sendContext, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(sendContext);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);
        if (context.HasInput && context.Input.TryGetValue(_key, out var value))
            _headerProperty.Set(sendContext, value);

        return Task.CompletedTask;
    }
}
