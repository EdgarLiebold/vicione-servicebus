using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced.Initializers;

/// <summary>Provides advanced send and publish operations that initialize contract messages from property values.</summary>
public static class AdvancedMessageInitializerExtensions
{
    /// <summary>Initializes and sends a message from property values.</summary>
    /// <typeparam name="T">The message contract to initialize and send.</typeparam>
    /// <param name="endpoint">The destination endpoint.</param>
    /// <param name="values">An object whose public properties provide the message values.</param>
    /// <param name="cancellationToken">The token that cancels message initialization or delivery.</param>
    /// <returns>A task that completes when the transport accepts the message.</returns>
    public static Task SendAsync<T>(this ISendEndpoint endpoint, object values, CancellationToken cancellationToken = default)
        where T : class => RequireAdvanced(endpoint).SendAsync<T>(values, cancellationToken);

    /// <summary>Initializes and sends a message through a typed send-context pipe.</summary>
    /// <typeparam name="T">The message contract to initialize and send.</typeparam>
    /// <param name="endpoint">The destination endpoint.</param>
    /// <param name="values">An object whose public properties provide the message values.</param>
    /// <param name="pipe">The pipe that configures the typed send context.</param>
    /// <param name="cancellationToken">The token that cancels message initialization or delivery.</param>
    /// <returns>A task that completes when the transport accepts the message.</returns>
    public static Task SendAsync<T>(this ISendEndpoint endpoint, object values, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken = default)
        where T : class => RequireAdvanced(endpoint).SendAsync(values, pipe, cancellationToken);

    /// <summary>Initializes and sends a message through an untyped send-context pipe.</summary>
    /// <typeparam name="T">The message contract to initialize and send.</typeparam>
    /// <param name="endpoint">The destination endpoint.</param>
    /// <param name="values">An object whose public properties provide the message values.</param>
    /// <param name="pipe">The pipe that configures the untyped send context.</param>
    /// <param name="cancellationToken">The token that cancels message initialization or delivery.</param>
    /// <returns>A task that completes when the transport accepts the message.</returns>
    public static Task SendAsync<T>(this ISendEndpoint endpoint, object values, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default)
        where T : class => RequireAdvanced(endpoint).SendAsync<T>(values, pipe, cancellationToken);

    /// <summary>Initializes and publishes a message from property values.</summary>
    /// <typeparam name="T">The message contract to initialize and publish.</typeparam>
    /// <param name="endpoint">The publish endpoint.</param>
    /// <param name="values">An object whose public properties provide the message values.</param>
    /// <param name="cancellationToken">The token that cancels message initialization or publication.</param>
    /// <returns>A task that completes when the transport accepts the publication.</returns>
    public static Task PublishAsync<T>(this IPublishEndpoint endpoint, object values, CancellationToken cancellationToken = default)
        where T : class => RequireAdvanced(endpoint).PublishAsync<T>(values, cancellationToken);

    /// <summary>Initializes and publishes a message through a typed publish-context pipe.</summary>
    /// <typeparam name="T">The message contract to initialize and publish.</typeparam>
    /// <param name="endpoint">The publish endpoint.</param>
    /// <param name="values">An object whose public properties provide the message values.</param>
    /// <param name="pipe">The pipe that configures the typed publish context.</param>
    /// <param name="cancellationToken">The token that cancels message initialization or publication.</param>
    /// <returns>A task that completes when the transport accepts the publication.</returns>
    public static Task PublishAsync<T>(this IPublishEndpoint endpoint, object values, IPipe<PublishContext<T>> pipe,
        CancellationToken cancellationToken = default)
        where T : class => RequireAdvanced(endpoint).PublishAsync(values, pipe, cancellationToken);

    /// <summary>Initializes and publishes a message through an untyped publish-context pipe.</summary>
    /// <typeparam name="T">The message contract to initialize and publish.</typeparam>
    /// <param name="endpoint">The publish endpoint.</param>
    /// <param name="values">An object whose public properties provide the message values.</param>
    /// <param name="pipe">The pipe that configures the untyped publish context.</param>
    /// <param name="cancellationToken">The token that cancels message initialization or publication.</param>
    /// <returns>A task that completes when the transport accepts the publication.</returns>
    public static Task PublishAsync<T>(this IPublishEndpoint endpoint, object values, IPipe<PublishContext> pipe,
        CancellationToken cancellationToken = default)
        where T : class => RequireAdvanced(endpoint).PublishAsync<T>(values, pipe, cancellationToken);

    static IAdvancedSendEndpoint RequireAdvanced(ISendEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        return endpoint as IAdvancedSendEndpoint
            ?? throw new NotSupportedException($"The send endpoint '{endpoint.GetType().FullName}' does not expose initializer operations.");
    }

    static IAdvancedPublishEndpoint RequireAdvanced(IPublishEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        return endpoint as IAdvancedPublishEndpoint
            ?? throw new NotSupportedException($"The publish endpoint '{endpoint.GetType().FullName}' does not expose initializer operations.");
    }
}
