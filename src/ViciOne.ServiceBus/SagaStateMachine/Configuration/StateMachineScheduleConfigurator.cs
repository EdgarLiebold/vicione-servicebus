using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a state machine schedule configurator implementation.
/// </summary>
/// <typeparam name="TInstance">The t instance type.</typeparam>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class StateMachineScheduleConfigurator<TInstance, TMessage> :
    IScheduleConfigurator<TInstance, TMessage>,
    ScheduleSettings<TInstance, TMessage>
    where TInstance : class, SagaStateMachineInstance
    where TMessage : class
{
    Action<IEventCorrelationConfigurator<TInstance, TMessage>> _received = null!;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public StateMachineScheduleConfigurator()
    {
        Delay = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// Gets the settings value.
    /// </summary>
    public ScheduleSettings<TInstance, TMessage> Settings => this;

    /// <summary>
    /// Gets or sets the delay value.
    /// </summary>
    public TimeSpan Delay
    {
        set { DelayProvider = _ => value; }
    }

    /// <summary>
    /// Gets or sets the delay provider value.
    /// </summary>
    public ScheduleDelayProvider<TInstance> DelayProvider { get; set; } = null!;
    Action<IEventCorrelationConfigurator<TInstance, TMessage>> IScheduleConfigurator<TInstance, TMessage>.Received
    {
        set => _received = value;
    }

    Action<IEventCorrelationConfigurator<TInstance, TMessage>> ScheduleSettings<TInstance, TMessage>.Received => _received;
}
