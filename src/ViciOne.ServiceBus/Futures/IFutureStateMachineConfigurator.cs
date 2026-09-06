using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Futures;

/// <summary>Configures future state machine.</summary>
public interface IFutureStateMachineConfigurator
{
    /// <summary>Creates response event.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The created response event.</returns>
    Event<T> CreateResponseEvent<T>()
        where T : class;

    /// <summary>Set the Future's result to the specified value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="responseReceived">The response received.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    void SetResult<T>(Event<T> responseReceived, Func<BehaviorContext<FutureState, T>, Task> callback)
        where T : class;

    /// <summary>Set the Future to the Faulted state, and set the Fault message.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="requestCompleted">The request completed.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    void SetFaulted<T>(Event<T> requestCompleted, Func<BehaviorContext<FutureState, T>, Task> callback)
        where T : class;

    /// <summary>Set the result for a pending request and remove the identifier.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="requestCompleted">The request completed.</param>
    /// <param name="pendingIdProvider">The pending id provider.</param>
    void CompletePendingRequest<T>(Event<T> requestCompleted, PendingFutureIdProvider<T> pendingIdProvider)
        where T : class;

    /// <summary>Add an event handler to the future.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="whenEvent">The when event.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    void DuringAnyWhen<T>(Event<T> whenEvent, Func<EventActivityBinder<FutureState, T>,
        EventActivityBinder<FutureState, T>> configure)
        where T : class;
}
