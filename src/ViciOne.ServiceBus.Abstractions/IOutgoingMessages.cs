namespace ViciOne.ServiceBus;

/// <summary>Exposes application-level outgoing operations bound to the active consume scope and its configured outbox.</summary>
public interface IOutgoingMessages
{
    /// <summary>Sends a message using its configured route.</summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <param name="message">The message to send.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the send operation.</returns>
    Task SendAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>Sends a message to an explicit destination with application-level options.</summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <param name="destination">The destination address.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="options">The application-level send options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the send operation.</returns>
    Task SendAsync<TMessage>(Uri destination, TMessage message, SendOptions options, CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>Publishes a message.</summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <param name="message">The message to publish.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the publish operation.</returns>
    Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>Publishes a message with application-level options.</summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <param name="message">The message to publish.</param>
    /// <param name="options">The application-level publish options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the publish operation.</returns>
    Task PublishAsync<TMessage>(TMessage message, PublishOptions options, CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>Schedules a message for delivery to an explicit destination.</summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <param name="destination">The destination address.</param>
    /// <param name="dueAt">The delivery time.</param>
    /// <param name="message">The message to schedule.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task whose result identifies the scheduled message.</returns>
    Task<ScheduledMessage<TMessage>> ScheduleSendAsync<TMessage>(Uri destination, DateTimeOffset dueAt, TMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : class;
}

sealed class ConsumeContextOutgoingMessages(ConsumeContext context) :
    IOutgoingMessages
{
    public async Task SendAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);

        ISendEndpointProvider provider = context.ReceiveContext.SendEndpointProvider;
        if (provider is not IMessageRouteProvider routeProvider ||
            !routeProvider.MessageRoutes.TryGetDestinationAddress<TMessage>(out Uri? destination))
        {
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Consume Context Outgoing Messages", "unknown", $"A message route for {typeof(TMessage).FullName} is not configured on this bus.", "Correct the named configuration before starting the host"));
        }

        ISendEndpoint endpoint = await context.GetSendEndpointAsync(destination, cancellationToken).ConfigureAwait(false);
        await endpoint.SendAsync(message, cancellationToken).ConfigureAwait(false);
    }

    public async Task SendAsync<TMessage>(Uri destination, TMessage message, SendOptions options,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(options);

        ISendEndpoint endpoint = await context.GetSendEndpointAsync(destination, cancellationToken).ConfigureAwait(false);
        await endpoint.SendAsync(message, options, cancellationToken).ConfigureAwait(false);
    }

    public Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);
        return context.PublishAsync(message, cancellationToken);
    }

    public Task PublishAsync<TMessage>(TMessage message, PublishOptions options, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(options);
        return context.PublishAsync(message, options, cancellationToken);
    }

    public Task<ScheduledMessage<TMessage>> ScheduleSendAsync<TMessage>(Uri destination, DateTimeOffset dueAt, TMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(message);

        if (!context.TryGetPayload(out MessageSchedulerContext? scheduler))
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Consume Context Outgoing Messages", "unknown", "No message scheduler is available in the active consume context.", "Correct the named configuration before starting the host"));

        return scheduler.ScheduleSendAsync(destination, dueAt, message, cancellationToken);
    }
}
