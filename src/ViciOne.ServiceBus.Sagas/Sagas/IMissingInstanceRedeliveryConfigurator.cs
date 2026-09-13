using System;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Configures missing instance redelivery.</summary>
public interface IMissingInstanceRedeliveryConfigurator :
    IRedeliveryConfigurator
{
    /// <summary>Use the message scheduler context instead of the redelivery context (only use when transport-level redelivery is not available).</summary>
    bool ConfigureMessageScheduler { set; }
}


/// <summary>Configures missing instance redelivery.</summary>
/// <typeparam name="TInstance">The instance type.</typeparam>
/// <typeparam name="TData">The data type.</typeparam>
public interface IMissingInstanceRedeliveryConfigurator<TInstance, TData> :
    IMissingInstanceRedeliveryConfigurator
    where TInstance : ISagaStateMachineInstance
    where TData : class
{
    /// <summary>Handles the notification for redelivery limit reached.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    void OnRedeliveryLimitReached(Func<IMissingInstanceConfigurator<TInstance, TData>, IPipe<ConsumeContext<TData>>> configure);
}
