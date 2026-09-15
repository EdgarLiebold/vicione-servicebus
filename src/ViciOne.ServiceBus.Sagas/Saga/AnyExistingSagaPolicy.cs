using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Saga;

/// <summary>Dispatches to existing saga instances and uses a configurable message pipeline when no instance is found.</summary>
/// <typeparam name="TSaga">The saga state accepted by existing-instance dispatch.</typeparam>
/// <typeparam name="TMessage">The message contract used for existing- and missing-instance dispatch.</typeparam>
public class AnyExistingSagaPolicy<TSaga, TMessage> :
    ISagaPolicy<TSaga, TMessage>
    where TSaga : class, ISaga
    where TMessage : class
{
    readonly IPipe<ConsumeContext<TMessage>> _missingPipe;

    /// <summary>Configures existing-instance dispatch, defaulting missing-instance handling to an empty pipeline.</summary>
    /// <param name="missingPipe">The optional message pipeline invoked when no instance is found; null selects an empty pipeline.</param>
    /// <param name="readOnly">Whether the repository should treat existing-instance dispatch as read-only.</param>
    public AnyExistingSagaPolicy(IPipe<ConsumeContext<TMessage>>? missingPipe = null, bool readOnly = false)
    {
        IsReadOnly = readOnly;
        _missingPipe = missingPipe ?? Pipe.Empty<ConsumeContext<TMessage>>();
    }

    /// <summary>Gets whether existing-instance dispatch uses a read-only repository policy.</summary>
    public bool IsReadOnly { get; }

    /// <summary>Declines pre-insertion because this policy never creates saga instances.</summary>
    /// <param name="context">The message context for the repository's pre-insertion check.</param>
    /// <param name="instance">Receives null because this policy does not create an instance.</param>
    /// <returns>Always false.</returns>
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
