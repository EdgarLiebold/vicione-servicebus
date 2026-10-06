using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals.Outgoing;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Exposes low-level send forms for middleware, serializers, initializers, and transport integrations.
/// Application code should prefer <see cref="ISendEndpoint"/>.
/// </summary>
/// <remarks>
/// Completion follows the selected endpoint policy. Buffered sends require explicit <c>IBufferedBus.FlushAsync</c>;
/// <c>UseVolatileOutbox</c> dispatches captured work after successful consumption and discards it on failure.
/// A transactional EF outbox requires <c>IEntityFrameworkTransactionalOutbox.CommitAsync</c>; a caller-owned outer transaction still requires its own commit.
/// See <see href="https://github.com/EdgarLiebold/vicione-servicebus/blob/main/docs/api/completion-boundaries.md">endpoint completion boundaries</see>.
/// </remarks>
public interface IAdvancedSendEndpoint :
    ISendEndpoint
{
    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="message">The message instance to send.</param>
    /// <param name="options">The application-level send options.</param>
    /// <param name="cancellationToken">The token that cancels sending.</param>
    /// <returns>A task that completes the configured endpoint operation. Buffer or outbox policy may complete it after local capture, before transport dispatch; completion does not confirm delivery or consumption.</returns>
    Task ISendEndpoint.SendAsync<T>(T message, SendOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        return SendAsync(message, new SendOptionsPipe<T>(options), cancellationToken);
    }

    /// <summary>Sends a typed message through a typed send-context pipe.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="message">The message instance to send.</param>
    /// <param name="pipe">The pipeline applied to the typed send context.</param>
    /// <param name="cancellationToken">The token that cancels sending.</param>
    /// <returns>A task that completes the configured endpoint operation. Buffer or outbox policy may complete it after local capture, before transport dispatch; completion does not confirm delivery or consumption.</returns>
    Task SendAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Sends a typed message through an untyped send-context pipe.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="message">The message instance to send.</param>
    /// <param name="pipe">The pipeline applied to the untyped send context.</param>
    /// <param name="cancellationToken">The token that cancels sending.</param>
    /// <returns>A task that completes the configured endpoint operation. Buffer or outbox policy may complete it after local capture, before transport dispatch; completion does not confirm delivery or consumption.</returns>
    Task SendAsync<T>(T message, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Sends a runtime-typed message.</summary>
    /// <param name="message">The message instance whose runtime type is sent.</param>
    /// <param name="cancellationToken">The token that cancels sending.</param>
    /// <returns>A task that completes the configured endpoint operation. Buffer or outbox policy may complete it after local capture, before transport dispatch; completion does not confirm delivery or consumption.</returns>
    Task SendAsync(object message, CancellationToken cancellationToken = default);

    /// <summary>Sends a message as the specified runtime type.</summary>
    /// <param name="message">The message instance to send.</param>
    /// <param name="messageType">The message contract type to declare to the send pipeline.</param>
    /// <param name="cancellationToken">The token that cancels sending.</param>
    /// <returns>A task that completes the configured endpoint operation. Buffer or outbox policy may complete it after local capture, before transport dispatch; completion does not confirm delivery or consumption.</returns>
    Task SendAsync(object message, Type messageType, CancellationToken cancellationToken = default);

    /// <summary>Sends a runtime-typed message through a send-context pipe.</summary>
    /// <param name="message">The message instance whose runtime type is sent.</param>
    /// <param name="pipe">The pipeline applied to the untyped send context.</param>
    /// <param name="cancellationToken">The token that cancels sending.</param>
    /// <returns>A task that completes the configured endpoint operation. Buffer or outbox policy may complete it after local capture, before transport dispatch; completion does not confirm delivery or consumption.</returns>
    Task SendAsync(object message, IPipe<SendContext> pipe, CancellationToken cancellationToken = default);

    /// <summary>Sends a message as the specified runtime type through a send-context pipe.</summary>
    /// <param name="message">The message instance to send.</param>
    /// <param name="messageType">The message contract type to declare to the send pipeline.</param>
    /// <param name="pipe">The pipeline applied to the untyped send context.</param>
    /// <param name="cancellationToken">The token that cancels sending.</param>
    /// <returns>A task that completes the configured endpoint operation. Buffer or outbox policy may complete it after local capture, before transport dispatch; completion does not confirm delivery or consumption.</returns>
    Task SendAsync(object message, Type messageType, IPipe<SendContext> pipe, CancellationToken cancellationToken = default);

    /// <summary>Initializes and sends a message from property values.</summary>
    /// <typeparam name="T">The message contract type to initialize.</typeparam>
    /// <param name="values">The property values used to initialize the message.</param>
    /// <param name="cancellationToken">The token that cancels initialization or sending.</param>
    /// <returns>A task that completes the configured endpoint operation. Buffer or outbox policy may complete it after local capture, before transport dispatch; completion does not confirm delivery or consumption.</returns>
    Task SendAsync<T>(object values, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Initializes and sends a message through a typed send-context pipe.</summary>
    /// <typeparam name="T">The message contract type to initialize.</typeparam>
    /// <param name="values">The property values used to initialize the message.</param>
    /// <param name="pipe">The pipeline applied to the typed send context.</param>
    /// <param name="cancellationToken">The token that cancels initialization or sending.</param>
    /// <returns>A task that completes the configured endpoint operation. Buffer or outbox policy may complete it after local capture, before transport dispatch; completion does not confirm delivery or consumption.</returns>
    Task SendAsync<T>(object values, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Initializes and sends a message through an untyped send-context pipe.</summary>
    /// <typeparam name="T">The message contract type to initialize.</typeparam>
    /// <param name="values">The property values used to initialize the message.</param>
    /// <param name="pipe">The pipeline applied to the untyped send context.</param>
    /// <param name="cancellationToken">The token that cancels initialization or sending.</param>
    /// <returns>A task that completes the configured endpoint operation. Buffer or outbox policy may complete it after local capture, before transport dispatch; completion does not confirm delivery or consumption.</returns>
    Task SendAsync<T>(object values, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where T : class;
}
