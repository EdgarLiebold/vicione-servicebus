using System;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Describes how a state-machine event is connected to its message contract.</summary>
public interface IEventCorrelation :
    ISpecification
{
    /// <summary>Gets the message type carried by the event.</summary>
    Type DataType { get; }

    /// <summary>Gets whether receive topology for the event message is configured automatically.</summary>
    bool ConfigureConsumeTopology { get; }
}


/// <summary>Describes the repository lookup, missing-instance policy, and message filter for a typed state-machine event.</summary>
/// <typeparam name="TInstance">The saga state type.</typeparam>
/// <typeparam name="TData">The event message type.</typeparam>
public interface IEventCorrelation<TInstance, TData> :
    IEventCorrelation
    where TInstance : class, ISagaStateMachineInstance
    where TData : class
{
    /// <summary>Gets the state-machine event associated with the message type.</summary>
    IEvent<TData> Event { get; }

    /// <summary>Gets the policy that controls saga creation, loading, and missing-instance handling for the event.</summary>
    ISagaPolicy<TInstance, TData>? Policy { get; }

    /// <summary>Gets the factory that creates a repository-aware filter for saga selection, or <see langword="null" /> when none is required.</summary>
    SagaFilterFactory<TInstance, TData>? FilterFactory { get; }

    /// <summary>Gets the consume-context filter that establishes message correlation, or <see langword="null" /> when no filter is required.</summary>
    IFilter<ConsumeContext<TData>>? MessageFilter { get; }
}
