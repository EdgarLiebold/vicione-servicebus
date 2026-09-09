using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides low-level publish operations outside the application API namespace.</summary>
public static class AdvancedPublishEndpointExtensions
{
    /// <summary>Returns the advanced publish contract implemented by the endpoint.</summary>
    /// <param name="endpoint">The publish endpoint to expose as an infrastructure contract.</param>
    /// <returns>The endpoint's advanced publish contract.</returns>
    public static IAdvancedPublishEndpoint Advanced(this IPublishEndpoint endpoint) =>
        RequireAdvanced(endpoint);

    /// <summary>Publishes a typed message through a typed publish-context pipe.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="endpoint">The endpoint that publishes the message.</param>
    /// <param name="message">The message instance to publish.</param>
    /// <param name="pipe">The pipeline applied to the typed publish context.</param>
    /// <param name="cancellationToken">The token that cancels publication.</param>
    /// <returns>A task that completes after the transport accepts the publication.</returns>
    public static Task PublishAsync<T>(this IPublishEndpoint endpoint, T message, IPipe<PublishContext<T>> pipe,
        CancellationToken cancellationToken = default)
        where T : class
    {
        IAdvancedPublishEndpoint advanced = RequireAdvanced(endpoint);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);

        return advanced.PublishAsync(message, pipe, cancellationToken);
    }

    /// <summary>Publishes a typed message through an untyped publish-context pipe.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="endpoint">The endpoint that publishes the message.</param>
    /// <param name="message">The message instance to publish.</param>
    /// <param name="pipe">The pipeline applied to the untyped publish context.</param>
    /// <param name="cancellationToken">The token that cancels publication.</param>
    /// <returns>A task that completes after the transport accepts the publication.</returns>
    public static Task PublishAsync<T>(this IPublishEndpoint endpoint, T message, IPipe<PublishContext> pipe,
        CancellationToken cancellationToken = default)
        where T : class
    {
        IAdvancedPublishEndpoint advanced = RequireAdvanced(endpoint);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);

        return advanced.PublishAsync(message, pipe, cancellationToken);
    }

    /// <summary>Publishes a runtime-typed message through a publish-context pipe.</summary>
    /// <param name="endpoint">The endpoint that publishes the message.</param>
    /// <param name="message">The message instance whose runtime type is published.</param>
    /// <param name="pipe">The pipeline applied to the untyped publish context.</param>
    /// <param name="cancellationToken">The token that cancels publication.</param>
    /// <returns>A task that completes after the transport accepts the publication.</returns>
    public static Task PublishAsync(this IPublishEndpoint endpoint, object message, IPipe<PublishContext> pipe,
        CancellationToken cancellationToken = default)
    {
        IAdvancedPublishEndpoint advanced = RequireAdvanced(endpoint);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);

        return advanced.PublishAsync(message, pipe, cancellationToken);
    }

    /// <summary>Publishes a message as the specified runtime type.</summary>
    /// <param name="endpoint">The endpoint that publishes the message.</param>
    /// <param name="message">The message instance to publish.</param>
    /// <param name="messageType">The message contract type to declare to the publish pipeline.</param>
    /// <param name="cancellationToken">The token that cancels publication.</param>
    /// <returns>A task that completes after the transport accepts the publication.</returns>
    public static Task PublishAsync(this IPublishEndpoint endpoint, object message, Type messageType,
        CancellationToken cancellationToken = default)
    {
        IAdvancedPublishEndpoint advanced = RequireAdvanced(endpoint);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageType);

        return advanced.PublishAsync(message, messageType, cancellationToken);
    }

    /// <summary>Publishes a message as the specified runtime type through a publish-context pipe.</summary>
    /// <param name="endpoint">The endpoint that publishes the message.</param>
    /// <param name="message">The message instance to publish.</param>
    /// <param name="messageType">The message contract type to declare to the publish pipeline.</param>
    /// <param name="pipe">The pipeline applied to the untyped publish context.</param>
    /// <param name="cancellationToken">The token that cancels publication.</param>
    /// <returns>A task that completes after the transport accepts the publication.</returns>
    public static Task PublishAsync(this IPublishEndpoint endpoint, object message, Type messageType, IPipe<PublishContext> pipe,
        CancellationToken cancellationToken = default)
    {
        IAdvancedPublishEndpoint advanced = RequireAdvanced(endpoint);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageType);
        ArgumentNullException.ThrowIfNull(pipe);

        return advanced.PublishAsync(message, messageType, pipe, cancellationToken);
    }

    static IAdvancedPublishEndpoint RequireAdvanced(IPublishEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        return endpoint as IAdvancedPublishEndpoint
            ?? throw new NotSupportedException($"The publish endpoint '{endpoint.GetType().FullName}' does not expose advanced publish operations.");
    }
}
