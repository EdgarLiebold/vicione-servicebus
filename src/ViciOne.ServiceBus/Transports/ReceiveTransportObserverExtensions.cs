using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Events;

namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Provides extension methods for receive transport observer.
/// </summary>
public static class ReceiveTransportObserverExtensions
{
    /// <summary>
    /// Performs the notify ready operation.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <param name="inputAddress">The input address value.</param>
    /// <param name="isStarted">The is started value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static Task NotifyReadyAsync(this IReceiveTransportObserver observer, Uri inputAddress, bool isStarted = true, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return observer.ReadyAsync(new ReceiveTransportReadyEvent(inputAddress, isStarted));
    }

    /// <summary>
    /// Performs the notify completed operation.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <param name="inputAddress">The input address value.</param>
    /// <param name="metrics">The metrics value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static Task NotifyCompletedAsync(this IReceiveTransportObserver observer, Uri inputAddress, DeliveryMetrics metrics, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return observer.CompletedAsync(new ReceiveTransportCompletedEvent(inputAddress, metrics));
    }

    /// <summary>
    /// Performs the notify faulted operation.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <param name="inputAddress">The input address value.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="isTerminal">The is terminal value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static Task NotifyFaultedAsync(this IReceiveTransportObserver observer, Uri inputAddress, Exception exception, bool isTerminal, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return observer.FaultedAsync(new ReceiveTransportFaultedEvent(inputAddress, exception, isTerminal));
    }
}
