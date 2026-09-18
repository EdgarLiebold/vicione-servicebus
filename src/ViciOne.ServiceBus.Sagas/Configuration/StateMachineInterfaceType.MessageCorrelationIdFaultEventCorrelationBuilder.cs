using System;

namespace ViciOne.ServiceBus.Configuration;

public partial class StateMachineInterfaceType<TInstance, TData>
{
    /// <summary>Builds fault-event correlation using the identifier of the enclosed original message.</summary>
    public class MessageCorrelationIdFaultEventCorrelationBuilder :
        IEventCorrelationBuilder
    {
        readonly StateMachineInterfaceType<TInstance, Fault<TData>>.ViciOneServiceBusEventCorrelationConfigurator _configurator;

        /// <summary>Configures identifier correlation for original messages carried by faults.</summary>
        /// <param name="machine">The state machine handling the correlated fault event.</param>
        /// <param name="event">The event carrying the fault and its original message.</param>
        /// <param name="messageCorrelationId">The extractor supplying an identifier from each fault's original message.</param>
        public MessageCorrelationIdFaultEventCorrelationBuilder(ISagaStateMachine<TInstance> machine, IEvent<Fault<TData>> @event,
            IMessageCorrelationId<TData> messageCorrelationId)
        {
            ArgumentNullException.ThrowIfNull(machine);
            ArgumentNullException.ThrowIfNull(@event, nameof(@event));
            ArgumentNullException.ThrowIfNull(messageCorrelationId);

            var configurator = new StateMachineInterfaceType<TInstance, Fault<TData>>.ViciOneServiceBusEventCorrelationConfigurator(machine, @event, null);

            configurator.CorrelateById(x => messageCorrelationId.TryGetCorrelationId(x.Message.Message, out var correlationId)
                ? correlationId
                : throw new ArgumentException($"The message {TypeCache<TData>.ShortName} did not have a correlationId"));

            _configurator = configurator;
        }

        /// <summary>Builds the configured fault-message event correlation.</summary>
        /// <returns>The correlation using the enclosed message's identifier.</returns>
        public IEventCorrelation Build()
        {
            return _configurator.Build();
        }
    }
}
