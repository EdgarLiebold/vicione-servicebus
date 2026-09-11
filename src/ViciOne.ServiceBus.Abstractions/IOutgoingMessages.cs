namespace ViciOne.ServiceBus;

/// <summary>Provides application-level send, publish, and schedule operations for the active consume scope.</summary>
public interface IOutgoingMessages
{
    /// <summary>Sends a message to the destination configured for its contract.</summary>
    /// <typeparam name="TMessage">The message contract type.</typeparam>
    /// <param name="message">The message to send.</param>
    /// <param name="cancellationToken">The token that cancels route resolution or delivery.</param>
    /// <returns>A task that completes when the destination transport has accepted the message.</returns>
    /// <exception cref="ConfigurationException">No unambiguous route is configured for <typeparamref name="TMessage" />.</exception>
    Task SendAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>Sends a message to an explicit destination with application-level options.</summary>
    /// <typeparam name="TMessage">The message contract type.</typeparam>
    /// <param name="destination">The destination address.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="options">The application metadata applied to the outgoing message.</param>
    /// <param name="cancellationToken">The token that cancels endpoint resolution or delivery.</param>
    /// <returns>A task that completes when the destination transport has accepted the message.</returns>
    Task SendAsync<TMessage>(Uri destination, TMessage message, SendOptions options, CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>Publishes a message to all matching subscriptions.</summary>
    /// <typeparam name="TMessage">The message contract type.</typeparam>
    /// <param name="message">The message to publish.</param>
    /// <param name="cancellationToken">The token that cancels publication.</param>
    /// <returns>A task that completes when the transport has accepted the publication.</returns>
    Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>Publishes a message with application-level options.</summary>
    /// <typeparam name="TMessage">The message contract type.</typeparam>
    /// <param name="message">The message to publish.</param>
    /// <param name="options">The application metadata applied to the publication.</param>
    /// <param name="cancellationToken">The token that cancels publication.</param>
    /// <returns>A task that completes when the transport has accepted the publication.</returns>
    Task PublishAsync<TMessage>(TMessage message, PublishOptions options, CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>Schedules a message for delivery to an explicit destination.</summary>
    /// <typeparam name="TMessage">The message contract type.</typeparam>
    /// <param name="destination">The destination address.</param>
    /// <param name="dueAt">The time at which the message becomes eligible for delivery.</param>
    /// <param name="message">The message to schedule.</param>
    /// <param name="cancellationToken">The token that cancels schedule acceptance.</param>
    /// <returns>A task that returns the message accepted for future delivery.</returns>
    /// <exception cref="ConfigurationException">The active consume scope has no message scheduler.</exception>
    Task<ScheduledMessage<TMessage>> ScheduleSendAsync<TMessage>(Uri destination, DateTimeOffset dueAt, TMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : class;
}
