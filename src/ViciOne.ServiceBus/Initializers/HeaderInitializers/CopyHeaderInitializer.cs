using System;
using System.Reflection;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Initializers.HeaderInitializers;

/// <summary>Set a header to a constant value from the input.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <typeparam name="TInput">The input type.</typeparam>
/// <typeparam name="THeader">The header type.</typeparam>
public class CopyHeaderInitializer<TMessage, TInput, THeader> :
    IHeaderInitializer<TMessage, TInput>
    where TMessage : class
    where TInput : class
{
    readonly IWriteProperty<SendContext, THeader> _headerProperty;
    readonly IReadProperty<TInput, THeader> _inputProperty;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="headerPropertyInfo">The header property info.</param>
    /// <param name="inputPropertyInfo">The input property info.</param>
    public CopyHeaderInitializer(PropertyInfo headerPropertyInfo, PropertyInfo inputPropertyInfo)
    {
        if (headerPropertyInfo == null)
            throw new ArgumentNullException(nameof(headerPropertyInfo));

        _inputProperty = ReadPropertyCache<TInput>.GetProperty<THeader>(inputPropertyInfo);
        _headerProperty = WritePropertyCache<SendContext>.GetProperty<THeader>(headerPropertyInfo);
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="sendContext">The send context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ApplyAsync(InitializeContext<TMessage, TInput> context, SendContext sendContext, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); var inputPropertyValue = _inputProperty.Get(context.Input);

        _headerProperty.Set(sendContext, inputPropertyValue);

        return Task.CompletedTask;
    }
}
