using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Observables;

/// <summary>Publishes observations for send.</summary>
public class SendObservable :
    Connectable<ISendObserver>,
    ISendObserver
{
    /// <summary>Runs before send.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PreSendAsync<T>(SendContext<T> context)
        where T : class
    {
        return ForEachAsync(x => x.PreSendAsync(context));
    }

    /// <summary>Runs after send.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PostSendAsync<T>(SendContext<T> context)
        where T : class
    {
        return ForEachAsync(x => x.PostSendAsync(context));
    }

    /// <summary>Sends fault.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SendFaultAsync<T>(SendContext<T> context, Exception exception)
        where T : class
    {
        return ForEachAsync(x => x.SendFaultAsync(context, exception));
    }
}
