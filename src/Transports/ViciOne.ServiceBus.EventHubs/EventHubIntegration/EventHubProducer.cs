using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>
/// Provides an event hub producer implementation.
/// </summary>
public class EventHubProducer :
    Supervisor,
    IAsyncDisposable,
    IEventHubProducer
{
    readonly ConnectHandle? _connectHandle;
    readonly EventHubSendTransportContext _context;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="connectHandle">The connect handle value.</param>
    public EventHubProducer(EventHubSendTransportContext context, ConnectHandle? connectHandle = null)
    {
        _context = context;
        _connectHandle = connectHandle;

        foreach (var handle in _context.GetAgentHandles())
            Add(handle);
    }

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public async ValueTask DisposeAsync()
    {
        _connectHandle?.Disconnect();
        await this.StopAsync("Disposing Agent").ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the produce operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task ProduceAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class
    {
        return ProduceAsync(message, Pipe.Empty<EventHubSendContext<T>>(), cancellationToken);
    }

    /// <summary>
    /// Performs the produce operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="messages">The messages value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task ProduceAsync<T>(IEnumerable<T> messages, CancellationToken cancellationToken = default)
        where T : class
    {
        return ProduceAsync(messages, Pipe.Empty<EventHubSendContext<T>>(), cancellationToken);
    }

    /// <summary>
    /// Performs the produce operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task ProduceAsync<T>(T message, IPipe<EventHubSendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        return _context.SendAsync(new SendPipe<T>(message, _context, pipe, cancellationToken), cancellationToken);
    }

    /// <summary>
    /// Performs the produce operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="messages">The messages value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task ProduceAsync<T>(IEnumerable<T> messages, IPipe<EventHubSendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        return _context.SendAsync(new BatchSendPipe<T>(messages, _context, pipe, cancellationToken), cancellationToken);
    }

    /// <summary>
    /// Performs the produce operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="values">The values value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task ProduceAsync<T>(object values, CancellationToken cancellationToken = default)
        where T : class
    {
        return ProduceAsync(values, Pipe.Empty<EventHubSendContext<T>>(), cancellationToken);
    }

    /// <summary>
    /// Performs the produce operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="values">The values value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task ProduceAsync<T>(IEnumerable<object> values, CancellationToken cancellationToken = default)
        where T : class
    {
        return ProduceAsync(values, Pipe.Empty<EventHubSendContext<T>>(), cancellationToken);
    }

    /// <summary>
    /// Performs the produce operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="values">The values value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task ProduceAsync<T>(object values, IPipe<EventHubSendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        (var message, IPipe<SendContext<T>> sendPipe) = await MessageInitializerCache<T>.InitializeMessageAsync(values, cancellationToken);

        await _context.SendAsync(new SendPipe<T>(message, _context, pipe, cancellationToken, sendPipe), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the produce operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="values">The values value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task ProduceAsync<T>(IEnumerable<object> values, IPipe<EventHubSendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>[] contexts = await Task.WhenAll(values.Select(value => MessageInitializerCache<T>.InitializeMessageAsync(value, cancellationToken)))
            .ConfigureAwait(false);

        await _context.SendAsync(new BatchSendPipe<T>(contexts.Select(x => x.Message), _context, pipe, cancellationToken, contexts.Select(x => x.Pipe)),
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Connects send observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        return _context.ConnectSendObserver(observer);
    }


    class SendPipe<T> :
        IPipe<ProducerContext>
        where T : class
    {
        readonly CancellationToken _cancellationToken;
        readonly EventHubSendTransportContext _context;
        readonly T _message;
        readonly IPipe<EventHubSendContext<T>> _pipe;
        readonly IPipe<SendContext<T>>? _sendPipe;

        public SendPipe(T message, EventHubSendTransportContext context, IPipe<EventHubSendContext<T>> pipe, CancellationToken cancellationToken,
            IPipe<SendContext<T>>? sendPipe = null)
        {
            _message = message;
            _context = context;
            _pipe = pipe;
            _cancellationToken = cancellationToken;
            _sendPipe = sendPipe;
        }

        public async Task SendAsync(ProducerContext context)
        {
            LogContext.SetCurrentIfNull(_context.LogContext);

            EventHubSendContext<T> sendContext = await _context.CreateContextAsync(_message, _pipe, _sendPipe, _cancellationToken).ConfigureAwait(false);

            sendContext.CancellationToken.ThrowIfCancellationRequested();
            if (_context is BaseSendTransportContext transportContext)
                transportContext.ApplyPayloadAdmission(sendContext);

            StartedActivity? activity = LogContext.Current?.StartSendActivity(_context, sendContext);
            var instrument = LogContext.Current?.StartSendInstrument(_context, sendContext);

            try
            {
                if (_context.SendObservers.Count > 0)
                    await _context.SendObservers.PreSendAsync(sendContext).ConfigureAwait(false);

                await _context.SendAsync(context, sendContext).ConfigureAwait(false);

                activity?.Update(sendContext);
                sendContext.LogSent();

                if (_context.SendObservers.Count > 0)
                    await _context.SendObservers.PostSendAsync(sendContext).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                sendContext.LogFaulted(exception);

                if (_context.SendObservers.Count > 0)
                    await _context.SendObservers.SendFaultAsync(sendContext, exception).ConfigureAwait(false);

                activity?.AddExceptionEvent(exception);
                instrument?.RecordException(exception);

                throw;
            }
            finally
            {
                activity?.Stop();
                instrument?.Complete();
            }
        }

        public void Probe(ProbeContext context)
        {
        }
    }


    class BatchSendPipe<T> :
        IPipe<ProducerContext>
        where T : class
    {
        readonly CancellationToken _cancellationToken;
        readonly EventHubSendTransportContext _context;
        readonly IPipe<SendContext<T>>[] _initializerPipes;
        readonly T[] _messages;
        readonly IPipe<EventHubSendContext<T>> _pipe;

        public BatchSendPipe(IEnumerable<T> messages, EventHubSendTransportContext context, IPipe<EventHubSendContext<T>> pipe,
            CancellationToken cancellationToken, IEnumerable<IPipe<SendContext<T>>>? sendPipes = null)
        {
            _messages = messages as T[] ?? messages.ToArray();
            _context = context;
            _pipe = pipe;
            _initializerPipes = sendPipes as IPipe<SendContext<T>>[] ?? sendPipes?.ToArray() ?? [];
            _cancellationToken = cancellationToken;
        }

        public async Task SendAsync(ProducerContext context)
        {
            if (_messages == null)
                throw new ArgumentNullException(nameof(_messages));

            LogContext.SetCurrentIfNull(_context.LogContext);

            var contexts = new EventHubSendContext<T>[_messages.Length];
            if (contexts.Length == 0)
                return;

            for (var i = 0; i < contexts.Length; i++)
            {
                contexts[i] = await _context.CreateContextAsync(_messages[i], _pipe,
                    _initializerPipes.Length > i ? _initializerPipes[i] : null, _cancellationToken).ConfigureAwait(false);
            }

            if (_context is BaseSendTransportContext transportContext)
            {
                foreach (EventHubSendContext<T> candidate in contexts)
                    transportContext.ApplyPayloadAdmission(candidate);
            }

            EventHubSendContext<T> sendContext = contexts[0];

            sendContext.CancellationToken.ThrowIfCancellationRequested();

            StartedActivity? activity = LogContext.Current?.StartSendActivity(_context, sendContext);
            try
            {
                if (_context.SendObservers.Count > 0)
                    await Task.WhenAll(contexts.Select(c => _context.SendObservers.PreSendAsync(c))).ConfigureAwait(false);

                await _context.SendAsync(context, contexts).ConfigureAwait(false);

                activity?.Update(sendContext);
                sendContext.LogSent();

                if (_context.SendObservers.Count > 0)
                    await Task.WhenAll(contexts.Select(c => _context.SendObservers.PostSendAsync(c))).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                sendContext.LogFaulted(exception);

                if (_context.SendObservers.Count > 0)
                    await Task.WhenAll(contexts.Select(c => _context.SendObservers.SendFaultAsync(c, exception))).ConfigureAwait(false);

                activity?.AddExceptionEvent(exception);

                throw;
            }
            finally
            {
                activity?.Stop();
            }
        }

        public void Probe(ProbeContext context)
        {
        }
    }
}
