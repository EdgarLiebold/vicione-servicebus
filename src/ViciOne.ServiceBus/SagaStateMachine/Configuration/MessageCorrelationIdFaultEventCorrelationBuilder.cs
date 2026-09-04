using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a state machine interface type implementation.
/// </summary>
public partial class StateMachineInterfaceType<TInstance, TData>
{
    /// <summary>
    /// Provides a message correlation id fault event correlation builder implementation.
    /// </summary>
    public class MessageCorrelationIdFaultEventCorrelationBuilder :
        IEventCorrelationBuilder
    {
        readonly StateMachineInterfaceType<TInstance, Fault<TData>>.ViciOneServiceBusEventCorrelationConfigurator _configurator;

        /// <summary>
        /// Initializes a new instance of the containing type.
        /// </summary>
        /// <param name="machine">The machine value.</param>
        /// <param name="event">The event value.</param>
        /// <param name="messageCorrelationId">The message correlation id value.</param>
        public MessageCorrelationIdFaultEventCorrelationBuilder(SagaStateMachine<TInstance> machine, Event<Fault<TData>> @event,
            IMessageCorrelationId<TData> messageCorrelationId)
        {
            var configurator = new StateMachineInterfaceType<TInstance, Fault<TData>>.ViciOneServiceBusEventCorrelationConfigurator(machine, @event, null);

            configurator.CorrelateById(x => messageCorrelationId.TryGetCorrelationId(x.Message.Message, out var correlationId)
                ? correlationId
                : throw new ArgumentException($"The message {TypeCache<TData>.ShortName} did not have a correlationId"));

            _configurator = configurator;
        }

        /// <summary>
        /// Performs the build operation.
        /// </summary>
        /// <returns>The result of the operation.</returns>
        public EventCorrelation Build()
        {
            return _configurator.Build();
        }
    }
}
