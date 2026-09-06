using System;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>The schedule settings, including the default delay for the message.</summary>
/// <typeparam name="TInstance">The instance type.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface ScheduleSettings<TInstance, TMessage>
    where TInstance : class, SagaStateMachineInstance
    where TMessage : class
{
    /// <summary>Provides the delay for the message.</summary>
    ScheduleDelayProvider<TInstance> DelayProvider { get; }

    /// <summary>Configure the received correlation.</summary>
    Action<IEventCorrelationConfigurator<TInstance, TMessage>> Received { get; }
}
