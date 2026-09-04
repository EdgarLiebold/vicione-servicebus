using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Advanced.Initializers;

/// <summary>
/// Provides extension methods for send endpoint.
/// </summary>
public static class SendEndpointExtensions
{
    /// <summary>
    /// Send a dynamically typed message initialized by a loosely typed dictionary of values. ViciOne.ServiceBus will
    /// create and populate an object instance with the properties of the <paramref name="values" /> argument.
    /// </summary>
    /// <param name="messageType">The message type to publish</param>
    /// <param name="values">
    /// The dictionary of values to become hydrated and published under the type of the interface.
    /// </param>
    /// <param name="cancellationToken"></param>
    /// <param name="publishEndpoint"></param>
    public static Task SendAsync(this ISendEndpoint publishEndpoint, Type messageType, object values, CancellationToken cancellationToken = default)
    {
        return SendEndpointConverterCache.SendInitializerAsync(publishEndpoint, messageType, values, cancellationToken);
    }

    /// <summary>
    /// Send a dynamically typed message initialized by a loosely typed dictionary of values. ViciOne.ServiceBus will
    /// create and populate an object instance with the properties of the <paramref name="values" /> argument.
    /// </summary>
    /// <param name="messageType">The message type to publish</param>
    /// <param name="values">
    /// The dictionary of values to become hydrated and published under the type of the interface.
    /// </param>
    /// <param name="pipe"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="publishEndpoint"></param>
    public static Task SendAsync(this ISendEndpoint publishEndpoint, Type messageType, object values, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default)
    {
        return SendEndpointConverterCache.SendInitializerAsync(publishEndpoint, messageType, values, pipe, cancellationToken);
    }
}
