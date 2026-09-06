using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides low-level publish operations outside the application API namespace.</summary>
public static class AdvancedPublishEndpointExtensions
{
    /// <summary>Returns the advanced publish contract implemented by the endpoint.</summary>
    /// <param name="endpoint">The endpoint.</param>
    /// <returns>The advanced publish endpoint produced by the operation.</returns>
    public static IAdvancedPublishEndpoint Advanced(this IPublishEndpoint endpoint) =>
        RequireAdvanced(endpoint);

    /// <summary>Publishes a typed message through a typed publish-context pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="endpoint">The endpoint used by the operation.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipe used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static Task PublishAsync<T>(this IPublishEndpoint endpoint, T message, IPipe<PublishContext<T>> pipe,
        CancellationToken cancellationToken = default)
        where T : class => RequireAdvanced(endpoint).PublishAsync(message, pipe, cancellationToken);

    /// <summary>Publishes a typed message through an untyped publish-context pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="endpoint">The endpoint used by the operation.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipe used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static Task PublishAsync<T>(this IPublishEndpoint endpoint, T message, IPipe<PublishContext> pipe,
        CancellationToken cancellationToken = default)
        where T : class => RequireAdvanced(endpoint).PublishAsync(message, pipe, cancellationToken);

    /// <summary>Publishes a runtime-typed message.</summary>
    /// <param name="endpoint">The endpoint used by the operation.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static Task PublishAsync(this IPublishEndpoint endpoint, object message, CancellationToken cancellationToken = default) =>
        RequireAdvanced(endpoint).PublishAsync(message, cancellationToken);

    /// <summary>Publishes a runtime-typed message through a publish-context pipe.</summary>
    /// <param name="endpoint">The endpoint used by the operation.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipe used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static Task PublishAsync(this IPublishEndpoint endpoint, object message, IPipe<PublishContext> pipe,
        CancellationToken cancellationToken = default) =>
        RequireAdvanced(endpoint).PublishAsync(message, pipe, cancellationToken);

    /// <summary>Publishes a message as the specified runtime type.</summary>
    /// <param name="endpoint">The endpoint used by the operation.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="messageType">The message type used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static Task PublishAsync(this IPublishEndpoint endpoint, object message, Type messageType,
        CancellationToken cancellationToken = default) =>
        RequireAdvanced(endpoint).PublishAsync(message, messageType, cancellationToken);

    /// <summary>Publishes a message as the specified runtime type through a publish-context pipe.</summary>
    /// <param name="endpoint">The endpoint used by the operation.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="messageType">The message type used by the operation.</param>
    /// <param name="pipe">The pipe used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static Task PublishAsync(this IPublishEndpoint endpoint, object message, Type messageType, IPipe<PublishContext> pipe,
        CancellationToken cancellationToken = default) =>
        RequireAdvanced(endpoint).PublishAsync(message, messageType, pipe, cancellationToken);

    static IAdvancedPublishEndpoint RequireAdvanced(IPublishEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        return endpoint as IAdvancedPublishEndpoint
            ?? throw new NotSupportedException($"The publish endpoint '{endpoint.GetType().FullName}' does not expose advanced publish operations.");
    }
}
