using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Builds correlated by fault event correlation components.</summary>
/// <typeparam name="TInstance">The instance type.</typeparam>
/// <typeparam name="TData">The data type.</typeparam>
public class CorrelatedByFaultEventCorrelationBuilder<TInstance, TData> :
    IEventCorrelationBuilder
    where TData : class, ICorrelatedBy<Guid>
    where TInstance : class, ISagaStateMachineInstance
{
    readonly StateMachineInterfaceType<TInstance, Fault<TData>>.ViciOneServiceBusEventCorrelationConfigurator _configurator;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="machine">The machine.</param>
    /// <param name="event">The event.</param>
    public CorrelatedByFaultEventCorrelationBuilder(ISagaStateMachine<TInstance> machine, IEvent<Fault<TData>> @event)
    {
        var configurator = new StateMachineInterfaceType<TInstance, Fault<TData>>.ViciOneServiceBusEventCorrelationConfigurator(machine, @event, null);
        configurator.CorrelateById(x => x.Message.Message.CorrelationId);

        _configurator = configurator;
    }

    /// <summary>Builds the configured component.</summary>
    /// <returns>The configured component.</returns>
    public IEventCorrelation Build()
    {
        return _configurator.Build();
    }
}
