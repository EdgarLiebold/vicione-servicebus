using System;
using System.Reflection;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Initializers.HeaderInitializers;

/// <summary>
/// Set a header to a constant value from the input
/// </summary>
/// <typeparam name="TMessage"></typeparam>
/// <typeparam name="TInput"></typeparam>
/// <typeparam name="THeader">The header type</typeparam>
public class CopyHeaderInitializer<TMessage, TInput, THeader> :
    IHeaderInitializer<TMessage, TInput>
    where TMessage : class
    where TInput : class
{
    readonly IWriteProperty<SendContext, THeader> _headerProperty;
    readonly IReadProperty<TInput, THeader> _inputProperty;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="headerPropertyInfo">The header property info value.</param>
    /// <param name="inputPropertyInfo">The input property info value.</param>
    public CopyHeaderInitializer(PropertyInfo headerPropertyInfo, PropertyInfo inputPropertyInfo)
    {
        if (headerPropertyInfo == null)
            throw new ArgumentNullException(nameof(headerPropertyInfo));

        _inputProperty = ReadPropertyCache<TInput>.GetProperty<THeader>(inputPropertyInfo);
        _headerProperty = WritePropertyCache<SendContext>.GetProperty<THeader>(headerPropertyInfo);
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
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); var inputPropertyValue = _inputProperty.Get(context.Input);

        _headerProperty.Set(sendContext, inputPropertyValue);

        return Task.CompletedTask;
    }
}
