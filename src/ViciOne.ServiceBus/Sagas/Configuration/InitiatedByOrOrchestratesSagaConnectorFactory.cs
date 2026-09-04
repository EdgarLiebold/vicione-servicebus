using System;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides an initiated by or orchestrates saga connector factory implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class InitiatedByOrOrchestratesSagaConnectorFactory<TSaga, TMessage> :
    ISagaConnectorFactory
    where TSaga : class, ISaga, InitiatedByOrOrchestrates<TMessage>
    where TMessage : class, CorrelatedBy<Guid>
{
    readonly ISagaMessageConnector<TSaga> _connector;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
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
