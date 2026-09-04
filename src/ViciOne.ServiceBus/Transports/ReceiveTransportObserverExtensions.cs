using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Events;

namespace ViciOne.ServiceBus.Transports;

public static class ReceiveTransportObserverExtensions
{
    public static Task NotifyReadyAsync(this IReceiveTransportObserver observer, Uri inputAddress, bool isStarted = true, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return observer.ReadyAsync(new ReceiveTransportReadyEvent(inputAddress, isStarted));
    }

    public static Task NotifyCompletedAsync(this IReceiveTransportObserver observer, Uri inputAddress, DeliveryMetrics metrics, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return observer.CompletedAsync(new ReceiveTransportCompletedEvent(inputAddress, metrics));
    }

    public static Task NotifyFaultedAsync(this IReceiveTransportObserver observer, Uri inputAddress, Exception exception, bool isTerminal, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return observer.FaultedAsync(new ReceiveTransportFaultedEvent(inputAddress, exception, isTerminal));
    }
}
