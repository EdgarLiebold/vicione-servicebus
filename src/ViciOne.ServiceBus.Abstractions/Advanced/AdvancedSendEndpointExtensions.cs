using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides low-level send operations outside the application API namespace.</summary>
public static class AdvancedSendEndpointExtensions
{
    /// <summary>Returns the advanced send contract implemented by the endpoint.</summary>
    /// <param name="endpoint">The send endpoint to expose as an infrastructure contract.</param>
    /// <returns>The endpoint's advanced send contract.</returns>
    public static IAdvancedSendEndpoint Advanced(this ISendEndpoint endpoint) =>
        RequireAdvanced(endpoint);

    /// <summary>Sends a typed message through a typed send-context pipe.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="endpoint">The destination endpoint.</param>
    /// <param name="message">The message instance to send.</param>
    /// <param name="pipe">The pipeline applied to the typed send context.</param>
    /// <param name="cancellationToken">The token that cancels sending.</param>
    /// <returns>A task that completes after the destination transport accepts the message.</returns>
    public static Task SendAsync<T>(this ISendEndpoint endpoint, T message, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken = default)
        where T : class
    {
        IAdvancedSendEndpoint advanced = RequireAdvanced(endpoint);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);

        return advanced.SendAsync(message, pipe, cancellationToken);
    }

    /// <summary>Sends a typed message through an untyped send-context pipe.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="endpoint">The destination endpoint.</param>
    /// <param name="message">The message instance to send.</param>
    /// <param name="pipe">The pipeline applied to the untyped send context.</param>
    /// <param name="cancellationToken">The token that cancels sending.</param>
    /// <returns>A task that completes after the destination transport accepts the message.</returns>
    public static Task SendAsync<T>(this ISendEndpoint endpoint, T message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default)
        where T : class
    {
        IAdvancedSendEndpoint advanced = RequireAdvanced(endpoint);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);

        return advanced.SendAsync(message, pipe, cancellationToken);
    }

    /// <summary>Sends a message as the specified runtime type.</summary>
    /// <param name="endpoint">The destination endpoint.</param>
    /// <param name="message">The message instance to send.</param>
    /// <param name="messageType">The message contract type to declare to the send pipeline.</param>
    /// <param name="cancellationToken">The token that cancels sending.</param>
    /// <returns>A task that completes after the destination transport accepts the message.</returns>
    public static Task SendAsync(this ISendEndpoint endpoint, object message, Type messageType,
        CancellationToken cancellationToken = default)
    {
        IAdvancedSendEndpoint advanced = RequireAdvanced(endpoint);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageType);

        return advanced.SendAsync(message, messageType, cancellationToken);
    }

    /// <summary>Sends a runtime-typed message through a send-context pipe.</summary>
    /// <param name="endpoint">The destination endpoint.</param>
    /// <param name="message">The message instance whose runtime type is sent.</param>
    /// <param name="pipe">The pipeline applied to the untyped send context.</param>
    /// <param name="cancellationToken">The token that cancels sending.</param>
    /// <returns>A task that completes after the destination transport accepts the message.</returns>
    public static Task SendAsync(this ISendEndpoint endpoint, object message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default)
    {
        IAdvancedSendEndpoint advanced = RequireAdvanced(endpoint);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);

        return advanced.SendAsync(message, pipe, cancellationToken);
    }

    /// <summary>Sends a message as the specified runtime type through a send-context pipe.</summary>
    /// <param name="endpoint">The destination endpoint.</param>
    /// <param name="message">The message instance to send.</param>
    /// <param name="messageType">The message contract type to declare to the send pipeline.</param>
    /// <param name="pipe">The pipeline applied to the untyped send context.</param>
    /// <param name="cancellationToken">The token that cancels sending.</param>
    /// <returns>A task that completes after the destination transport accepts the message.</returns>
    public static Task SendAsync(this ISendEndpoint endpoint, object message, Type messageType, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default)
    {
        IAdvancedSendEndpoint advanced = RequireAdvanced(endpoint);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageType);
        ArgumentNullException.ThrowIfNull(pipe);

        return advanced.SendAsync(message, messageType, pipe, cancellationToken);
    }

    static IAdvancedSendEndpoint RequireAdvanced(ISendEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        return endpoint as IAdvancedSendEndpoint
            ?? throw new NotSupportedException($"The send endpoint '{endpoint.GetType().FullName}' does not expose advanced send operations.");
    }
}
