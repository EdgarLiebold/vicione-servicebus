using System.Threading.Tasks;

namespace ViciOne.ServiceBus.AzureServiceBusTransport.Middleware;

public class SetSessionIdFilter<T> :
    IFilter<ServiceBusSendContext<T>>
    where T : class
{
    readonly IMessageSessionIdFormatter<T> _sessionIdFormatter;

    public SetSessionIdFilter(IMessageSessionIdFormatter<T> sessionIdFormatter)
    {
        _sessionIdFormatter = sessionIdFormatter;
    }

    public Task SendAsync(ServiceBusSendContext<T> context, IPipe<ServiceBusSendContext<T>> next)
    {
        var sessionId = _sessionIdFormatter.FormatSessionId(context);

        if (!string.IsNullOrWhiteSpace(sessionId))
            context.SessionId = sessionId;

        return next.SendAsync(context);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("setSessionId");
    }
}
