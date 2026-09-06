using System.Threading.Tasks;

namespace ViciOne.ServiceBus.AzureServiceBus.Middleware;

/// <summary>Runs a provider-specific send filter when the generic context exposes Azure Service Bus state.</summary>
/// <typeparam name="T">The message type being sent.</typeparam>
public class ServiceBusSendContextFilter<T> :
    IFilter<SendContext<T>>
    where T : class
{
    readonly IFilter<ServiceBusSendContext<T>> _filter;

    /// <summary>Initializes the adapter for a provider-specific filter.</summary>
    /// <param name="filter">The Azure Service Bus send-context filter to invoke.</param>
    public ServiceBusSendContextFilter(IFilter<ServiceBusSendContext<T>> filter)
    {
        _filter = filter;
    }

    /// <summary>Invokes the provider-specific filter when its context payload is available; otherwise continues the generic pipeline.</summary>
    /// <param name="context">The generic send context.</param>
    /// <param name="next">The remaining generic pipeline.</param>
    /// <returns>The task from the Azure-specific filter or generic continuation selected for <paramref name="context"/>.</returns>
    public Task SendAsync(SendContext<T> context, IPipe<SendContext<T>> next)
    {
        return context.TryGetPayload(out ServiceBusSendContext<T>? serviceBusSendContext)
            ? _filter.SendAsync(serviceBusSendContext, next)
            : next.SendAsync(context);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The probe context intentionally left unchanged by this adapter.</param>
    public void Probe(ProbeContext context)
    {
    }
}
