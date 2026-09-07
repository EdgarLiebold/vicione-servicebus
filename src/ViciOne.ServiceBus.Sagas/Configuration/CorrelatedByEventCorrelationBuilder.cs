using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Builds correlated by event correlation components.</summary>
/// <typeparam name="TInstance">The instance type.</typeparam>
/// <typeparam name="TData">The data type.</typeparam>
public class CorrelatedByEventCorrelationBuilder<TInstance, TData> :
    IEventCorrelationBuilder
    where TData : class, CorrelatedBy<Guid>
    where TInstance : class, SagaStateMachineInstance
{
    readonly StateMachineInterfaceType<TInstance, TData>.ViciOneServiceBusEventCorrelationConfigurator _configurator;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="machine">The machine.</param>
    /// <param name="event">The event.</param>
    public CorrelatedByEventCorrelationBuilder(SagaStateMachine<TInstance> machine, Event<TData> @event)
    {
        var configurator = new StateMachineInterfaceType<TInstance, TData>.ViciOneServiceBusEventCorrelationConfigurator(machine, @event, null);
        configurator.CorrelateById(x => x.Message.CorrelationId);

        _configurator = configurator;
    }

    /// <summary>Builds the configured component.</summary>
    /// <returns>The configured component.</returns>
    public EventCorrelation Build()
    {
        return _configurator.Build();
    }
}
