using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Observables;

/// <summary>Dispatches receive observations over a stable snapshot of endpoint registrations.</summary>
public class ReceiveObservable :
    Connectable<IReceiveObserver>,
    IReceiveObserver
{
    /// <summary>Notifies observers before the receive pipeline processes a delivery.</summary>
    /// <param name="context">The received delivery.</param>
    /// <returns>A non-null task that completes after every invoked observer has finished.</returns>
    public Task PreReceiveAsync(ReceiveContext context)
    {
        return ForEachAsync(x => x.PreReceiveAsync(context));
    }

    /// <summary>Notifies observers after receive processing and successful transport settlement.</summary>
    /// <param name="context">The settled delivery.</param>
    /// <returns>A non-null task that completes after every invoked observer has finished.</returns>
    public Task PostReceiveAsync(ReceiveContext context)
    {
        return ForEachAsync(x => x.PostReceiveAsync(context));
    }

    /// <summary>Notifies observers that one consumer has completed successfully.</summary>
    /// <typeparam name="T">The consumed message contract.</typeparam>
    /// <param name="context">The completed consume context.</param>
    /// <param name="duration">The elapsed consumer execution time.</param>
    /// <param name="consumerType">The runtime consumer type used by the operation.</param>
    /// <returns>A non-null task that completes after every invoked observer has finished.</returns>
    public Task PostConsumeAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType)
        where T : class
    {
        return ForEachAsync(x => x.PostConsumeAsync(context, duration, consumerType));
    }

    /// <summary>Notifies observers that one consumer has faulted.</summary>
    /// <typeparam name="T">The consumed message contract.</typeparam>
    /// <param name="context">The failed consume context.</param>
    /// <param name="duration">The elapsed consumer execution time.</param>
    /// <param name="consumerType">The runtime consumer type used by the operation.</param>
    /// <param name="exception">The consumer failure.</param>
    /// <returns>A non-null task that completes after every invoked observer has finished.</returns>
    public Task ConsumeFaultAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception)
        where T : class
    {
        return ForEachAsync(x => x.ConsumeFaultAsync(context, duration, consumerType, exception));
    }

    /// <summary>Notifies observers that receive processing or transport settlement has faulted.</summary>
    /// <param name="context">The failed delivery.</param>
    /// <param name="exception">The receive or settlement failure.</param>
    /// <returns>A non-null task that completes after every invoked observer has finished.</returns>
    public Task ReceiveFaultAsync(ReceiveContext context, Exception exception)
    {
        return ForEachAsync(x => x.ReceiveFaultAsync(context, exception));
    }
}
