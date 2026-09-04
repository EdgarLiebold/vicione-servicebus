using System;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>
/// Defines the contract for missing instance redelivery configurator.
/// </summary>
public interface IMissingInstanceRedeliveryConfigurator :
    IRedeliveryConfigurator
{
    /// <summary>
    /// Use the message scheduler context instead of the redelivery context (only use when transport-level redelivery is not available)
    /// </summary>
    bool UseMessageScheduler { set; }
}


/// <summary>
/// Defines the contract for missing instance redelivery configurator.
/// </summary>
/// <typeparam name="TInstance">The t instance type.</typeparam>
/// <typeparam name="TData">The t data type.</typeparam>
public interface IMissingInstanceRedeliveryConfigurator<TInstance, TData> :
    IMissingInstanceRedeliveryConfigurator
    where TInstance : SagaStateMachineInstance
    where TData : class
{
    /// <summary>
    /// Performs the on redelivery limit reached operation.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    void OnRedeliveryLimitReached(Func<IMissingInstanceConfigurator<TInstance, TData>, IPipe<ConsumeContext<TData>>> configure);
}
