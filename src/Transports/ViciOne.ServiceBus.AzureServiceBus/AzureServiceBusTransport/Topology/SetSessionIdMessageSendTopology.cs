using System;
using ViciOne.ServiceBus.AzureServiceBus.Middleware;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>
/// Provides a set session id message send topology implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class SetSessionIdMessageSendTopology<T> :
    IMessageSendTopology<T>
    where T : class
{
    readonly IFilter<SendContext<T>> _filter;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="sessionIdFormatter">The session id formatter value.</param>
    public SetSessionIdMessageSendTopology(IMessageSessionIdFormatter<T> sessionIdFormatter)
    {
        if (sessionIdFormatter == null)
            throw new ArgumentNullException(nameof(sessionIdFormatter));

        _filter = new ServiceBusSendContextFilter<T>(new SetSessionIdFilter<T>(sessionIdFormatter));
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Apply(ITopologyPipeBuilder<SendContext<T>> builder)
    {
        builder.AddFilter(_filter);
    }
}
