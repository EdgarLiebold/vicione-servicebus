using System;

namespace ViciOne.ServiceBus.Configuration;

public partial class StateMachineInterfaceType<TInstance, TData>
{
    /// <summary>Builds event correlation using an identifier extracted from the event message.</summary>
    public class MessageCorrelationIdEventCorrelationBuilder :
        IEventCorrelationBuilder
    {
        readonly ViciOneServiceBusEventCorrelationConfigurator _configurator;

        /// <summary>Configures identifier correlation and rejects messages without an extractable identifier during dispatch.</summary>
        /// <param name="machine">The state machine handling the correlated event.</param>
        /// <param name="event">The event carrying the message to correlate.</param>
        /// <param name="messageCorrelationId">The extractor supplying the saga correlation identifier from each message.</param>
        public MessageCorrelationIdEventCorrelationBuilder(ISagaStateMachine<TInstance> machine, IEvent<TData> @event,
            IMessageCorrelationId<TData> messageCorrelationId)
        {
            var configurator = new ViciOneServiceBusEventCorrelationConfigurator(machine, @event, null);

            configurator.CorrelateById(x => messageCorrelationId.TryGetCorrelationId(x.Message, out var correlationId)
                ? correlationId
                : throw new ArgumentException($"The message {TypeCache<TData>.ShortName} did not have a correlationId"));

            _configurator = configurator;
        }

        /// <summary>Builds the configured message-event correlation.</summary>
        /// <returns>The correlation using the message identifier extractor.</returns>
        public IEventCorrelation Build()
        {
            return _configurator.Build();
        }
    }
}
