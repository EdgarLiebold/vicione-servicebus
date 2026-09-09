using System;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides request routing, response connections, endpoint resolution, and time to a client factory.</summary>
public interface ClientFactoryContext :
    IConsumePipeConnector,
    IRequestPipeConnector
{
    /// <summary>Gets the timeout used when a request does not specify one.</summary>
    RequestTimeout DefaultTimeout { get; }

    /// <summary>Gets the time source used for request deadlines and timeout timers.</summary>
    TimeProvider TimeProvider { get; }

    /// <summary>Gets the message routes owned by the bus that created this context.</summary>
    IMessageRouteTable MessageRoutes { get; }

    /// <summary>Gets the address to which request responders send replies.</summary>
    Uri ResponseAddress { get; }

    /// <summary>Gets the route-resolved send endpoint for a request contract.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <param name="consumeContext">The consumed message whose outbound metadata is propagated, or <see langword="null" />.</param>
    /// <returns>The endpoint that sends requests of type <typeparamref name="T" />.</returns>
    IRequestSendEndpoint<T> GetRequestEndpoint<T>(ConsumeContext? consumeContext = default)
        where T : class;

    /// <summary>Gets the send endpoint for a request contract at an explicit destination.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <param name="destinationAddress">The address to which requests are sent.</param>
    /// <param name="consumeContext">The consumed message whose outbound metadata is propagated, or <see langword="null" />.</param>
    /// <returns>The endpoint that sends requests of type <typeparamref name="T" />.</returns>
    IRequestSendEndpoint<T> GetRequestEndpoint<T>(Uri destinationAddress, ConsumeContext? consumeContext = default)
        where T : class;
}
