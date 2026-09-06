using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Observables;

/// <summary>Publishes observations for receive.</summary>
public class ReceiveObservable :
    Connectable<IReceiveObserver>,
    IReceiveObserver
{
    /// <summary>Runs before receive.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PreReceiveAsync(ReceiveContext context)
    {
        return ForEachAsync(x => x.PreReceiveAsync(context));
    }

    /// <summary>Runs after receive.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PostReceiveAsync(ReceiveContext context)
    {
        return ForEachAsync(x => x.PostReceiveAsync(context));
    }

    /// <summary>Runs after consume.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="duration">The duration.</param>
    /// <param name="consumerType">The runtime consumer type used by the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PostConsumeAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType)
        where T : class
    {
        return ForEachAsync(x => x.PostConsumeAsync(context, duration, consumerType));
    }

    /// <summary>Consumes fault.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="duration">The duration.</param>
    /// <param name="consumerType">The runtime consumer type used by the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ConsumeFaultAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception)
        where T : class
    {
        return ForEachAsync(x => x.ConsumeFaultAsync(context, duration, consumerType, exception));
    }

    /// <summary>Receives fault.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ReceiveFaultAsync(ReceiveContext context, Exception exception)
    {
        return ForEachAsync(x => x.ReceiveFaultAsync(context, exception));
    }
}
