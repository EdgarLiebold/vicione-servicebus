using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Logging.Diagnostics;
using ViciOne.ServiceBus.Logging.Monitoring;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Runs the common physical-send pipeline over a provider-specific transport context.</summary>
/// <typeparam name="TContext">The provider-specific transport context type.</typeparam>
public sealed class SendTransport<TContext> :
    Supervisor,
    ISendTransport,
    IAsyncDisposable
    where TContext : class, PipeContext
{
    readonly SendTransportContext<TContext> _context;

    /// <summary>Initializes the transport and supervises the agents owned by its context.</summary>
    /// <param name="context">The provider context that creates and dispatches send contexts.</param>
    public SendTransport(SendTransportContext<TContext> context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));

        IEnumerable<IAgent> agents = context.GetAgentHandles()
            ?? throw new ArgumentException("The send transport context returned no agent collection.", nameof(context));
        foreach (IAgent agent in agents)
        {
            if (agent is null)
                throw new ArgumentException("The send transport context returned an agent collection containing null.", nameof(context));

            Add(agent);
        }
    }

    /// <summary>Stops the transport and every agent owned by its provider context.</summary>
    /// <returns>A value task that completes after transport shutdown.</returns>
    public async ValueTask DisposeAsync()
    {
        await this.StopAsync("Disposed").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _context.ConnectSendObserver(observer);
    }

    /// <inheritdoc />
    public Task<SendContext<T>> CreateSendContextAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);
        cancellationToken.ThrowIfCancellationRequested();

        LogContext.SetCurrentIfNull(_context.LogContext);

        return _context.CreateSendContextAsync(message, pipe, cancellationToken)
            ?? throw new InvalidOperationException("The send transport context returned no context-creation task.");
    }

    /// <inheritdoc />
    public Task SendAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);
        cancellationToken.ThrowIfCancellationRequested();

        if (IsStopping || IsStopped)
            throw new TransportUnavailableException($"The send transport is stopping or stopped: {_context.EntityName}.");

        LogContext.SetCurrentIfNull(_context.LogContext);

        var sendPipe = new SendPipe<T>(_context, message, pipe, cancellationToken);

        return _context.SendAsync(sendPipe, cancellationToken)
            ?? throw new InvalidOperationException("The send transport context returned no send task.");
    }

    /// <summary>Logs transport shutdown before stopping provider agents.</summary>
    /// <param name="context">The stop reason and cancellation budget for supervised agents.</param>
    /// <returns>A task that completes after the supervisor has stopped its agents.</returns>
    protected override Task StopSupervisorAsync(StopSupervisorContext context)
    {
        TransportLogMessages.StoppingSendTransport(_context.EntityName);

        return base.StopSupervisorAsync(context);
    }


    sealed class SendPipe<T> :
        IPipe<TContext>
        where T : class
    {
        readonly CancellationToken _cancellationToken;
        readonly T _message;
        readonly IPipe<SendContext<T>> _pipe;
        readonly SendTransportContext<TContext> _sendTransportContext;

        public SendPipe(SendTransportContext<TContext> sendTransportContext, T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        {
            _sendTransportContext = sendTransportContext ?? throw new ArgumentNullException(nameof(sendTransportContext));
            _message = message ?? throw new ArgumentNullException(nameof(message));
            _pipe = pipe ?? throw new ArgumentNullException(nameof(pipe));
            _cancellationToken = cancellationToken;
        }

        public async Task SendAsync(TContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            SendContext<T> sendContext = await CreateSendContextAsync(context).ConfigureAwait(false);

            ForwardingExpiration.MarkIfExpired(sendContext, null, sendContext.GetTimeProvider());
            if (ForwardingExpiration.TryDiscard(sendContext))
                return;

            await SendWithDiagnosticsAsync(context, sendContext).ConfigureAwait(false);
        }

        async Task<SendContext<T>> CreateSendContextAsync(TContext context)
        {
            Task<SendContext<T>> createContextTask = _sendTransportContext.CreateSendContextAsync(
                    context,
                    _message,
                    _pipe,
                    _cancellationToken)
                ?? throw new InvalidOperationException("The send transport context returned no context-creation task.");
            return await createContextTask.ConfigureAwait(false)
                ?? throw new InvalidOperationException("The send transport context returned no send context.");
        }

        async Task SendWithDiagnosticsAsync(TContext context, SendContext<T> sendContext)
        {
            StartedActivity? activity = MessageActivity.TryStartSend(_sendTransportContext, sendContext);
            MetricOperation? instrument = LogContext.Current?.TryStartSendMetrics(_sendTransportContext, sendContext);
            try
            {
                await DispatchAsync(context, sendContext, activity).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                await HandleSendFailureAsync(sendContext, exception, activity, instrument).ConfigureAwait(false);

                throw;
            }
            finally
            {
                activity?.Stop();
                instrument?.Complete();
            }
        }

        async Task DispatchAsync(TContext context, SendContext<T> sendContext, StartedActivity? activity)
        {
            if (_sendTransportContext is BaseSendTransportContext transportContext)
                transportContext.ApplyPayloadAdmission(sendContext);

            if (_sendTransportContext.SendObservers.Count > 0)
                await _sendTransportContext.SendObservers.PreSendAsync(sendContext).ConfigureAwait(false);

            if (_sendTransportContext is BaseSendTransportContext admittedTransportContext)
                admittedTransportContext.ApplyPayloadAdmission(sendContext);

            Task sendTask = _sendTransportContext.SendAsync(context, sendContext)
                ?? throw new InvalidOperationException("The send transport context returned no send task.");
            await sendTask.ConfigureAwait(false);

            activity?.Update(sendContext);
            sendContext.LogSent();

            if (_sendTransportContext.SendObservers.Count > 0)
                await _sendTransportContext.SendObservers.PostSendAsync(sendContext).ConfigureAwait(false);
        }

        async Task HandleSendFailureAsync(
            SendContext<T> sendContext,
            Exception sendFailure,
            StartedActivity? activity,
            MetricOperation? instrument)
        {
            sendContext.LogFaulted(sendFailure);

            if (_sendTransportContext.SendObservers.Count > 0)
            {
                try
                {
                    await _sendTransportContext.SendObservers.SendFaultAsync(sendContext, sendFailure).ConfigureAwait(false);
                }
                catch (Exception observerFailure)
                {
                    LogContext.Error?.Log(observerFailure,
                        "A send-fault observer failed after the send operation faulted: {DestinationAddress}",
                        sendContext.DestinationAddress);
                }
            }

            activity?.AddExceptionEvent(sendFailure);
            instrument?.RecordException(sendFailure);
        }

        public void Probe(ProbeContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
        }
    }
}
