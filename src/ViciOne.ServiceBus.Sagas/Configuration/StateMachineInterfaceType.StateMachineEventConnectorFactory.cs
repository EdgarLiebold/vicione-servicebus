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
            ArgumentNullException.ThrowIfNull(stateMachine);
            ArgumentNullException.ThrowIfNull(correlation);

            IEvent<TData> @event = correlation.Event
                ?? throw new InvalidOperationException("The event correlation returned a null event.");
            ISagaPolicy<TInstance, TData>? policy = correlation.Policy;
            SagaFilterFactory<TInstance, TData>? filterFactory = correlation.FilterFactory;
            IFilter<ConsumeContext<TData>>? messageFilter = correlation.MessageFilter;
            bool configureConsumeTopology = correlation.ConfigureConsumeTopology;

            if (filterFactory != null)
            {
                SagaFilterFactory<TInstance, TData> configuredFilterFactory = filterFactory;
                filterFactory = (repository, sagaPolicy, sagaPipe) =>
                    configuredFilterFactory(repository, sagaPolicy, sagaPipe)
                    ?? throw new InvalidOperationException("The event correlation filter factory returned a null saga filter.");
            }

            var consumeFilter = new StateMachineSagaMessageFilter<TInstance, TData>(stateMachine, @event);

            _connector = new StateMachineSagaMessageConnector(
                consumeFilter,
                policy,
                filterFactory,
                messageFilter,
                configureConsumeTopology);
        }

        ISagaMessageConnector<T> ISagaConnectorFactory.CreateMessageConnector<T>()
        {
            if (typeof(T) != typeof(TInstance))
                throw new ArgumentException("The generic argument did not match the state machine instance type", nameof(T));

            return (ISagaMessageConnector<T>)(object)_connector;
        }
    }
}
