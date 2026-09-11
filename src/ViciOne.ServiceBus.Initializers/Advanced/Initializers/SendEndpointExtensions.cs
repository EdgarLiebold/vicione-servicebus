using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Internals.Dispatching;

namespace ViciOne.ServiceBus.Advanced.Initializers;

/// <summary>Sends runtime-selected contracts initialized from property values.</summary>
public static class SendEndpointExtensions
{
    /// <summary>Initializes and sends a runtime-selected message contract.</summary>
    /// <param name="endpoint">The endpoint that sends the initialized message.</param>
    /// <param name="messageType">The message contract to initialize and send.</param>
    /// <param name="values">The object whose public properties supply message values.</param>
    /// <param name="cancellationToken">The token that cancels the send.</param>
    /// <returns>A task that completes when the initialized message has been sent to the transport.</returns>
    public static Task SendAsync(this ISendEndpoint endpoint, Type messageType, object values, CancellationToken cancellationToken = default)
    {
        return SendEndpointDispatcher.SendInitializerAsync(endpoint, messageType, values, cancellationToken);
    }

    /// <summary>Initializes and sends a runtime-selected message contract through a send pipeline.</summary>
    /// <param name="endpoint">The endpoint that sends the initialized message.</param>
    /// <param name="messageType">The message contract to initialize and send.</param>
    /// <param name="values">The object whose public properties supply message values.</param>
    /// <param name="pipe">The pipeline that configures the send context.</param>
    /// <param name="cancellationToken">The token that cancels the send.</param>
    /// <returns>A task that completes when the initialized message has been sent to the transport.</returns>
    public static Task SendAsync(this ISendEndpoint endpoint, Type messageType, object values, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default)
    {
        return SendEndpointDispatcher.SendInitializerAsync(endpoint, messageType, values, pipe, cancellationToken);
    }
}
