using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Observables;

/// <summary>
/// Provides a send observable implementation.
/// </summary>
public class SendObservable :
    Connectable<ISendObserver>,
    ISendObserver
{
    /// <summary>
    /// Performs the pre send operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task PreSendAsync<T>(SendContext<T> context)
        where T : class
    {
        return ForEachAsync(x => x.PreSendAsync(context));
    }

    /// <summary>
    /// Performs the post send operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task PostSendAsync<T>(SendContext<T> context)
        where T : class
    {
        return ForEachAsync(x => x.PostSendAsync(context));
    }

    /// <summary>
    /// Sends fault.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendFaultAsync<T>(SendContext<T> context, Exception exception)
        where T : class
    {
        return ForEachAsync(x => x.SendFaultAsync(context, exception));
    }
}
