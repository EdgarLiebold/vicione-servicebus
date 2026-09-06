using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Middleware.Outbox;

/// <summary>Provides an endpoint for outbox send.</summary>
public class OutboxSendEndpoint :
    ITransportSendEndpoint
{
    readonly OutboxSendContext _context;
    readonly ITransportSendEndpoint _endpoint;

    /// <summary>Creates an send endpoint on the outbox.</summary>
    /// <param name="outboxContext">The outbox context for this consume operation.</param>
    /// <param name="endpoint">The endpoint.</param>
    public OutboxSendEndpoint(OutboxSendContext outboxContext, ISendEndpoint endpoint)
    {
        _context = outboxContext;
        _endpoint = endpoint as ITransportSendEndpoint ?? throw new ArgumentException("Must be a transport endpoint", nameof(endpoint));
    }

    /// <summary>Gets the endpoint.</summary>
    public ISendEndpoint Endpoint => _endpoint;

    /// <summary>Connects send observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        return new EmptyConnectHandle();
    }

    /// <summary>Creates send context.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the created value.</returns>
    public Task<SendContext<T>> CreateSendContextAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        return _endpoint.CreateSendContextAsync(message, new OutboxSendEndpointPipe<T>(pipe, _context), cancellationToken);
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SendAsync<T>(T message, CancellationToken cancellationToken)
        where T : class
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        SendContext<T> context =
            await _endpoint.CreateSendContextAsync(message, new OutboxSendEndpointPipe<T>(_context), cancellationToken).ConfigureAwait(false);

        await AddSendAsync(context).ConfigureAwait(false);
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SendAsync(object message, CancellationToken cancellationToken)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var messageType = message.GetType();

        return SendEndpointConverterCache.SendAsync(this, message, messageType, cancellationToken);
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SendAsync(object message, Type messageType, CancellationToken cancellationToken)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (messageType == null)
            throw new ArgumentNullException(nameof(messageType));

        return SendEndpointConverterCache.SendAsync(this, message, messageType, cancellationToken);
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SendAsync(object message, IPipe<SendContext> pipe, CancellationToken cancellationToken)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));

        var messageType = message.GetType();

        return SendEndpointConverterCache.SendAsync(this, message, messageType, pipe, cancellationToken);
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SendAsync(object message, Type messageType, IPipe<SendContext> pipe, CancellationToken cancellationToken)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (messageType == null)
            throw new ArgumentNullException(nameof(messageType));
        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));

        return SendEndpointConverterCache.SendAsync(this, message, messageType, pipe, cancellationToken);
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="values">The values.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="values">The values.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SendAsync<T>(object values, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        (var message, IPipe<SendContext<T>> sendPipe) =
            await MessageInitializerCache<T>.InitializeMessageAsync(values, new OutboxSendEndpointPipe<T>(pipe, _context), cancellationToken)
                .ConfigureAwait(false);

        SendContext<T> context =
            await _endpoint.CreateSendContextAsync(message, new OutboxSendEndpointPipe<T>(sendPipe, _context), cancellationToken).ConfigureAwait(false);

        await AddSendAsync(context).ConfigureAwait(false);
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="values">The values.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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

        StartedActivity? activity = LogContext.Current?.StartOutboxSendActivity(context);
        var instrument = LogContext.Current?.StartOutboxEnqueueInstrument();
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
