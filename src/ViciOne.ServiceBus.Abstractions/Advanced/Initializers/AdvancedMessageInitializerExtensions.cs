using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced.Initializers;

/// <summary>Provides anonymous-value message initialization outside the application API namespace.</summary>
public static class AdvancedMessageInitializerExtensions
{
    /// <summary>Initializes and sends a message from property values.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="endpoint">The endpoint used by the operation.</param>
    /// <param name="values">The values used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static Task SendAsync<T>(this ISendEndpoint endpoint, object values, CancellationToken cancellationToken = default)
        where T : class => RequireAdvanced(endpoint).SendAsync<T>(values, cancellationToken);

    /// <summary>Initializes and sends a message through a typed send-context pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="endpoint">The endpoint used by the operation.</param>
    /// <param name="values">The values used by the operation.</param>
    /// <param name="pipe">The pipe used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static Task SendAsync<T>(this ISendEndpoint endpoint, object values, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken = default)
        where T : class => RequireAdvanced(endpoint).SendAsync(values, pipe, cancellationToken);

    /// <summary>Initializes and sends a message through an untyped send-context pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="endpoint">The endpoint used by the operation.</param>
    /// <param name="values">The values used by the operation.</param>
    /// <param name="pipe">The pipe used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static Task SendAsync<T>(this ISendEndpoint endpoint, object values, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default)
        where T : class => RequireAdvanced(endpoint).SendAsync<T>(values, pipe, cancellationToken);

    /// <summary>Initializes and publishes a message from property values.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="endpoint">The endpoint used by the operation.</param>
    /// <param name="values">The values used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static Task PublishAsync<T>(this IPublishEndpoint endpoint, object values, CancellationToken cancellationToken = default)
        where T : class => RequireAdvanced(endpoint).PublishAsync<T>(values, cancellationToken);

    /// <summary>Initializes and publishes a message through a typed publish-context pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="endpoint">The endpoint used by the operation.</param>
    /// <param name="values">The values used by the operation.</param>
    /// <param name="pipe">The pipe used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static Task PublishAsync<T>(this IPublishEndpoint endpoint, object values, IPipe<PublishContext<T>> pipe,
        CancellationToken cancellationToken = default)
        where T : class => RequireAdvanced(endpoint).PublishAsync(values, pipe, cancellationToken);

    /// <summary>Initializes and publishes a message through an untyped publish-context pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="endpoint">The endpoint used by the operation.</param>
    /// <param name="values">The values used by the operation.</param>
    /// <param name="pipe">The pipe used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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
