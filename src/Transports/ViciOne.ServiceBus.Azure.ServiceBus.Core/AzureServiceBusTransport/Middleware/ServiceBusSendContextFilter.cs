using System.Threading.Tasks;

namespace ViciOne.ServiceBus.AzureServiceBusTransport.Middleware;

public class ServiceBusSendContextFilter<T> :
    IFilter<SendContext<T>>
    where T : class
{
    readonly IFilter<ServiceBusSendContext<T>> _filter;

    public ServiceBusSendContextFilter(IFilter<ServiceBusSendContext<T>> filter)
    {
        _filter = filter;
    }

    public Task SendAsync(SendContext<T> context, IPipe<SendContext<T>> next)
    {
        return context.TryGetPayload(out ServiceBusSendContext<T>? serviceBusSendContext)
            ? _filter.SendAsync(serviceBusSendContext, next)
            : next.SendAsync(context);
    }

    public void Probe(ProbeContext context)
    {
    }
}
