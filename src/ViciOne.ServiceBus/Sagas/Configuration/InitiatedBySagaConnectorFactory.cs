using System;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides an initiated by saga connector factory implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class InitiatedBySagaConnectorFactory<TSaga, TMessage> :
    ISagaConnectorFactory
    where TSaga : class, ISaga, InitiatedBy<TMessage>
    where TMessage : class, CorrelatedBy<Guid>
{
    readonly ISagaMessageConnector<TSaga> _connector;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public InitiatedBySagaConnectorFactory()
    {
        var consumeFilter = new InitiatedBySagaMessageFilter<TSaga, TMessage>();

        ISagaFactory<TSaga, TMessage> sagaFactory = new DefaultSagaFactory<TSaga, TMessage>();

        var policy = new NewSagaPolicy<TSaga, TMessage>(sagaFactory, false);

        _connector = new SagaConnector<TSaga, TMessage>.CorrelatedSagaMessageConnector(consumeFilter, policy, x => x.Message.CorrelationId);
    }

    ISagaMessageConnector<T> ISagaConnectorFactory.CreateMessageConnector<T>()
    {
        if (_connector is ISagaMessageConnector<T> connector)
            return connector;

        throw new ArgumentException("The saga type did not match the connector type");
    }
}
