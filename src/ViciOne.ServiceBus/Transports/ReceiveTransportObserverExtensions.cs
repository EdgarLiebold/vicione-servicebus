using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Events;

namespace ViciOne.ServiceBus.Transports;

public static class ReceiveTransportObserverExtensions
{
    public static Task NotifyReady(this IReceiveTransportObserver observer, Uri inputAddress, bool isStarted = true)
    {
        return observer.Ready(new ReceiveTransportReadyEvent(inputAddress, isStarted));
    }

    public static Task NotifyCompleted(this IReceiveTransportObserver observer, Uri inputAddress, DeliveryMetrics metrics)
    {
        return observer.Completed(new ReceiveTransportCompletedEvent(inputAddress, metrics));
    }

    public static Task NotifyFaulted(this IReceiveTransportObserver observer, Uri inputAddress, Exception exception, bool isTerminal)
    {
        return observer.Faulted(new ReceiveTransportFaultedEvent(inputAddress, exception, isTerminal));
    }
}
