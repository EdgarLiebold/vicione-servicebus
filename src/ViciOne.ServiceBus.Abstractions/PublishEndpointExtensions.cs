using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Advanced.Initializers;

/// <summary>Provides extension methods for publish endpoint.</summary>
public static class PublishEndpointExtensions
{
    /// <summary>
    /// Publish a dynamically typed message initialized by a loosely typed dictionary of values. ViciOne.ServiceBus will
    /// create and populate an object instance with the properties of the <paramref name="values" /> argument.
    /// </summary>
    /// <param name="publishEndpoint">The publish endpoint.</param>
    /// <param name="messageType">The message type to publish.</param>
    /// <param name="values">The dictionary of values to become hydrated and published under the type of the interface.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static Task PublishAsync(this IPublishEndpoint publishEndpoint, Type messageType, object values, CancellationToken cancellationToken = default)
    {
        return PublishEndpointConverterCache.PublishInitializerAsync(publishEndpoint, messageType, values, cancellationToken);
    }

    /// <summary>
    /// Publish a dynamically typed message initialized by a loosely typed dictionary of values. ViciOne.ServiceBus will
    /// create and populate an object instance with the properties of the <paramref name="values" /> argument.
    /// </summary>
    /// <param name="publishEndpoint">The publish endpoint.</param>
    /// <param name="messageType">The message type to publish.</param>
    /// <param name="values">The dictionary of values to become hydrated and published under the type of the interface.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static Task PublishAsync(this IPublishEndpoint publishEndpoint, Type messageType, object values, IPipe<PublishContext> pipe,
        CancellationToken cancellationToken = default)
    {
        return PublishEndpointConverterCache.PublishInitializerAsync(publishEndpoint, messageType, values, pipe, cancellationToken);
    }
}
