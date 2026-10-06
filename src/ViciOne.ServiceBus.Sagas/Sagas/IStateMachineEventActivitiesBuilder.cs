using System;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Builds state machine event activities components.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public interface IStateMachineEventActivitiesBuilder<TSaga> :
    IStateMachineModifier<TSaga>
    where TSaga : class, ISagaStateMachineInstance
{
    /// <summary>Gets a value indicating whether committed.</summary>
    bool IsCommitted { get; }

    /// <summary>Adds behavior for the selected event.</summary>
    /// <param name="event">The event.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The state machine event activities builder produced by the operation.</returns>
    IStateMachineEventActivitiesBuilder<TSaga> When(IEvent @event,
        Func<IEventActivityBinder<TSaga>, IEventActivityBinder<TSaga>> configure);

    /// <summary>Adds behavior for the selected event.</summary>
    /// <param name="event">The event.</param>
    /// <param name="filter">The filter to add to the pipeline.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The state machine event activities builder produced by the operation.</returns>
    IStateMachineEventActivitiesBuilder<TSaga> When(IEvent @event, StateMachineCondition<TSaga> filter,
        Func<IEventActivityBinder<TSaga>, IEventActivityBinder<TSaga>> configure);

    /// <summary>Adds behavior for the selected event.</summary>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <param name="event">The event.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The state machine event activities builder produced by the operation.</returns>
    IStateMachineEventActivitiesBuilder<TSaga> When<TData>(IEvent<TData> @event,
        Func<IEventActivityBinder<TSaga, TData>, IEventActivityBinder<TSaga, TData>> configure)
        where TData : class;

    /// <summary>Adds behavior for the selected event.</summary>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <param name="event">The event.</param>
    /// <param name="filter">The filter to add to the pipeline.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The state machine event activities builder produced by the operation.</returns>
    IStateMachineEventActivitiesBuilder<TSaga> When<TData>(IEvent<TData> @event, StateMachineCondition<TSaga, TData> filter,
        Func<IEventActivityBinder<TSaga, TData>, IEventActivityBinder<TSaga, TData>> configure)
        where TData : class;

    /// <summary>Ignores the selected event or message.</summary>
    /// <param name="event">The event.</param>
    /// <returns>The state machine event activities builder produced by the operation.</returns>
    IStateMachineEventActivitiesBuilder<TSaga> Ignore(IEvent @event);

    /// <summary>Ignores the selected event or message.</summary>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="event">The event.</param>
    /// <returns>The state machine event activities builder produced by the operation.</returns>
    IStateMachineEventActivitiesBuilder<TSaga> Ignore<TMessage>(IEvent<TMessage> @event)
        where TMessage : class;

    /// <summary>Ignores the selected event or message.</summary>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="event">The event.</param>
    /// <param name="filter">The filter to add to the pipeline.</param>
    /// <returns>The state machine event activities builder produced by the operation.</returns>
    IStateMachineEventActivitiesBuilder<TSaga> Ignore<TMessage>(IEvent<TMessage> @event, StateMachineCondition<TSaga, TMessage> filter)
        where TMessage : class;

    /// <summary>Commits the configured state-machine event activities.</summary>
    /// <returns>The state machine modifier produced by the operation.</returns>
    IStateMachineModifier<TSaga> CommitActivities();
}
