using System;

namespace ViciOne.ServiceBus.Configuration;

public class CorrelatedByFaultEventCorrelationBuilder<TInstance, TData> :
    IEventCorrelationBuilder
    where TData : class, CorrelatedBy<Guid>
    where TInstance : class, SagaStateMachineInstance
{
    readonly StateMachineInterfaceType<TInstance, Fault<TData>>.ViciOneServiceBusEventCorrelationConfigurator _configurator;

    public CorrelatedByFaultEventCorrelationBuilder(SagaStateMachine<TInstance> machine, Event<Fault<TData>> @event)
    {
        var configurator = new StateMachineInterfaceType<TInstance, Fault<TData>>.ViciOneServiceBusEventCorrelationConfigurator(machine, @event, null);
        configurator.CorrelateById(x => x.Message.Message.CorrelationId);

        _configurator = configurator;
    }

    public EventCorrelation Build()
    {
        return _configurator.Build();
    }
}
