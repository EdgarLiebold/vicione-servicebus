using System;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Configures schedule.</summary>
/// <typeparam name="TInstance">The instance type.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IScheduleConfigurator<TInstance, TMessage>
    where TInstance : class, ISagaStateMachineInstance
    where TMessage : class
{
    /// <summary>Sets a fixed message delay that applies unless an individual schedule operation overrides it.</summary>
    TimeSpan Delay { set; }

    /// <summary>
    /// Sets a message-delay provider that derives the delay from the state-machine instance unless
    /// an individual schedule operation overrides it.
    /// </summary>
    ScheduleDelayProvider<TInstance> DelayProvider { set; }

    /// <summary>Sets the correlation configuration applied when the scheduled message is received.</summary>
    Action<IEventCorrelationConfigurator<TInstance, TMessage>>? Received { set; }
}
