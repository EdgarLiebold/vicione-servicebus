using System;

namespace ViciOne.ServiceBus.Configuration;

public partial class StateMachineInterfaceType<TInstance, TData>
{
    /// <summary>Builds message correlation id event correlation components.</summary>
    public class MessageCorrelationIdEventCorrelationBuilder :
        IEventCorrelationBuilder
    {
        readonly ViciOneServiceBusEventCorrelationConfigurator _configurator;

        /// <summary>Initializes a new instance.</summary>
        /// <param name="machine">The machine.</param>
        /// <param name="event">The event.</param>
        /// <param name="messageCorrelationId">The message correlation id.</param>
        public MessageCorrelationIdEventCorrelationBuilder(SagaStateMachine<TInstance> machine, Event<TData> @event,
            IMessageCorrelationId<TData> messageCorrelationId)
        {
            var configurator = new ViciOneServiceBusEventCorrelationConfigurator(machine, @event, null);

            configurator.CorrelateById(x => messageCorrelationId.TryGetCorrelationId(x.Message, out var correlationId)
                ? correlationId
                : throw new ArgumentException($"The message {TypeCache<TData>.ShortName} did not have a correlationId"));

            _configurator = configurator;
        }

        /// <summary>Builds the configured component.</summary>
        /// <returns>The configured component.</returns>
        public EventCorrelation Build()
        {
            return _configurator.Build();
        }
    }
}
