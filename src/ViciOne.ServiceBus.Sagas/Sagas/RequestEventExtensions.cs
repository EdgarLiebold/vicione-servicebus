using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Provides extension methods for request event.</summary>
public static class RequestEventExtensions
{
    /// <summary>
    /// Publishes the <see cref="ViciOne.ServiceBus.Contracts.IRequestStarted" /> event, used by the request state machine to track
    /// pending requests for a saga instance.
    /// </summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TInstance, TData> RequestStarted<TInstance, TData>(this IEventActivityBinder<TInstance, TData> source)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
    {
        return source.Add(new RequestStartedActivity<TInstance, TData>());
    }

    /// <summary>
    /// Publishes the <see cref="ViciOne.ServiceBus.Contracts.IRequestCompleted" /> event, used by the request state machine to complete pending
    /// requests. The response type of the inbound request must be the same as the <typeparamref name="TData" /> type.
    /// </summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TInstance, TData> RequestCompleted<TInstance, TData>(this IEventActivityBinder<TInstance, TData> source)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
    {
        return source.Add(new RequestCompletedActivity<TInstance, TData>());
    }

    /// <summary>
    /// Publishes the <see cref="ViciOne.ServiceBus.Contracts.IRequestCompleted" /> event, used by the request state machine to complete pending
    /// requests.
    /// </summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="messageFactory">The message factory.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TInstance, TData> RequestCompleted<TInstance, TData, TResponse>(this IEventActivityBinder<TInstance, TData> source,
        AsyncEventMessageFactory<TInstance, TData, TResponse> messageFactory)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TResponse : class
    {
        return source.Add(new RequestCompletedActivity<TInstance, TData, TResponse>(messageFactory));
    }

    /// <summary>Publishes the <see cref="ViciOne.ServiceBus.Contracts.IRequestFaulted" /> event, used by the request state machine to fault pending requests.</summary>
    /// <typeparam name="TInstance">The instance type.</typeparam>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="requestEvent">The request event.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static IEventActivityBinder<TInstance, TData> RequestFaulted<TInstance, TData, TRequest>(this IEventActivityBinder<TInstance, TData> source,
        IEvent<TRequest> requestEvent)
        where TInstance : class, ISagaStateMachineInstance
        where TData : class
        where TRequest : class
    {
        return source.Add(new RequestFaultedActivity<TInstance, TData, TRequest>());
    }
}
