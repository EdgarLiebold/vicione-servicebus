using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Observables;

public class PublishObservable :
    Connectable<IPublishObserver>,
    IPublishObserver,
    ISendObserver
{
    public Task PrePublishAsync<T>(PublishContext<T> context)
        where T : class
    {
        return ForEachAsync(x => x.PrePublishAsync(context));
    }

    public Task PostPublishAsync<T>(PublishContext<T> context)
        where T : class
    {
        return ForEachAsync(x => x.PostPublishAsync(context));
    }

    public Task PublishFaultAsync<T>(PublishContext<T> context, Exception exception)
        where T : class
    {
        return ForEachAsync(x => x.PublishFaultAsync(context, exception));
    }

    public Task PreSendAsync<T>(SendContext<T> context)
        where T : class
    {
        var publishContext = context.GetPayload<PublishContext<T>>();

        return ForEachAsync(x => x.PrePublishAsync(publishContext));
    }

    public Task PostSendAsync<T>(SendContext<T> context)
        where T : class
    {
        var publishContext = context.GetPayload<PublishContext<T>>();

        return ForEachAsync(x => x.PostPublishAsync(publishContext));
    }

    public Task SendFaultAsync<T>(SendContext<T> context, Exception exception)
        where T : class
    {
        var publishContext = context.GetPayload<PublishContext<T>>();

        return ForEachAsync(x => x.PublishFaultAsync(publishContext, exception));
    }
}
