using System;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Futures;

/// <summary>Adds future lifecycle and result activities to state-machine event binders.</summary>
public static class FutureExtensions
{
    /// <summary>Adds an activity that initializes durable future state from its initiating command.</summary>
    /// <typeparam name="T">The initiating command contract.</typeparam>
    /// <param name="binder">The command event binder to extend.</param>
    /// <returns>The same binder with future initialization appended.</returns>
    public static EventActivityBinder<FutureState, T> InitializeFuture<T>(this EventActivityBinder<FutureState, T> binder)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(binder);
        return binder
            .Then(context =>
            {
                context.Saga.Created = context.SentTime ?? context.GetUtcDateTime();
                context.Saga.Command = context.CreateFutureMessage(context.Message);
                context.Saga.Location = new FutureLocation(context.Saga.CorrelationId, context.ReceiveContext.InputAddress);

                context.AddSubscription();
            });
    }

    /// <summary>Adds an activity that subscribes a later request to the existing future outcome.</summary>
    /// <typeparam name="T">The request contract.</typeparam>
    /// <param name="binder">The request event binder to extend.</param>
    /// <returns>The same binder with subscription registration appended.</returns>
    public static EventActivityBinder<FutureState, T> AddSubscription<T>(this EventActivityBinder<FutureState, T> binder)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(binder);
        return binder.Then(context =>
        {
            context.AddSubscription();
        });
    }

    /// <summary>Adds an asynchronous activity that stores a successful result for an operation.</summary>
    /// <typeparam name="T">The response event contract.</typeparam>
    /// <typeparam name="TResult">The successful result contract.</typeparam>
    /// <param name="binder">The response event binder to extend.</param>
    /// <param name="getResultId">The selector for the completed operation identifier.</param>
    /// <param name="messageFactory">The asynchronous result factory.</param>
    /// <returns>The same binder with result storage appended.</returns>
    public static EventActivityBinder<FutureState, T> SetResult<T, TResult>(this EventActivityBinder<FutureState, T> binder,
        Func<BehaviorContext<FutureState, T>, Guid> getResultId, AsyncEventMessageFactory<FutureState, T, TResult> messageFactory)
        where T : class
        where TResult : class
    {
        ArgumentNullException.ThrowIfNull(binder);
        ArgumentNullException.ThrowIfNull(getResultId);
        ArgumentNullException.ThrowIfNull(messageFactory);
        return binder.ThenAwaited(context =>
        {
            var resultId = getResultId(context);

            return context.SetResultAsync(resultId, messageFactory);
        });
    }

    /// <summary>Adds an activity that stores a successful result for an operation.</summary>
    /// <typeparam name="T">The response event contract.</typeparam>
    /// <typeparam name="TResult">The successful result contract.</typeparam>
    /// <param name="binder">The response event binder to extend.</param>
    /// <param name="getResultId">The selector for the completed operation identifier.</param>
    /// <param name="messageFactory">The synchronous result factory.</param>
    /// <returns>The same binder with result storage appended.</returns>
    public static EventActivityBinder<FutureState, T> SetResult<T, TResult>(this EventActivityBinder<FutureState, T> binder,
        Func<BehaviorContext<FutureState, T>, Guid> getResultId, EventMessageFactory<FutureState, T, TResult> messageFactory)
        where T : class
        where TResult : class
    {
        ArgumentNullException.ThrowIfNull(binder);
        ArgumentNullException.ThrowIfNull(getResultId);
        ArgumentNullException.ThrowIfNull(messageFactory);
        return binder.Then(context =>
        {
            var resultId = getResultId(context);

            context.SetResult(resultId, messageFactory);
        });
    }

    /// <summary>Adds an activity that stores a fault for a failed request.</summary>
    /// <typeparam name="T">The request contract carried by the fault event.</typeparam>
    /// <typeparam name="TResult">The future fault contract to store.</typeparam>
    /// <param name="binder">The fault event binder to extend.</param>
    /// <param name="getResultId">The selector for the failed operation identifier.</param>
    /// <param name="messageFactory">The synchronous fault factory.</param>
    /// <returns>The same binder with fault storage appended.</returns>
    public static EventActivityBinder<FutureState, Fault<T>> SetFault<T, TResult>(this EventActivityBinder<FutureState, Fault<T>> binder,
        Func<BehaviorContext<FutureState, Fault<T>>, Guid> getResultId, EventMessageFactory<FutureState, Fault<T>, TResult> messageFactory)
        where T : class
        where TResult : class
    {
        ArgumentNullException.ThrowIfNull(binder);
        ArgumentNullException.ThrowIfNull(getResultId);
        ArgumentNullException.ThrowIfNull(messageFactory);
        return binder.Then(context =>
        {
            var resultId = getResultId(context);

            context.SetFault(resultId, messageFactory);
        });
    }

    /// <summary>Adds an activity that stores a fault for a failed routing slip.</summary>
    /// <typeparam name="TResult">The future fault contract to store.</typeparam>
    /// <param name="binder">The routing-slip fault event binder to extend.</param>
    /// <param name="messageFactory">The synchronous fault factory.</param>
    /// <returns>The same binder with fault storage appended.</returns>
    public static EventActivityBinder<FutureState, RoutingSlipFaulted> SetFault<TResult>(this EventActivityBinder<FutureState, RoutingSlipFaulted> binder,
        EventMessageFactory<FutureState, RoutingSlipFaulted, TResult> messageFactory)
        where TResult : class
    {
        ArgumentNullException.ThrowIfNull(binder);
        ArgumentNullException.ThrowIfNull(messageFactory);
        return binder.Then(context =>
        {
            var resultId = context.Message.TrackingNumber;

            context.SetFault(resultId, messageFactory);
        });
    }
}
