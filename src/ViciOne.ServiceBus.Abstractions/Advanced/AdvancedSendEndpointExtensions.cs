using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides low-level send operations outside the application API namespace.</summary>
public static class AdvancedSendEndpointExtensions
{
    /// <summary>Returns the advanced send contract implemented by the endpoint.</summary>
    /// <param name="endpoint">The endpoint.</param>
    /// <returns>The advanced send endpoint produced by the operation.</returns>
    public static IAdvancedSendEndpoint Advanced(this ISendEndpoint endpoint) =>
        RequireAdvanced(endpoint);

    /// <summary>Sends a typed message through a typed send-context pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="endpoint">The endpoint used by the operation.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipe used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static Task SendAsync<T>(this ISendEndpoint endpoint, T message, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken = default)
        where T : class => RequireAdvanced(endpoint).SendAsync(message, pipe, cancellationToken);

    /// <summary>Sends a typed message through an untyped send-context pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="endpoint">The endpoint used by the operation.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipe used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static Task SendAsync<T>(this ISendEndpoint endpoint, T message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default)
        where T : class => RequireAdvanced(endpoint).SendAsync(message, pipe, cancellationToken);

    /// <summary>Sends a runtime-typed message.</summary>
    /// <param name="endpoint">The endpoint used by the operation.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static Task SendAsync(this ISendEndpoint endpoint, object message, CancellationToken cancellationToken = default) =>
        RequireAdvanced(endpoint).SendAsync(message, cancellationToken);

    /// <summary>Sends a message as the specified runtime type.</summary>
    /// <param name="endpoint">The endpoint used by the operation.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="messageType">The message type used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static Task SendAsync(this ISendEndpoint endpoint, object message, Type messageType,
        CancellationToken cancellationToken = default) =>
        RequireAdvanced(endpoint).SendAsync(message, messageType, cancellationToken);

    /// <summary>Sends a runtime-typed message through a send-context pipe.</summary>
    /// <param name="endpoint">The endpoint used by the operation.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipe used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static Task SendAsync(this ISendEndpoint endpoint, object message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default) =>
        RequireAdvanced(endpoint).SendAsync(message, pipe, cancellationToken);

    /// <summary>Sends a message as the specified runtime type through a send-context pipe.</summary>
    /// <param name="endpoint">The endpoint used by the operation.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="messageType">The message type used by the operation.</param>
    /// <param name="pipe">The pipe used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static Task SendAsync(this ISendEndpoint endpoint, object message, Type messageType, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default) =>
        RequireAdvanced(endpoint).SendAsync(message, messageType, pipe, cancellationToken);

    static IAdvancedSendEndpoint RequireAdvanced(ISendEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        return endpoint as IAdvancedSendEndpoint
            ?? throw new NotSupportedException($"The send endpoint '{endpoint.GetType().FullName}' does not expose advanced send operations.");
    }
}
