using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Logging.Diagnostics;
using ViciOne.ServiceBus.Logging.Monitoring;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Owns a supervised Event Hubs send transport and applies initialization, admission, observers, and diagnostics around production.</summary>
public class EventHubProducer :
    Supervisor,
    IAsyncDisposable,
    IEventHubProducer
{
    readonly ConnectHandle? _connectHandle;
    readonly EventHubSendTransportContext _context;

    /// <summary>Creates a producer and adopts the transport's agent lifetimes.</summary>
    /// <param name="context">The Event Hubs send transport context.</param>
    /// <param name="connectHandle">The optional observer connection owned by this producer.</param>
    public EventHubProducer(EventHubSendTransportContext context, ConnectHandle? connectHandle = null)
    {
        _context = context;
        _connectHandle = connectHandle;

        foreach (var handle in _context.GetAgentHandles())
            Add(handle);
    }

    /// <summary>Disconnects the owned observer registration and stops supervised producer agents.</summary>
    /// <returns>A task that completes after producer shutdown.</returns>
    public async ValueTask DisposeAsync()
    {
        _connectHandle?.Disconnect();
        await this.StopAsync("Disposing Agent").ConfigureAwait(false);
    }

    /// <summary>Produces one message using the default Event Hubs send-context pipe.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="message">The message to produce.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The supervised transport task for the serialized message.</returns>
    public Task ProduceAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class
    {
        return ProduceAsync(message, Pipe.Empty<EventHubSendContext<T>>(), cancellationToken);
    }

    /// <summary>Produces a batch of messages using the default Event Hubs send-context pipe.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="messages">The messages to produce.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when all provider batches have been sent.</returns>
    public Task ProduceAsync<T>(IEnumerable<T> messages, CancellationToken cancellationToken = default)
        where T : class
    {
        return ProduceAsync(messages, Pipe.Empty<EventHubSendContext<T>>(), cancellationToken);
    }

    /// <summary>Configures, observes, and produces one message through the supervised transport.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="message">The message to produce.</param>
    /// <param name="pipe">The pipe that configures the Event Hubs send context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes after send observers and provider submission finish.</returns>
    public Task ProduceAsync<T>(T message, IPipe<EventHubSendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        return _context.SendAsync(new SendPipe<T>(message, _context, pipe, cancellationToken), cancellationToken);
    }

    /// <summary>Configures, observes, and produces messages in provider-sized batches.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="messages">The messages to produce.</param>
    /// <param name="pipe">The pipe applied to each Event Hubs send context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes after observers and all provider batches finish.</returns>
    public Task ProduceAsync<T>(IEnumerable<T> messages, IPipe<EventHubSendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        return _context.SendAsync(new BatchSendPipe<T>(messages, _context, pipe, cancellationToken), cancellationToken);
    }

    /// <summary>Initializes and produces one message using the default Event Hubs send-context pipe.</summary>
    /// <typeparam name="T">The message type to initialize.</typeparam>
    /// <param name="values">The values used to initialize the message.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes after initialization and provider submission.</returns>
    public Task ProduceAsync<T>(object values, CancellationToken cancellationToken = default)
        where T : class
    {
        return ProduceAsync(values, Pipe.Empty<EventHubSendContext<T>>(), cancellationToken);
    }

    /// <summary>Initializes and produces a batch of messages using the default Event Hubs send-context pipe.</summary>
    /// <typeparam name="T">The message type to initialize.</typeparam>
    /// <param name="values">The values used to initialize the messages.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes after initialization and all provider batches finish.</returns>
    public Task ProduceAsync<T>(IEnumerable<object> values, CancellationToken cancellationToken = default)
        where T : class
    {
        return ProduceAsync(values, Pipe.Empty<EventHubSendContext<T>>(), cancellationToken);
    }

    /// <summary>Initializes, configures, observes, and produces one message.</summary>
    /// <typeparam name="T">The message type to initialize.</typeparam>
    /// <param name="values">The values used to initialize the message.</param>
    /// <param name="pipe">The pipe that configures the Event Hubs send context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes after initialization, observers, and provider submission.</returns>
    public async Task ProduceAsync<T>(object values, IPipe<EventHubSendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        (var message, IPipe<SendContext<T>> sendPipe) = await MessageInitializerCache<T>.InitializeMessageAsync(values, cancellationToken);

        await _context.SendAsync(new SendPipe<T>(message, _context, pipe, cancellationToken, sendPipe), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Initializes, configures, observes, and produces messages in provider-sized batches.</summary>
    /// <typeparam name="T">The message type to initialize.</typeparam>
    /// <param name="values">The values used to initialize the messages.</param>
    /// <param name="pipe">The pipe applied to each Event Hubs send context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes after initialization, observers, and all provider batches finish.</returns>
    public async Task ProduceAsync<T>(IEnumerable<object> values, IPipe<EventHubSendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>[] contexts = await Task.WhenAll(values.Select(value => MessageInitializerCache<T>.InitializeMessageAsync(value, cancellationToken)))
            .ConfigureAwait(false);

        await _context.SendAsync(new BatchSendPipe<T>(contexts.Select(x => x.Message), _context, pipe, cancellationToken, contexts.Select(x => x.Pipe)),
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Connects a send observer to the underlying transport context.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
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
            BaseSendTransportContext? transportContext = _context as BaseSendTransportContext;
            if (transportContext is not null)
                transportContext.ApplyPayloadAdmission(sendContext);

            StartedActivity? activity = MessageActivity.TryStartSend(_context, sendContext);
            var instrument = LogContext.Current?.TryStartSendMetrics(_context, sendContext);

            try
            {
                if (_context.SendObservers.Count > 0)
                    await _context.SendObservers.PreSendAsync(sendContext).ConfigureAwait(false);

                if (transportContext is not null)
                    transportContext.ApplyPayloadAdmission(sendContext);
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
            LogContext.SetCurrentIfNull(_context.LogContext);

            var contexts = new EventHubSendContext<T>[_messages.Length];
            if (contexts.Length == 0)
                return;

            for (var i = 0; i < contexts.Length; i++)
            {
                contexts[i] = await _context.CreateContextAsync(_messages[i], _pipe,
                    _initializerPipes.Length > i ? _initializerPipes[i] : null, _cancellationToken).ConfigureAwait(false);
            }

            BaseSendTransportContext? transportContext = _context as BaseSendTransportContext;
            if (transportContext is not null)
            {
                foreach (EventHubSendContext<T> candidate in contexts)
                    transportContext.ApplyPayloadAdmission(candidate);
            }

            EventHubSendContext<T> sendContext = contexts[0];

            sendContext.CancellationToken.ThrowIfCancellationRequested();

            StartedActivity? activity = MessageActivity.TryStartSend(_context, sendContext);
            try
            {
                if (_context.SendObservers.Count > 0)
                    await Task.WhenAll(contexts.Select(c => _context.SendObservers.PreSendAsync(c))).ConfigureAwait(false);

                if (transportContext is not null)
                {
                    foreach (EventHubSendContext<T> candidate in contexts)
                        transportContext.ApplyPayloadAdmission(candidate);
                }

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
