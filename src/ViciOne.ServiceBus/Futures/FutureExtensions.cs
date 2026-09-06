using System;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Futures;

/// <summary>Provides extension methods for future.</summary>
public static class FutureExtensions
{
    /// <summary>Initialize the FutureState properties of the request.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="binder">The binder.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static EventActivityBinder<FutureState, T> InitializeFuture<T>(this EventActivityBinder<FutureState, T> binder)
        where T : class
    {
        return binder
            .Then(context =>
            {
                context.Saga.Created = context.SentTime ?? context.GetUtcDateTime();
                context.Saga.Command = context.CreateFutureMessage(context.Message);
                context.Saga.Location = new FutureLocation(context.Saga.CorrelationId, context.ReceiveContext.InputAddress);

                context.AddSubscription();
            });
    }

    /// <summary>Use when a request is received after the initial request is still awaiting completion.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="binder">The binder.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static EventActivityBinder<FutureState, T> AddSubscription<T>(this EventActivityBinder<FutureState, T> binder)
        where T : class
    {
        return binder.Then(context =>
        {
            context.AddSubscription();
        });
    }

    /// <summary>Set the result associated with the identifier using the message factory.</summary>
    /// <typeparam name="T">The event type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="binder">The binder.</param>
    /// <param name="getResultId">Should return the result identifier.</param>
    /// <param name="messageFactory">Should return the result message.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static EventActivityBinder<FutureState, T> SetResult<T, TResult>(this EventActivityBinder<FutureState, T> binder,
        Func<BehaviorContext<FutureState, T>, Guid> getResultId, AsyncEventMessageFactory<FutureState, T, TResult> messageFactory)
        where T : class
        where TResult : class
    {
        return binder.ThenAsync(context =>
        {
            var resultId = getResultId(context);

            return context.SetResultAsync(resultId, messageFactory);
        });
    }

    /// <summary>Set the result associated with the identifier using the message factory.</summary>
    /// <typeparam name="T">The event type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="binder">The binder.</param>
    /// <param name="getResultId">Should return the result identifier.</param>
    /// <param name="messageFactory">Should return the result message.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static EventActivityBinder<FutureState, T> SetResult<T, TResult>(this EventActivityBinder<FutureState, T> binder,
        Func<BehaviorContext<FutureState, T>, Guid> getResultId, EventMessageFactory<FutureState, T, TResult> messageFactory)
        where T : class
        where TResult : class
    {
        return binder.Then(context =>
        {
            var resultId = getResultId(context);

            context.SetResult(resultId, messageFactory);
        });
    }

    /// <summary>Set the result associated with the identifier using the message factory.</summary>
    /// <typeparam name="T">The event type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="binder">The binder.</param>
    /// <param name="getResultId">Should return the result identifier.</param>
    /// <param name="messageFactory">Should return the result message.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static EventActivityBinder<FutureState, Fault<T>> SetFault<T, TResult>(this EventActivityBinder<FutureState, Fault<T>> binder,
        Func<BehaviorContext<FutureState, Fault<T>>, Guid> getResultId, EventMessageFactory<FutureState, Fault<T>, TResult> messageFactory)
        where T : class
        where TResult : class
    {
        return binder.Then(context =>
        {
            var resultId = getResultId(context);

            context.SetFault(resultId, messageFactory);
        });
    }

    /// <summary>Set the result associated with the identifier using the message factory.</summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="binder">The binder.</param>
    /// <param name="messageFactory">Should return the result message.</param>
    /// <returns>The event activity binder produced by the operation.</returns>
    public static EventActivityBinder<FutureState, RoutingSlipFaulted> SetFault<TResult>(this EventActivityBinder<FutureState, RoutingSlipFaulted> binder,
        EventMessageFactory<FutureState, RoutingSlipFaulted, TResult> messageFactory)
        where TResult : class
    {
        return binder.Then(context =>
        {
            var resultId = context.Message.TrackingNumber;

            context.SetFault(resultId, messageFactory);
        });
    }
}
