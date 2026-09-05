using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Provides a send transport implementation.
/// </summary>
/// <typeparam name="TContext">The t context type.</typeparam>
public class SendTransport<TContext> :
    Supervisor,
    ISendTransport,
    IAsyncDisposable
    where TContext : class, PipeContext
{
    readonly SendTransportContext<TContext> _context;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public SendTransport(SendTransportContext<TContext> context)
    {
        _context = context;

        foreach (var agent in context.GetAgentHandles())
            Add(agent);
    }

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public async ValueTask DisposeAsync()
    {
        await this.StopAsync("Disposed").ConfigureAwait(false);
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

    /// <summary>
    /// Creates send context.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<SendContext<T>> CreateSendContextAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        LogContext.SetCurrentIfNull(_context.LogContext);

        return _context.CreateSendContextAsync(message, pipe, cancellationToken);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        if (IsStopped)
            throw new TransportUnavailableException($"The send transport is stopped: {_context.EntityName}");

        LogContext.SetCurrentIfNull(_context.LogContext);

        var sendPipe = new SendPipe<T>(_context, message, pipe, cancellationToken);

        return _context.SendAsync(sendPipe, cancellationToken);
    }

    /// <summary>
    /// Stops supervisor.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    protected override Task StopSupervisorAsync(StopSupervisorContext context)
    {
        TransportLogMessages.StoppingSendTransport(_context.EntityName);

        return base.StopSupervisorAsync(context);
    }


    class SendPipe<T> :
        IPipe<TContext>
        where T : class
    {
        readonly CancellationToken _cancellationToken;
        readonly T _message;
        readonly IPipe<SendContext<T>> _pipe;
        readonly SendTransportContext<TContext> _sendTransportContext;

        public SendPipe(SendTransportContext<TContext> sendTransportContext, T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        {
            _sendTransportContext = sendTransportContext;
            _message = message;
            _pipe = pipe;
            _cancellationToken = cancellationToken;
        }

        public async Task SendAsync(TContext context)
        {
            SendContext<T> sendContext = await _sendTransportContext.CreateSendContextAsync(context, _message, _pipe, _cancellationToken).ConfigureAwait(false);

            ForwardingExpiration.MarkIfExpired(sendContext, null, sendContext.GetTimeProvider());
            if (ForwardingExpiration.TryDiscard(sendContext))
                return;

            StartedActivity? activity = LogContext.Current?.StartSendActivity(_sendTransportContext, sendContext);
            var instrument = LogContext.Current?.StartSendInstrument(_sendTransportContext, sendContext);
            try
            {
                if (_sendTransportContext is BaseSendTransportContext transportContext)
                    transportContext.ApplyPayloadAdmission(sendContext);

                if (_sendTransportContext.SendObservers.Count > 0)
                    await _sendTransportContext.SendObservers.PreSendAsync(sendContext).ConfigureAwait(false);

                await _sendTransportContext.SendAsync(context, sendContext).ConfigureAwait(false);

                activity?.Update(sendContext);
                sendContext.LogSent();

                if (_sendTransportContext.SendObservers.Count > 0)
                    await _sendTransportContext.SendObservers.PostSendAsync(sendContext).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                sendContext.LogFaulted(ex);

                if (_sendTransportContext.SendObservers.Count > 0)
                    await _sendTransportContext.SendObservers.SendFaultAsync(sendContext, ex).ConfigureAwait(false);

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

        public void Probe(ProbeContext context)
        {
        }
    }
}
