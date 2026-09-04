using System;

namespace ViciOne.ServiceBus.Configuration;

public partial class StateMachineInterfaceType<TInstance, TData>
{
    public class MessageCorrelationIdEventCorrelationBuilder :
        IEventCorrelationBuilder
    {
        readonly ViciOneServiceBusEventCorrelationConfigurator _configurator;

        public MessageCorrelationIdEventCorrelationBuilder(SagaStateMachine<TInstance> machine, Event<TData> @event,
            IMessageCorrelationId<TData> messageCorrelationId)
        {
            var configurator = new ViciOneServiceBusEventCorrelationConfigurator(machine, @event, null);

            configurator.CorrelateById(x => messageCorrelationId.TryGetCorrelationId(x.Message, out var correlationId)
                ? correlationId
                : throw new ArgumentException($"The message {TypeCache<TData>.ShortName} did not have a correlationId"));

            _configurator = configurator;
        }

        public EventCorrelation Build()
        {
            return _configurator.Build();
        }
    }
}
