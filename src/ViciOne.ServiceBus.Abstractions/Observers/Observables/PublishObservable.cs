using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Observables;

/// <summary>
/// Provides a publish observable implementation.
/// </summary>
public class PublishObservable :
    Connectable<IPublishObserver>,
    IPublishObserver,
    ISendObserver
{
    /// <summary>
    /// Performs the pre publish operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task PrePublishAsync<T>(PublishContext<T> context)
        where T : class
    {
        return ForEachAsync(x => x.PrePublishAsync(context));
    }

    /// <summary>
    /// Performs the post publish operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task PostPublishAsync<T>(PublishContext<T> context)
        where T : class
    {
        return ForEachAsync(x => x.PostPublishAsync(context));
    }

    /// <summary>
    /// Publishes fault.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task PublishFaultAsync<T>(PublishContext<T> context, Exception exception)
        where T : class
    {
        return ForEachAsync(x => x.PublishFaultAsync(context, exception));
    }

    /// <summary>
    /// Performs the pre send operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task PreSendAsync<T>(SendContext<T> context)
        where T : class
    {
        var publishContext = context.GetPayload<PublishContext<T>>();

        return ForEachAsync(x => x.PrePublishAsync(publishContext));
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
        var publishContext = context.GetPayload<PublishContext<T>>();

        return ForEachAsync(x => x.PostPublishAsync(publishContext));
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
        var publishContext = context.GetPayload<PublishContext<T>>();

        return ForEachAsync(x => x.PublishFaultAsync(publishContext, exception));
    }
}
