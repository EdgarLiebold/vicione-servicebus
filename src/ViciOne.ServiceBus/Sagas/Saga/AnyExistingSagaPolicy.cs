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

    public AnyExistingSagaPolicy(IPipe<ConsumeContext<TMessage>>? missingPipe = null, bool readOnly = false)
    {
        IsReadOnly = readOnly;
        _missingPipe = missingPipe ?? Pipe.Empty<ConsumeContext<TMessage>>();
    }

    public bool IsReadOnly { get; }

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
