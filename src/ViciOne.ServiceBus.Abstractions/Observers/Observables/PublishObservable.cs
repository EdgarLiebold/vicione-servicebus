using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Observables;

/// <summary>Publishes observations for publish.</summary>
public class PublishObservable :
    Connectable<IPublishObserver>,
    IPublishObserver,
    ISendObserver
{
    /// <summary>Runs before publish.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PrePublishAsync<T>(PublishContext<T> context)
        where T : class
    {
        return ForEachAsync(x => x.PrePublishAsync(context));
    }

    /// <summary>Runs after publish.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PostPublishAsync<T>(PublishContext<T> context)
        where T : class
    {
        return ForEachAsync(x => x.PostPublishAsync(context));
    }

    /// <summary>Publishes fault.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PublishFaultAsync<T>(PublishContext<T> context, Exception exception)
        where T : class
    {
        return ForEachAsync(x => x.PublishFaultAsync(context, exception));
    }

    /// <summary>Runs before send.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PreSendAsync<T>(SendContext<T> context)
        where T : class
    {
        var publishContext = context.GetPayload<PublishContext<T>>();

        return ForEachAsync(x => x.PrePublishAsync(publishContext));
    }

    /// <summary>Runs after send.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PostSendAsync<T>(SendContext<T> context)
        where T : class
    {
        var publishContext = context.GetPayload<PublishContext<T>>();

        return ForEachAsync(x => x.PostPublishAsync(publishContext));
    }

    /// <summary>Sends fault.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SendFaultAsync<T>(SendContext<T> context, Exception exception)
        where T : class
    {
        var publishContext = context.GetPayload<PublishContext<T>>();

        return ForEachAsync(x => x.PublishFaultAsync(publishContext, exception));
    }
}
