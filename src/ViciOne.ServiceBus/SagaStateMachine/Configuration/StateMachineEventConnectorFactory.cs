using System;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

public partial class StateMachineInterfaceType<TInstance, TData>
{
    /// <summary>Creates state machine event connector instances.</summary>
    public class StateMachineEventConnectorFactory :
        ISagaConnectorFactory
    {
        readonly ISagaMessageConnector<TInstance> _connector;

        /// <summary>Initializes a new instance.</summary>
        /// <param name="stateMachine">The state machine.</param>
        /// <param name="correlation">The correlation.</param>
        public StateMachineEventConnectorFactory(SagaStateMachine<TInstance> stateMachine, EventCorrelation<TInstance, TData> correlation)
        {
            var consumeFilter = new StateMachineSagaMessageFilter<TInstance, TData>(stateMachine, correlation.Event);

            _connector = new StateMachineSagaMessageConnector(consumeFilter, correlation.Policy,
                correlation.FilterFactory,
                correlation.MessageFilter, correlation.ConfigureConsumeTopology);
        }

        ISagaMessageConnector<T> ISagaConnectorFactory.CreateMessageConnector<T>()
        {
            if (_connector is ISagaMessageConnector<T> connector)
                return connector;

            throw new ArgumentException("The saga type did not match the connector type");
        }
    }
}
