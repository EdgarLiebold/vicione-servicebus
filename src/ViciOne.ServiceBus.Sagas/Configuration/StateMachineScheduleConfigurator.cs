using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures state machine schedule.</summary>
/// <typeparam name="TInstance">The instance type.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class StateMachineScheduleConfigurator<TInstance, TMessage> :
    IScheduleConfigurator<TInstance, TMessage>,
    IScheduleSettings<TInstance, TMessage>
    where TInstance : class, ISagaStateMachineInstance
    where TMessage : class
{
    Action<IEventCorrelationConfigurator<TInstance, TMessage>>? _received = null;

    /// <summary>Initializes a new instance.</summary>
    public StateMachineScheduleConfigurator()
    {
        Delay = TimeSpan.FromSeconds(30);
    }

    /// <summary>Gets the settings.</summary>
    public IScheduleSettings<TInstance, TMessage> Settings => this;

    /// <summary>Sets a fixed delay provider for the schedule.</summary>
    public TimeSpan Delay
    {
        set { DelayProvider = _ => value; }
    }

    /// <summary>Gets or sets the delay provider.</summary>
    public ScheduleDelayProvider<TInstance> DelayProvider { get; set; } = null!;
    Action<IEventCorrelationConfigurator<TInstance, TMessage>>? IScheduleConfigurator<TInstance, TMessage>.Received
    {
        set => _received = value;
    }

    Action<IEventCorrelationConfigurator<TInstance, TMessage>>? IScheduleSettings<TInstance, TMessage>.Received => _received;
}
