using System;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Provides the delay and receive-event correlation used by a state-machine schedule.</summary>
/// <typeparam name="TInstance">The saga state type.</typeparam>
/// <typeparam name="TMessage">The scheduled message type.</typeparam>
public interface IScheduleSettings<TInstance, TMessage>
    where TInstance : class, ISagaStateMachineInstance
    where TMessage : class
{
    /// <summary>Gets the function that calculates the delivery delay from the current saga context.</summary>
    ScheduleDelayProvider<TInstance> DelayProvider { get; }

    /// <summary>Gets the callback that configures correlation for the scheduled-message receive event.</summary>
    Action<IEventCorrelationConfigurator<TInstance, TMessage>> Received { get; }
}
