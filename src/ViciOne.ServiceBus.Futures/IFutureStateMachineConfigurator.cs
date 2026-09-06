using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Futures;

/// <summary>Connects request and result configuration to a future state machine.</summary>
internal interface IFutureStateMachineConfigurator
{
    /// <summary>Creates the state-machine event that consumes a response contract.</summary>
    /// <typeparam name="T">The response contract.</typeparam>
    /// <returns>The created response event.</returns>
    Event<T> CreateResponseEvent<T>()
        where T : class;

    /// <summary>Configures a response event to create and persist the successful future result.</summary>
    /// <typeparam name="T">The response contract.</typeparam>
    /// <param name="responseReceived">The response event to configure.</param>
    /// <param name="callback">The asynchronous result callback executed for the response.</param>
    void SetResult<T>(Event<T> responseReceived, Func<BehaviorContext<FutureState, T>, Task> callback)
        where T : class;

    /// <summary>Configures an event to create and persist the terminal future fault.</summary>
    /// <typeparam name="T">The event contract that triggers the fault.</typeparam>
    /// <param name="requestCompleted">The event to configure.</param>
    /// <param name="callback">The asynchronous fault callback executed for the event.</param>
    void SetFaulted<T>(Event<T> requestCompleted, Func<BehaviorContext<FutureState, T>, Task> callback)
        where T : class;

    /// <summary>Configures a response event to store its result and remove the corresponding pending identifier.</summary>
    /// <typeparam name="T">The response contract.</typeparam>
    /// <param name="requestCompleted">The response event to configure.</param>
    /// <param name="pendingIdProvider">The selector that extracts the completed operation identifier.</param>
    void CompletePendingRequest<T>(Event<T> requestCompleted, PendingFutureIdProvider<T> pendingIdProvider)
        where T : class;

    /// <summary>Adds activities for an event in every non-terminal future state.</summary>
    /// <typeparam name="T">The event contract.</typeparam>
    /// <param name="whenEvent">The event to handle.</param>
    /// <param name="configure">The callback that adds activities to the event binder.</param>
    void DuringAnyWhen<T>(Event<T> whenEvent, Func<EventActivityBinder<FutureState, T>,
        EventActivityBinder<FutureState, T>> configure)
        where T : class;
}
