// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    using System;


    public class CorrelatedByEventCorrelationBuilder<TInstance, TData> :
        IEventCorrelationBuilder
        where TData : class, CorrelatedBy<Guid>
        where TInstance : class, SagaStateMachineInstance
    {
        readonly StateMachineInterfaceType<TInstance, TData>.ViciOneServiceBusEventCorrelationConfigurator _configurator;

        public CorrelatedByEventCorrelationBuilder(SagaStateMachine<TInstance> machine, Event<TData> @event)
        {
            var configurator = new StateMachineInterfaceType<TInstance, TData>.ViciOneServiceBusEventCorrelationConfigurator(machine, @event, null);
            configurator.CorrelateById(x => x.Message.CorrelationId);

            _configurator = configurator;
        }

        public EventCorrelation Build()
        {
            return _configurator.Build();
        }
    }
}
