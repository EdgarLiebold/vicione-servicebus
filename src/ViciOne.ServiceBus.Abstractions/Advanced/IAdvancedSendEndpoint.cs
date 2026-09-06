using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Exposes low-level send forms for middleware, serializers, initializers, and transport integrations.
/// Application code should prefer <see cref="ISendEndpoint"/>.
/// </summary>
public interface IAdvancedSendEndpoint :
    ISendEndpoint
{
    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ISendEndpoint.SendAsync<T>(T message, SendOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        return SendAsync(message, new SendOptionsPipe<T>(options), cancellationToken);
    }

    /// <summary>Sends a typed message through a typed send-context pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipe used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SendAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Sends a typed message through an untyped send-context pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipe used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SendAsync<T>(T message, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Sends a runtime-typed message.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SendAsync(object message, CancellationToken cancellationToken = default);

    /// <summary>Sends a message as the specified runtime type.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="messageType">The message type used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SendAsync(object message, Type messageType, CancellationToken cancellationToken = default);

    /// <summary>Sends a runtime-typed message through a send-context pipe.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipe used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SendAsync(object message, IPipe<SendContext> pipe, CancellationToken cancellationToken = default);

    /// <summary>Sends a message as the specified runtime type through a send-context pipe.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="messageType">The message type used by the operation.</param>
    /// <param name="pipe">The pipe used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SendAsync(object message, Type messageType, IPipe<SendContext> pipe, CancellationToken cancellationToken = default);

    /// <summary>Initializes and sends a message from property values.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="values">The values used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SendAsync<T>(object values, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Initializes and sends a message through a typed send-context pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="values">The values used by the operation.</param>
    /// <param name="pipe">The pipe used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SendAsync<T>(object values, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Initializes and sends a message through an untyped send-context pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="values">The values used by the operation.</param>
    /// <param name="pipe">The pipe used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SendAsync<T>(object values, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where T : class;
}
