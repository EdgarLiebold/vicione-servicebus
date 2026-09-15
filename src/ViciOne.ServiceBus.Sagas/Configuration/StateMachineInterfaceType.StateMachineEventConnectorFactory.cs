using System;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

public partial class StateMachineInterfaceType<TInstance, TData>
{
    /// <summary>Provides a saga message connector composed from a state-machine event correlation.</summary>
    public class StateMachineEventConnectorFactory :
        ISagaConnectorFactory
    {
        readonly ISagaMessageConnector<TInstance> _connector;

        /// <summary>Composes the state-machine consume filter with the event correlation's repository dispatch.</summary>
        /// <param name="stateMachine">The state machine consuming the correlated saga event.</param>
        /// <param name="correlation">The correlation supplying event, policy, filters and topology selection.</param>
        public StateMachineEventConnectorFactory(ISagaStateMachine<TInstance> stateMachine, IEventCorrelation<TInstance, TData> correlation)
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
