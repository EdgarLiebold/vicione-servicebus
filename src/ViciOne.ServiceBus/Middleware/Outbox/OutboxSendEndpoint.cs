using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Internals.Dispatching;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Logging.Diagnostics;
using ViciOne.ServiceBus.Logging.Monitoring;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Middleware.Outbox;

/// <summary>Captures outgoing messages in a durable outbox instead of delivering them immediately.</summary>
internal sealed class OutboxSendEndpoint :
    ITransportSendEndpoint
{
    readonly OutboxSendContext _context;
    readonly ITransportSendEndpoint _endpoint;

    /// <summary>Initializes an outbox endpoint over a transport endpoint.</summary>
    /// <param name="outboxContext">The outbox context that captures outgoing messages.</param>
    /// <param name="endpoint">The transport endpoint used to create send contexts.</param>
    public OutboxSendEndpoint(OutboxSendContext outboxContext, ISendEndpoint endpoint)
    {
        _context = outboxContext ?? throw new ArgumentNullException(nameof(outboxContext));
        ArgumentNullException.ThrowIfNull(endpoint);
        _endpoint = endpoint as ITransportSendEndpoint ?? throw new ArgumentException("Must be a transport endpoint", nameof(endpoint));
    }

    /// <summary>Gets the wrapped transport endpoint.</summary>
    public ISendEndpoint Endpoint => _endpoint;

    /// <summary>Registers an observer with the wrapped transport endpoint.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _endpoint.ConnectSendObserver(observer);
    }

    /// <summary>Creates a send context configured to expose the outbox consume scope.</summary>
    /// <typeparam name="T">The outgoing message contract.</typeparam>
    /// <param name="message">The outgoing message.</param>
    /// <param name="pipe">The pipe that customizes the send context.</param>
    /// <param name="cancellationToken">The token that cancels context creation.</param>
    /// <returns>A task containing the configured send context.</returns>
    public Task<SendContext<T>> CreateSendContextAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);
        return _endpoint.CreateSendContextAsync(message, new OutboxSendEndpointPipe<T>(pipe, _context), cancellationToken);
    }

    /// <summary>Captures a typed message for delivery to the configured destination after the outbox commits.</summary>
    /// <typeparam name="T">The outgoing message contract.</typeparam>
    /// <param name="message">The outgoing message.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the message has been captured.</returns>
    public async Task SendAsync<T>(T message, CancellationToken cancellationToken)
        where T : class
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        SendContext<T> context =
            await _endpoint.CreateSendContextAsync(message, new OutboxSendEndpointPipe<T>(_context), cancellationToken).ConfigureAwait(false);

        await AddSendAsync(context).ConfigureAwait(false);
    }

    /// <summary>Captures a typed message with typed send-context configuration for delivery after the outbox commits.</summary>
    /// <typeparam name="T">The outgoing message contract.</typeparam>
    /// <param name="message">The outgoing message.</param>
    /// <param name="pipe">The pipe that customizes the send context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the message has been captured.</returns>
    public async Task SendAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));

        SendContext<T> context = await _endpoint.CreateSendContextAsync(message, new OutboxSendEndpointPipe<T>(pipe, _context), cancellationToken)
            .ConfigureAwait(false);

        await AddSendAsync(context).ConfigureAwait(false);
    }

    /// <summary>Captures a message using its runtime type for delivery after the outbox commits.</summary>
    /// <param name="message">The outgoing message whose runtime type is its contract.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the message has been captured.</returns>
    public Task SendAsync(object message, CancellationToken cancellationToken)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var messageType = message.GetType();

        return SendEndpointDispatcher.SendAsync(this, message, messageType, cancellationToken);
    }

    /// <summary>Captures a runtime-typed message for delivery after the outbox commits.</summary>
    /// <param name="message">The outgoing message.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the message has been captured.</returns>
    public Task SendAsync(object message, Type messageType, CancellationToken cancellationToken)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (messageType == null)
            throw new ArgumentNullException(nameof(messageType));

        return SendEndpointDispatcher.SendAsync(this, message, messageType, cancellationToken);
    }

    /// <summary>Captures a typed message with untyped send-context configuration for delivery after the outbox commits.</summary>
    /// <typeparam name="T">The outgoing message contract.</typeparam>
    /// <param name="message">The outgoing message.</param>
    /// <param name="pipe">The untyped pipe that customizes the send context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the message has been captured.</returns>
    public async Task SendAsync<T>(T message, IPipe<SendContext> pipe, CancellationToken cancellationToken)
        where T : class
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));

        SendContext<T> context = await _endpoint.CreateSendContextAsync(message, new OutboxSendEndpointPipe<T>(pipe, _context), cancellationToken)
            .ConfigureAwait(false);

        await AddSendAsync(context).ConfigureAwait(false);
    }

    /// <summary>Captures a message using its runtime type and send-context configuration for delivery after the outbox commits.</summary>
    /// <param name="message">The outgoing message whose runtime type is its contract.</param>
    /// <param name="pipe">The pipe that customizes the send context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the message has been captured.</returns>
    public Task SendAsync(object message, IPipe<SendContext> pipe, CancellationToken cancellationToken)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));

        var messageType = message.GetType();

        return SendEndpointDispatcher.SendAsync(this, message, messageType, pipe, cancellationToken);
    }

    /// <summary>Captures a runtime-typed message with send-context configuration for delivery after the outbox commits.</summary>
    /// <param name="message">The outgoing message.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="pipe">The pipe that customizes the send context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the message has been captured.</returns>
    public Task SendAsync(object message, Type messageType, IPipe<SendContext> pipe, CancellationToken cancellationToken)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (messageType == null)
            throw new ArgumentNullException(nameof(messageType));
        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));

        return SendEndpointDispatcher.SendAsync(this, message, messageType, pipe, cancellationToken);
    }

    /// <summary>Initializes and captures a typed message for delivery after the outbox commits.</summary>
    /// <typeparam name="T">The message contract to initialize.</typeparam>
    /// <param name="values">The values used to initialize the outgoing message.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the initialized message has been captured.</returns>
    public async Task SendAsync<T>(object values, CancellationToken cancellationToken)
        where T : class
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        (var message, IPipe<SendContext<T>> sendPipe) =
            await MessageInitializerCache<T>.InitializeMessageAsync(values, new OutboxSendEndpointPipe<T>(_context), cancellationToken).ConfigureAwait(false);

        SendContext<T> context =
            await _endpoint.CreateSendContextAsync(message, new OutboxSendEndpointPipe<T>(sendPipe, _context), cancellationToken).ConfigureAwait(false);

        await AddSendAsync(context).ConfigureAwait(false);
    }

    /// <summary>Initializes and captures a typed message with typed send-context configuration for delivery after the outbox commits.</summary>
    /// <typeparam name="T">The message contract to initialize.</typeparam>
    /// <param name="values">The values used to initialize the outgoing message.</param>
    /// <param name="pipe">The typed pipe that customizes the send context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the initialized message has been captured.</returns>
    public async Task SendAsync<T>(object values, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));
        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));

        (var message, IPipe<SendContext<T>> sendPipe) =
            await MessageInitializerCache<T>.InitializeMessageAsync(values, new OutboxSendEndpointPipe<T>(pipe, _context), cancellationToken)
                .ConfigureAwait(false);

        SendContext<T> context =
            await _endpoint.CreateSendContextAsync(message, new OutboxSendEndpointPipe<T>(sendPipe, _context), cancellationToken).ConfigureAwait(false);

        await AddSendAsync(context).ConfigureAwait(false);
    }

    /// <summary>Initializes and captures a typed message with untyped send-context configuration for delivery after the outbox commits.</summary>
    /// <typeparam name="T">The message contract to initialize.</typeparam>
    /// <param name="values">The values used to initialize the outgoing message.</param>
    /// <param name="pipe">The untyped pipe that customizes the send context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the initialized message has been captured.</returns>
    public async Task SendAsync<T>(object values, IPipe<SendContext> pipe, CancellationToken cancellationToken)
        where T : class
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));
        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));

        (var message, IPipe<SendContext<T>> sendPipe) =
            await MessageInitializerCache<T>.InitializeMessageAsync(values, new OutboxSendEndpointPipe<T>(pipe, _context), cancellationToken)
                .ConfigureAwait(false);

        SendContext<T> context =
            await _endpoint.CreateSendContextAsync(message, new OutboxSendEndpointPipe<T>(sendPipe, _context), cancellationToken).ConfigureAwait(false);

        await AddSendAsync(context).ConfigureAwait(false);
    }

    async Task AddSendAsync<T>(SendContext<T> context)
        where T : class
    {
        if (ForwardingExpiration.TryDiscard(context))
            return;

        StartedActivity? activity = MessageActivity.TryStartOutboxSend(context);
        var instrument = LogContext.Current?.TryStartOutboxEnqueueMetrics();
        try
        {
            await _context.AddSendAsync(context).ConfigureAwait(false);
            activity?.Update(context);
        }
        catch (Exception ex)
        {
            activity?.AddExceptionEvent(ex);
            instrument?.RecordException(ex);
            throw;
        }
        finally
        {
            activity?.Stop();
            instrument?.Complete();
        }
    }


    class OutboxSendEndpointPipe<T> :
        SendContextPipeAdapter<T>
        where T : class
    {
        readonly IServiceProvider _provider;

        public OutboxSendEndpointPipe(IServiceProvider provider)
            : base(null)
        {
            _provider = provider;
        }

        public OutboxSendEndpointPipe(IPipe<SendContext<T>> pipe, IServiceProvider provider)
            : base(pipe)
        {
            _provider = provider;
        }

        protected override void Send<TMessage>(SendContext<TMessage> context)
        {
            context.GetOrAddPayload(() => _provider);
        }

        protected override void Send(SendContext<T> context)
        {
            context.ConversationId ??= NewId.NextGuid();
        }
    }
}
