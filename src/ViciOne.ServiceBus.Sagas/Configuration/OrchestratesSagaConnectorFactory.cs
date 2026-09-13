using System;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Creates orchestrates saga connector instances.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class OrchestratesSagaConnectorFactory<TSaga, TMessage> :
    ISagaConnectorFactory
    where TSaga : class, ISaga, IOrchestrates<TMessage>
    where TMessage : class, ICorrelatedBy<Guid>
{
    readonly ISagaMessageConnector<TSaga> _connector;

    /// <summary>Initializes a new instance.</summary>
    public OrchestratesSagaConnectorFactory()
    {
        IPipe<ConsumeContext<TMessage>> missingPipe = Pipe.Execute<ConsumeContext<TMessage>>(context =>
            throw new SagaException("An existing saga instance was not found", typeof(TSaga), typeof(TMessage), context.CorrelationId ?? Guid.Empty));

        var policy = new AnyExistingSagaPolicy<TSaga, TMessage>(missingPipe);

        var consumeFilter = new OrchestratesSagaMessageFilter<TSaga, TMessage>();

        _connector = new SagaConnector<TSaga, TMessage>.CorrelatedSagaMessageConnector(consumeFilter, policy, x
            => x.Message
                .CorrelationId);
    }

    ISagaMessageConnector<T> ISagaConnectorFactory.CreateMessageConnector<T>()
    {
        var connector = _connector as ISagaMessageConnector<T>;
        if (connector == null)
            throw new ArgumentException("The saga type did not match the connector type");

        return connector;
    }
}
