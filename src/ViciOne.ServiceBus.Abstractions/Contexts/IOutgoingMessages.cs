namespace ViciOne.ServiceBus;

/// <summary>
/// Exposes application-level outgoing operations bound to the active consume scope and its configured outbox.
/// </summary>
public interface IOutgoingMessages
{
    /// <summary>Sends a message using its configured route.</summary>
    /// <param name="message">The message processed by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task SendAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Sends a message to an explicit destination with application-level options.</summary>
    /// <param name="destination">The destination used by the operation.</param>
    /// <param name="message">The message processed by the operation.</param>
    /// <param name="options">The options used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task SendAsync<T>(Uri destination, T message, SendOptions options, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Publishes a message.</summary>
    /// <param name="message">The message processed by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Publishes a message with application-level options.</summary>
    /// <param name="message">The message processed by the operation.</param>
    /// <param name="options">The options used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task PublishAsync<T>(T message, PublishOptions options, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Schedules a message for delivery to an explicit destination.</summary>
    /// <param name="destination">The destination used by the operation.</param>
    /// <param name="dueAt">The due at used by the operation.</param>
    /// <param name="message">The message processed by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destination, DateTimeOffset dueAt, T message,
        CancellationToken cancellationToken = default)
        where T : class;
}

sealed class ConsumeContextOutgoingMessages(ConsumeContext context) :
    IOutgoingMessages
{
    public async Task SendAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);

        ISendEndpointProvider provider = context.ReceiveContext.SendEndpointProvider;
        if (provider is not IMessageRouteProvider routeProvider ||
            !routeProvider.MessageRoutes.TryGetDestinationAddress<T>(out Uri destination))
        {
            throw new ConfigurationException($"A message route for {typeof(T).FullName} is not configured on this bus.");
        }

        ISendEndpoint endpoint = await context.GetSendEndpointAsync(destination, cancellationToken).ConfigureAwait(false);
        await endpoint.SendAsync(message, cancellationToken).ConfigureAwait(false);
    }

    public async Task SendAsync<T>(Uri destination, T message, SendOptions options,
        CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(options);

        ISendEndpoint endpoint = await context.GetSendEndpointAsync(destination, cancellationToken).ConfigureAwait(false);
        await endpoint.SendAsync(message, options, cancellationToken).ConfigureAwait(false);
    }

    public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);
        return context.PublishAsync(message, cancellationToken);
    }

    public Task PublishAsync<T>(T message, PublishOptions options, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(options);
        return context.PublishAsync(message, options, cancellationToken);
    }

    public Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destination, DateTimeOffset dueAt, T message,
        CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(message);

        if (!context.TryGetPayload(out MessageSchedulerContext? scheduler))
            throw new ConfigurationException("No message scheduler is available in the active consume context.");

        return scheduler.ScheduleSendAsync(destination, dueAt, message, cancellationToken);
    }
}
