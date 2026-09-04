using System;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>
/// Defines the contract for event correlation.
/// </summary>
public interface EventCorrelation :
    ISpecification
{
    /// <summary>
    /// The data type for the event
    /// </summary>
    Type DataType { get; }

    /// <summary>
    /// Gets the configure consume topology value.
    /// </summary>
    bool ConfigureConsumeTopology { get; }
}


/// <summary>
/// Defines the contract for event correlation.
/// </summary>
/// <typeparam name="TInstance">The t instance type.</typeparam>
/// <typeparam name="TData">The t data type.</typeparam>
public interface EventCorrelation<TInstance, TData> :
    EventCorrelation
    where TInstance : class, SagaStateMachineInstance
    where TData : class
{
    /// <summary>
    /// Gets the event value.
    /// </summary>
    Event<TData> Event { get; }

    /// <summary>
    /// Returns the saga policy for the event correlation
    /// </summary>
    /// <value></value>
    ISagaPolicy<TInstance, TData>? Policy { get; }

    /// <summary>
    /// The filter factory creates the filter when requested by the connector
    /// </summary>
    SagaFilterFactory<TInstance, TData>? FilterFactory { get; }

    /// <summary>
    /// The message filter which extracts the correlationId from the message
    /// </summary>
    IFilter<ConsumeContext<TData>>? MessageFilter { get; }
}
