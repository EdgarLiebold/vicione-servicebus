using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Saga;

/// <summary>
/// Sends the message to any existing saga instances, failing silently if no saga instances are found.
/// </summary>
/// <typeparam name="TSaga">The saga type</typeparam>
/// <typeparam name="TMessage">The message type</typeparam>
public class AnyExistingSagaPolicy<TSaga, TMessage> :
    ISagaPolicy<TSaga, TMessage>
    where TSaga : class, ISaga
    where TMessage : class
{
    readonly IPipe<ConsumeContext<TMessage>> _missingPipe;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="missingPipe">The missing pipe value.</param>
    /// <param name="readOnly">The read only value.</param>
    public AnyExistingSagaPolicy(IPipe<ConsumeContext<TMessage>>? missingPipe = null, bool readOnly = false)
    {
        IsReadOnly = readOnly;
        _missingPipe = missingPipe ?? Pipe.Empty<ConsumeContext<TMessage>>();
    }

    /// <summary>
    /// Gets the is read only value.
    /// </summary>
    public bool IsReadOnly { get; }

    /// <summary>
    /// Performs the pre insert instance operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="instance">The instance value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool PreInsertInstance(ConsumeContext<TMessage> context, [NotNullWhen(true)] out TSaga? instance)
    {
        instance = null;
        return false;
    }

    Task ISagaPolicy<TSaga, TMessage>.ExistingAsync(SagaConsumeContext<TSaga, TMessage> context, IPipe<SagaConsumeContext<TSaga, TMessage>> next)
    {
        return next.SendAsync(context);
    }

    Task ISagaPolicy<TSaga, TMessage>.MissingAsync(ConsumeContext<TMessage> context, IPipe<SagaConsumeContext<TSaga, TMessage>> next)
    {
        return _missingPipe.SendAsync(context);
    }
}
