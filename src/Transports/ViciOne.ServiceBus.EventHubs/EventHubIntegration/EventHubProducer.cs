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
                try
                {
                    if (_context.SendObservers.Count > 0)
                        await _context.SendObservers.PreSendAsync(sendContext).ConfigureAwait(false);

                    if (transportContext is not null)
                        transportContext.ApplyPayloadAdmission(sendContext);
                    await _context.SendAsync(context, sendContext).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    TryLogFault(sendContext, exception);

                    if (_context.SendObservers.Count > 0)
                    {
                        try
                        {
                            await _context.SendObservers.SendFaultAsync(sendContext, exception).ConfigureAwait(false);
                        }
                        catch (Exception observerFailure)
                        {
                            TryLogSecondaryFailure(observerFailure, sendContext.DestinationAddress);
                        }
                    }

                    activity?.AddExceptionEvent(exception);
                    instrument?.RecordException(exception);
                    throw;
                }

                TryLogSent(sendContext, activity);

                if (_context.SendObservers.Count > 0)
                {
                    try
                    {
                        await _context.SendObservers.PostSendAsync(sendContext).ConfigureAwait(false);
                    }
                    catch (Exception observerFailure)
                    {
                        TryLogSecondaryFailure(observerFailure, sendContext.DestinationAddress);
                    }
                }
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
        readonly bool[] _confirmedMessages;
        readonly IPipe<SendContext<T>>[] _initializerPipes;
        readonly T[] _messages;
        readonly IPipe<EventHubSendContext<T>> _pipe;

        public BatchSendPipe(IEnumerable<T> messages, EventHubSendTransportContext context, IPipe<EventHubSendContext<T>> pipe,
            CancellationToken cancellationToken, IEnumerable<IPipe<SendContext<T>>>? sendPipes = null)
        {
            _messages = messages as T[] ?? messages.ToArray();
            _confirmedMessages = new bool[_messages.Length];
            _context = context;
            _pipe = pipe;
            _initializerPipes = sendPipes as IPipe<SendContext<T>>[] ?? sendPipes?.ToArray() ?? [];
            _cancellationToken = cancellationToken;
        }

        public async Task SendAsync(ProducerContext context)
        {
            LogContext.SetCurrentIfNull(_context.LogContext);

            int[] pendingIndices = Enumerable.Range(0, _messages.Length)
                .Where(index => !_confirmedMessages[index])
                .ToArray();
            if (pendingIndices.Length == 0)
                return;

            EventHubSendContext<T>[] contexts = await CreatePendingContextsAsync(pendingIndices).ConfigureAwait(false);
            ApplyPayloadAdmission(contexts);
            EventHubSendContext<T> sendContext = contexts[0];
            sendContext.CancellationToken.ThrowIfCancellationRequested();
            StartedActivity? activity = MessageActivity.TryStartSend(_context, sendContext);
            try
            {
                try
                {
                    await SendPendingAsync(context, contexts).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    if (await HandlePartialFailureAsync(exception, contexts, pendingIndices, activity).ConfigureAwait(false))
                        return;
                    throw;
                }

                MarkConfirmed(pendingIndices);
                TryLogSent(sendContext, activity);
                await ObservePostAsync(contexts).ConfigureAwait(false);
            }
            finally
            {
                activity?.Stop();
            }
        }

        async Task<EventHubSendContext<T>[]> CreatePendingContextsAsync(int[] pendingIndices)
        {
            var contexts = new EventHubSendContext<T>[pendingIndices.Length];
            for (var index = 0; index < pendingIndices.Length; index++)
            {
                int originalIndex = pendingIndices[index];
                contexts[index] = await _context.CreateContextAsync(_messages[originalIndex], _pipe,
                    _initializerPipes.Length > originalIndex ? _initializerPipes[originalIndex] : null,
                    _cancellationToken).ConfigureAwait(false);
            }

            return contexts;
        }

        void ApplyPayloadAdmission(EventHubSendContext<T>[] contexts)
        {
            if (_context is not BaseSendTransportContext transportContext)
                return;

            foreach (EventHubSendContext<T> candidate in contexts)
                transportContext.ApplyPayloadAdmission(candidate);
        }

        async Task SendPendingAsync(ProducerContext producerContext, EventHubSendContext<T>[] contexts)
        {
            if (_context.SendObservers.Count > 0)
                await Task.WhenAll(contexts.Select(candidate => _context.SendObservers.PreSendAsync(candidate))).ConfigureAwait(false);

            ApplyPayloadAdmission(contexts);
            await _context.SendAsync(producerContext, contexts).ConfigureAwait(false);
        }

        async Task<bool> HandlePartialFailureAsync(Exception exception, EventHubSendContext<T>[] contexts,
            int[] pendingIndices, StartedActivity? activity)
        {
            var confirmed = new List<EventHubSendContext<T>>();
            var unresolved = new List<EventHubSendContext<T>>();
            for (var index = 0; index < contexts.Length; index++)
            {
                if (contexts[index] is EventHubMessageSendContext<T> { IsProviderConfirmed: true })
                {
                    _confirmedMessages[pendingIndices[index]] = true;
                    confirmed.Add(contexts[index]);
                }
                else
                    unresolved.Add(contexts[index]);
            }

            if (confirmed.Count > 0)
            {
                TryLogSent(confirmed[0], activity);
                await ObservePostAsync(confirmed).ConfigureAwait(false);
            }

            if (unresolved.Count == 0)
            {
                TryLogSecondaryFailure(exception, contexts[0].DestinationAddress);
                return true;
            }

            TryLogFault(unresolved[0], exception);
            await ObserveFaultAsync(unresolved, exception).ConfigureAwait(false);
            if (confirmed.Count == 0)
                activity?.AddExceptionEvent(exception);
            return false;
        }

        void MarkConfirmed(int[] pendingIndices)
        {
            foreach (int index in pendingIndices)
                _confirmedMessages[index] = true;
        }

        async Task ObservePostAsync(IReadOnlyList<EventHubSendContext<T>> contexts)
        {
            if (_context.SendObservers.Count == 0)
                return;

            try
            {
                await Task.WhenAll(contexts.Select(context => _context.SendObservers.PostSendAsync(context))).ConfigureAwait(false);
            }
            catch (Exception observerFailure)
            {
                TryLogSecondaryFailure(observerFailure, contexts[0].DestinationAddress);
            }
        }

        async Task ObserveFaultAsync(IReadOnlyList<EventHubSendContext<T>> contexts, Exception sendFailure)
        {
            if (_context.SendObservers.Count == 0)
                return;

            try
            {
                await Task.WhenAll(contexts.Select(context => _context.SendObservers.SendFaultAsync(context, sendFailure))).ConfigureAwait(false);
            }
            catch (Exception observerFailure)
            {
                TryLogSecondaryFailure(observerFailure, contexts[0].DestinationAddress);
            }
        }

        public void Probe(ProbeContext context)
        {
        }
    }

    static void TryLogSent<T>(EventHubSendContext<T> sendContext, StartedActivity? activity) where T : class
    {
        try
        {
            activity?.Update(sendContext);
            sendContext.LogSent();
        }
        catch (Exception diagnosticFailure)
        {
            TryLogSecondaryFailure(diagnosticFailure, sendContext.DestinationAddress);
        }
    }

    static void TryLogFault<T>(EventHubSendContext<T> sendContext, Exception sendFailure) where T : class
    {
        try
        {
            sendContext.LogFaulted(sendFailure);
        }
        catch (Exception diagnosticFailure)
        {
            TryLogSecondaryFailure(diagnosticFailure, sendContext.DestinationAddress);
        }
    }

    static void TryLogSecondaryFailure(Exception failure, Uri? destinationAddress)
    {
        try
        {
            LogContext.Error?.Log(failure,
                "A send diagnostic or observer failed: {DestinationAddress}", destinationAddress);
        }
        catch (Exception)
        {
            // Logging must not change the provider-confirmed send outcome.
        }
    }
}
