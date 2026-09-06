using System;
using ViciOne.ServiceBus.AzureServiceBus.Middleware;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>Adds a filter that derives the Azure Service Bus session identifier for outgoing messages.</summary>
/// <typeparam name="T">The sent message contract.</typeparam>
public class SetSessionIdMessageSendTopology<T> :
    IMessageSendTopology<T>
    where T : class
{
    readonly IFilter<SendContext<T>> _filter;

    /// <summary>Creates topology backed by a session identifier formatter.</summary>
    /// <param name="sessionIdFormatter">The formatter invoked for each outgoing message.</param>
    public SetSessionIdMessageSendTopology(IMessageSessionIdFormatter<T> sessionIdFormatter)
    {
        if (sessionIdFormatter == null)
            throw new ArgumentNullException(nameof(sessionIdFormatter));

        _filter = new ServiceBusSendContextFilter<T>(new SetSessionIdFilter<T>(sessionIdFormatter));
    }

    /// <summary>Adds the session identifier filter to the send topology pipe.</summary>
    /// <param name="builder">The send topology builder.</param>
    public void Apply(ITopologyPipeBuilder<SendContext<T>> builder)
    {
        builder.AddFilter(_filter);
    }
}
