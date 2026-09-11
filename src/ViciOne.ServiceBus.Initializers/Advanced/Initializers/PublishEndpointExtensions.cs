using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Internals.Dispatching;

namespace ViciOne.ServiceBus.Advanced.Initializers;

/// <summary>Publishes runtime-selected contracts initialized from property values.</summary>
public static class PublishEndpointExtensions
{
    /// <summary>Initializes and publishes a runtime-selected message contract.</summary>
    /// <param name="publishEndpoint">The endpoint that publishes the initialized message.</param>
    /// <param name="messageType">The message contract to initialize and publish.</param>
    /// <param name="values">The object whose public properties supply message values.</param>
    /// <param name="cancellationToken">The token that cancels publication.</param>
    /// <returns>A task that completes when the initialized message has been sent to the transport.</returns>
    public static Task PublishAsync(this IPublishEndpoint publishEndpoint, Type messageType, object values, CancellationToken cancellationToken = default)
    {
        return PublishEndpointDispatcher.PublishInitializerAsync(publishEndpoint, messageType, values, cancellationToken);
    }

    /// <summary>Initializes and publishes a runtime-selected message contract through a publish pipeline.</summary>
    /// <param name="publishEndpoint">The endpoint that publishes the initialized message.</param>
    /// <param name="messageType">The message contract to initialize and publish.</param>
    /// <param name="values">The object whose public properties supply message values.</param>
    /// <param name="pipe">The pipeline that configures the publish context.</param>
    /// <param name="cancellationToken">The token that cancels publication.</param>
    /// <returns>A task that completes when the initialized message has been sent to the transport.</returns>
    public static Task PublishAsync(this IPublishEndpoint publishEndpoint, Type messageType, object values, IPipe<PublishContext> pipe,
        CancellationToken cancellationToken = default)
    {
        return PublishEndpointDispatcher.PublishInitializerAsync(publishEndpoint, messageType, values, pipe, cancellationToken);
    }
}
