using System;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Creates initiated by saga connector instances.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class InitiatedBySagaConnectorFactory<TSaga, TMessage> :
    ISagaConnectorFactory
    where TSaga : class, ISaga, IInitiatedBy<TMessage>
    where TMessage : class, ICorrelatedBy<Guid>
{
    readonly ISagaMessageConnector<TSaga> _connector;

    /// <summary>Initializes a new instance.</summary>
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
