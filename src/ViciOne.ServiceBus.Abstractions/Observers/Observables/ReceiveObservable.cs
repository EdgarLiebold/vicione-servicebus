using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Observables;

/// <summary>
/// Provides a receive observable implementation.
/// </summary>
public class ReceiveObservable :
    Connectable<IReceiveObserver>,
    IReceiveObserver
{
    /// <summary>
    /// Performs the pre receive operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task PreReceiveAsync(ReceiveContext context)
    {
        return ForEachAsync(x => x.PreReceiveAsync(context));
    }

    /// <summary>
    /// Performs the post receive operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task PostReceiveAsync(ReceiveContext context)
    {
        return ForEachAsync(x => x.PostReceiveAsync(context));
    }

    /// <summary>
    /// Performs the post consume operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="duration">The duration value.</param>
    /// <param name="consumerType">The consumer type value.</param>
    /// <returns>The result of the operation.</returns>
    public Task PostConsumeAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType)
        where T : class
    {
        return ForEachAsync(x => x.PostConsumeAsync(context, duration, consumerType));
    }

    /// <summary>
    /// Consumes fault.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="duration">The duration value.</param>
    /// <param name="consumerType">The consumer type value.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task ConsumeFaultAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception)
        where T : class
    {
        return ForEachAsync(x => x.ConsumeFaultAsync(context, duration, consumerType, exception));
    }

    /// <summary>
    /// Performs the receive fault operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task ReceiveFaultAsync(ReceiveContext context, Exception exception)
    {
        return ForEachAsync(x => x.ReceiveFaultAsync(context, exception));
    }
}
