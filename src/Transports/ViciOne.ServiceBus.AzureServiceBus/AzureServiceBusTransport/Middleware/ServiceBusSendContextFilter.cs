using System.Threading.Tasks;

namespace ViciOne.ServiceBus.AzureServiceBus.Middleware;

/// <summary>
/// Provides a service bus send context filter implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class ServiceBusSendContextFilter<T> :
    IFilter<SendContext<T>>
    where T : class
{
    readonly IFilter<ServiceBusSendContext<T>> _filter;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="filter">The filter value.</param>
    public ServiceBusSendContextFilter(IFilter<ServiceBusSendContext<T>> filter)
    {
        _filter = filter;
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync(SendContext<T> context, IPipe<SendContext<T>> next)
    {
        return context.TryGetPayload(out ServiceBusSendContext<T>? serviceBusSendContext)
            ? _filter.SendAsync(serviceBusSendContext, next)
            : next.SendAsync(context);
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
    }
}
