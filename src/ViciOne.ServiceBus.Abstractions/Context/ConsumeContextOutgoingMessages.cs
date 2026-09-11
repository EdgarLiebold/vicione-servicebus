using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Providers.Configuration;

namespace ViciOne.ServiceBus.Context;

/// <summary>Executes application-level outgoing operations through one consume context.</summary>
internal sealed class ConsumeContextOutgoingMessages :
    IOutgoingMessages
{
    readonly ConsumeContext _context;

    /// <summary>Creates an outgoing facade whose operations inherit the supplied consume scope.</summary>
    /// <param name="context">The consume context that owns outgoing endpoints, payloads, and completion.</param>
    internal ConsumeContextOutgoingMessages(ConsumeContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task SendAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);

        ISendEndpointProvider provider = _context.ReceiveContext.SendEndpointProvider;
        if (provider is not IMessageRouteProvider routeProvider ||
            !routeProvider.MessageRoutes.TryGetDestinationAddress<TMessage>(out Uri? destination))
        {
            throw new ConfigurationException(ConfigurationMessages.Create(
                "Consume Context Outgoing Messages",
                "unknown",
                $"A message route for {typeof(TMessage).FullName} is not configured on this bus.",
                "Correct the named configuration before starting the host"));
        }

        ISendEndpoint endpoint = await _context.GetSendEndpointAsync(destination, cancellationToken).ConfigureAwait(false);
        await endpoint.SendAsync(message, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SendAsync<TMessage>(Uri destination, TMessage message, SendOptions options,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(options);

        ISendEndpoint endpoint = await _context.GetSendEndpointAsync(destination, cancellationToken).ConfigureAwait(false);
        await endpoint.SendAsync(message, options, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);
        return _context.PublishAsync(message, cancellationToken);
    }

    /// <inheritdoc />
    public Task PublishAsync<TMessage>(TMessage message, PublishOptions options, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(options);
        return _context.PublishAsync(message, options, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ScheduledMessage<TMessage>> ScheduleSendAsync<TMessage>(Uri destination, DateTimeOffset dueAt, TMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(message);

        if (!_context.TryGetPayload(out MessageSchedulerContext? scheduler))
        {
            throw new ConfigurationException(ConfigurationMessages.Create(
                "Consume Context Outgoing Messages",
                "unknown",
                "No message scheduler is available in the active consume context.",
                "Correct the named configuration before starting the host"));
        }

        return scheduler.ScheduleSendAsync(destination, dueAt, message, cancellationToken);
    }
}
