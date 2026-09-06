using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Saga;

/// <summary>Sends the message to any existing saga instances, failing silently if no saga instances are found.</summary>
/// <typeparam name="TSaga">The saga type.</typeparam>
/// <typeparam name="TMessage">The message type.</typeparam>
public class AnyExistingSagaPolicy<TSaga, TMessage> :
    ISagaPolicy<TSaga, TMessage>
    where TSaga : class, ISaga
    where TMessage : class
{
    readonly IPipe<ConsumeContext<TMessage>> _missingPipe;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="missingPipe">The missing pipe.</param>
    /// <param name="readOnly">The read only.</param>
    public AnyExistingSagaPolicy(IPipe<ConsumeContext<TMessage>>? missingPipe = null, bool readOnly = false)
    {
        IsReadOnly = readOnly;
        _missingPipe = missingPipe ?? Pipe.Empty<ConsumeContext<TMessage>>();
    }

    /// <summary>Gets a value indicating whether read only.</summary>
    public bool IsReadOnly { get; }

    /// <summary>Runs before insert instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="instance">Receives the instance produced by the operation.</param>
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
