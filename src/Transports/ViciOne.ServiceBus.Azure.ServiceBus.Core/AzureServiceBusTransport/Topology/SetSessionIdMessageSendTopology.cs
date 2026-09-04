using System;
using ViciOne.ServiceBus.AzureServiceBusTransport.Middleware;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.AzureServiceBusTransport.Topology;

public class SetSessionIdMessageSendTopology<T> :
    IMessageSendTopology<T>
    where T : class
{
    readonly IFilter<SendContext<T>> _filter;

    public SetSessionIdMessageSendTopology(IMessageSessionIdFormatter<T> sessionIdFormatter)
    {
        if (sessionIdFormatter == null)
            throw new ArgumentNullException(nameof(sessionIdFormatter));

        _filter = new ServiceBusSendContextFilter<T>(new SetSessionIdFilter<T>(sessionIdFormatter));
    }

    public void Apply(ITopologyPipeBuilder<SendContext<T>> builder)
    {
        builder.AddFilter(_filter);
    }
}
