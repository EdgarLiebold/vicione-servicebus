using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Events;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Publishes strongly typed receive-transport lifecycle events to an observer.</summary>
public static class ReceiveTransportObserverExtensions
{
    /// <summary>Notifies an observer that a receive transport is available.</summary>
    /// <param name="observer">The observer to notify.</param>
    /// <param name="inputAddress">The transport input address.</param>
    /// <param name="isStarted"><see langword="true" /> when the endpoint owns a started transport.</param>
    /// <param name="cancellationToken">The token that cancels notification before dispatch.</param>
    /// <returns>The task returned by the observer.</returns>
    public static Task NotifyReadyAsync(this IReceiveTransportObserver observer, Uri inputAddress, bool isStarted = true, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(observer);
        ArgumentNullException.ThrowIfNull(inputAddress);

        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        return observer.ReadyAsync(new ReceiveTransportReadyEvent(inputAddress, isStarted))
            ?? throw new InvalidOperationException("The receive transport observer returned no readiness task.");
    }

    /// <summary>Notifies an observer that a receive transport completed.</summary>
    /// <param name="observer">The observer to notify.</param>
    /// <param name="inputAddress">The transport input address.</param>
    /// <param name="metrics">The final delivery measurements.</param>
    /// <param name="cancellationToken">The token that cancels notification before dispatch.</param>
    /// <returns>The task returned by the observer.</returns>
    public static Task NotifyCompletedAsync(this IReceiveTransportObserver observer, Uri inputAddress, IDeliveryMetrics metrics, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(observer);
        ArgumentNullException.ThrowIfNull(inputAddress);
        ArgumentNullException.ThrowIfNull(metrics);

        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        return observer.CompletedAsync(new ReceiveTransportCompletedEvent(inputAddress, metrics))
            ?? throw new InvalidOperationException("The receive transport observer returned no completion task.");
    }

    /// <summary>Notifies an observer that a receive transport faulted.</summary>
    /// <param name="observer">The observer to notify.</param>
    /// <param name="inputAddress">The transport input address.</param>
    /// <param name="exception">The transport failure.</param>
    /// <param name="isTerminal"><see langword="true" /> when no retry will follow.</param>
    /// <param name="cancellationToken">The token that cancels notification before dispatch.</param>
    /// <returns>The task returned by the observer.</returns>
    public static Task NotifyFaultedAsync(this IReceiveTransportObserver observer, Uri inputAddress, Exception exception, bool isTerminal, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(observer);
        ArgumentNullException.ThrowIfNull(inputAddress);
        ArgumentNullException.ThrowIfNull(exception);

        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        return observer.FaultedAsync(new ReceiveTransportFaultedEvent(inputAddress, exception, isTerminal))
            ?? throw new InvalidOperationException("The receive transport observer returned no fault task.");
    }
}
