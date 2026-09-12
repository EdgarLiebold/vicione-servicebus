using System;
using System.Reflection;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Initializers.HeaderInitializers;

/// <summary>Copies a typed input property into a typed send-context header property.</summary>
/// <typeparam name="TMessage">The initialized message contract.</typeparam>
/// <typeparam name="TInput">The input object containing the header value.</typeparam>
/// <typeparam name="THeader">The header value type.</typeparam>
internal sealed class CopyHeaderInitializer<TMessage, TInput, THeader> :
    IHeaderInitializer<TMessage, TInput>
    where TMessage : class
    where TInput : class
{
    readonly IWriteProperty<SendContext, THeader> _headerProperty;
    readonly IReadProperty<TInput, THeader> _inputProperty;

    /// <summary>Creates an input-property-to-header mapping.</summary>
    /// <param name="headerPropertyInfo">The writable standard send-header property.</param>
    /// <param name="inputPropertyInfo">The readable input property.</param>
    public CopyHeaderInitializer(PropertyInfo headerPropertyInfo, PropertyInfo inputPropertyInfo)
    {
        if (headerPropertyInfo == null)
            throw new ArgumentNullException(nameof(headerPropertyInfo));
        if (inputPropertyInfo == null)
            throw new ArgumentNullException(nameof(inputPropertyInfo));

        _inputProperty = ReadPropertyCache<TInput>.GetProperty<THeader>(inputPropertyInfo);
        _headerProperty = WritePropertyCache<SendContext>.GetProperty<THeader>(headerPropertyInfo);
    }

    /// <summary>Copies the configured input property into the outgoing send context.</summary>
    /// <param name="context">The initialized message and input object.</param>
    /// <param name="sendContext">The outgoing context whose standard header is assigned.</param>
    /// <param name="cancellationToken">The token that cancels header assignment.</param>
    /// <returns>A task that completes after the header has been assigned.</returns>
    public Task ApplyAsync(InitializeContext<TMessage, TInput> context, SendContext sendContext, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(sendContext);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);
        if (!context.HasInput)
            return Task.CompletedTask;

        var inputPropertyValue = _inputProperty.Get(context.Input);

        _headerProperty.Set(sendContext, inputPropertyValue);

        return Task.CompletedTask;
    }
}
