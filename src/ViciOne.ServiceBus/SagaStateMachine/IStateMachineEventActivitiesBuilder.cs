using System;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>
/// Defines the contract for state machine event activities builder.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public interface IStateMachineEventActivitiesBuilder<TSaga> :
    IStateMachineModifier<TSaga>
    where TSaga : class, SagaStateMachineInstance
{
    /// <summary>
    /// Gets the is committed value.
    /// </summary>
    bool IsCommitted { get; }

    /// <summary>
    /// Performs the when operation.
    /// </summary>
    /// <param name="event">The event value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    IStateMachineEventActivitiesBuilder<TSaga> When(Event @event,
        Func<EventActivityBinder<TSaga>, EventActivityBinder<TSaga>> configure);

    /// <summary>
    /// Performs the when operation.
    /// </summary>
    /// <param name="event">The event value.</param>
    /// <param name="filter">The filter value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    IStateMachineEventActivitiesBuilder<TSaga> When(Event @event, StateMachineCondition<TSaga> filter,
        Func<EventActivityBinder<TSaga>, EventActivityBinder<TSaga>> configure);

    /// <summary>
    /// Performs the when operation.
    /// </summary>
    /// <typeparam name="TData">The t data type.</typeparam>
    /// <param name="event">The event value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    IStateMachineEventActivitiesBuilder<TSaga> When<TData>(Event<TData> @event,
        Func<EventActivityBinder<TSaga, TData>, EventActivityBinder<TSaga, TData>> configure)
        where TData : class;

    /// <summary>
    /// Performs the when operation.
    /// </summary>
    /// <typeparam name="TData">The t data type.</typeparam>
    /// <param name="event">The event value.</param>
    /// <param name="filter">The filter value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    IStateMachineEventActivitiesBuilder<TSaga> When<TData>(Event<TData> @event, StateMachineCondition<TSaga, TData> filter,
        Func<EventActivityBinder<TSaga, TData>, EventActivityBinder<TSaga, TData>> configure)
        where TData : class;

    /// <summary>
    /// Performs the ignore operation.
    /// </summary>
    /// <param name="event">The event value.</param>
    /// <returns>The result of the operation.</returns>
    IStateMachineEventActivitiesBuilder<TSaga> Ignore(Event @event);

    /// <summary>
    /// Performs the ignore operation.
    /// </summary>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="event">The event value.</param>
    /// <returns>The result of the operation.</returns>
    IStateMachineEventActivitiesBuilder<TSaga> Ignore<TMessage>(Event<TMessage> @event)
        where TMessage : class;

    /// <summary>
    /// Performs the ignore operation.
    /// </summary>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="event">The event value.</param>
    /// <param name="filter">The filter value.</param>
    /// <returns>The result of the operation.</returns>
    IStateMachineEventActivitiesBuilder<TSaga> Ignore<TMessage>(Event<TMessage> @event, StateMachineCondition<TSaga, TMessage> filter)
        where TMessage : class;

    /// <summary>
    /// Performs the commit activities operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    IStateMachineModifier<TSaga> CommitActivities();
}
