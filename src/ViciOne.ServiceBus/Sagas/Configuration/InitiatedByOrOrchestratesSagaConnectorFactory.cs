using System;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Creates initiated by or orchestrates saga connector instances.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class InitiatedByOrOrchestratesSagaConnectorFactory<TSaga, TMessage> :
    ISagaConnectorFactory
    where TSaga : class, ISaga, InitiatedByOrOrchestrates<TMessage>
    where TMessage : class, CorrelatedBy<Guid>
{
    readonly ISagaMessageConnector<TSaga> _connector;

    /// <summary>Initializes a new instance.</summary>
    public InitiatedByOrOrchestratesSagaConnectorFactory()
    {
        var consumeFilter = new InitiatedByOrOrchestratesSagaMessageFilter<TSaga, TMessage>();

        ISagaFactory<TSaga, TMessage> sagaFactory = new DefaultSagaFactory<TSaga, TMessage>();

        var policy = new NewOrExistingSagaPolicy<TSaga, TMessage>(sagaFactory, false);

        _connector = new SagaConnector<TSaga, TMessage>.CorrelatedSagaMessageConnector(consumeFilter, policy, x => x.Message.CorrelationId);
    }

    ISagaMessageConnector<T> ISagaConnectorFactory.CreateMessageConnector<T>()
    {
        var connector = _connector as ISagaMessageConnector<T>;
        if (connector == null)
            throw new ArgumentException("The saga type did not match the connector type");

        return connector;
    }
}
