using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Saga;

/// <summary>
/// Accepts a message to a saga that does not already exist, throwing an exception if an existing
/// saga instance is specified.
/// </summary>
/// <typeparam name="TSaga">The saga type.</typeparam>
/// <typeparam name="TMessage">The message type.</typeparam>
public class NewSagaPolicy<TSaga, TMessage> :
    ISagaPolicy<TSaga, TMessage>
    where TSaga : class, ISaga
    where TMessage : class
{
    readonly bool _insertOnInitial;
    readonly ISagaFactory<TSaga, TMessage> _sagaFactory;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="sagaFactory">The saga factory.</param>
    /// <param name="insertOnInitial">The insert on initial.</param>
    public NewSagaPolicy(ISagaFactory<TSaga, TMessage> sagaFactory, bool insertOnInitial)
    {
        _sagaFactory = sagaFactory;
        _insertOnInitial = insertOnInitial;
    }

    /// <summary>Gets a value indicating whether read only.</summary>
    public bool IsReadOnly => false;

    /// <summary>Runs before insert instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="instance">Receives the instance produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool PreInsertInstance(ConsumeContext<TMessage> context, [NotNullWhen(true)] out TSaga? instance)
    {
        if (_insertOnInitial)
        {
            instance = _sagaFactory.Create(context);
            return true;
        }

        instance = null;
        return false;
    }

    Task ISagaPolicy<TSaga, TMessage>.ExistingAsync(SagaConsumeContext<TSaga, TMessage> context, IPipe<SagaConsumeContext<TSaga, TMessage>> next)
    {
        throw new SagaException("The message cannot be accepted by an existing saga", typeof(TSaga), typeof(TMessage),
            context.CorrelationId ?? Guid.Empty);
    }

    Task ISagaPolicy<TSaga, TMessage>.MissingAsync(ConsumeContext<TMessage> context, IPipe<SagaConsumeContext<TSaga, TMessage>> next)
    {
        return _sagaFactory.SendAsync(context, next);
    }
}
