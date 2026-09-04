using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a correlated by event correlation builder implementation.
/// </summary>
/// <typeparam name="TInstance">The t instance type.</typeparam>
/// <typeparam name="TData">The t data type.</typeparam>
public class CorrelatedByEventCorrelationBuilder<TInstance, TData> :
    IEventCorrelationBuilder
    where TData : class, CorrelatedBy<Guid>
    where TInstance : class, SagaStateMachineInstance
{
    readonly StateMachineInterfaceType<TInstance, TData>.ViciOneServiceBusEventCorrelationConfigurator _configurator;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="machine">The machine value.</param>
    /// <param name="event">The event value.</param>
    public CorrelatedByEventCorrelationBuilder(SagaStateMachine<TInstance> machine, Event<TData> @event)
    {
        var configurator = new StateMachineInterfaceType<TInstance, TData>.ViciOneServiceBusEventCorrelationConfigurator(machine, @event, null);
        configurator.CorrelateById(x => x.Message.CorrelationId);

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
