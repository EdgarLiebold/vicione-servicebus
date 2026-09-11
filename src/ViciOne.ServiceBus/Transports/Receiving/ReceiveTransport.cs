using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Runs a supervised transport receive pipeline under the host retry policy.</summary>
/// <typeparam name="TContext">The transport connection context supplied to the pipeline.</typeparam>
public sealed class ReceiveTransport<TContext> :
    IReceiveTransport
    where TContext : class, PipeContext
{
    readonly ReceiveEndpointContext _context;
    readonly IHostConfiguration _hostConfiguration;
    IPipe<TContext> _preStartPipe = Pipe.Empty<TContext>();
    readonly Func<ITransportSupervisor<TContext>> _supervisorFactory;
    readonly IPipe<TContext> _transportPipe;

    /// <summary>Initializes a receive transport for one endpoint and connection context.</summary>
    /// <param name="hostConfiguration">The host configuration that supplies transport retry policy.</param>
    /// <param name="context">The endpoint context that owns observers and receive resources.</param>
    /// <param name="supervisorFactory">The factory that supplies a supervisor for each transport attempt.</param>
    /// <param name="transportPipe">The pipeline that receives messages from a connected transport.</param>
    public ReceiveTransport(IHostConfiguration hostConfiguration, ReceiveEndpointContext context, Func<ITransportSupervisor<TContext>> supervisorFactory,
        IPipe<TContext> transportPipe)
    {
        _hostConfiguration = hostConfiguration ?? throw new ArgumentNullException(nameof(hostConfiguration));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _supervisorFactory = supervisorFactory ?? throw new ArgumentNullException(nameof(supervisorFactory));
        _transportPipe = transportPipe ?? throw new ArgumentNullException(nameof(transportPipe));
    }

    /// <summary>Gets or sets the pipeline executed on a connected transport before message reception starts.</summary>
    public IPipe<TContext> PreStartPipe
    {
        get => _preStartPipe;
        set => _preStartPipe = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Adds receive transport and endpoint diagnostics to a probe.</summary>
    /// <param name="context">The probe context that receives the diagnostics.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var scope = context.CreateScope("receiveTransport");

        _context.Probe(scope);
    }

    /// <summary>Starts the receive transport.</summary>
    /// <returns>A handle that exposes transport readiness and controls its lifetime.</returns>
    public ReceiveTransportHandle Start()
    {
        IRetryPolicy retryPolicy = _hostConfiguration.ReceiveTransportRetryPolicy
            ?? throw new InvalidOperationException("The host configuration returned no receive transport retry policy.");
        return new ReceiveTransportAgent(retryPolicy, _context, _supervisorFactory, _transportPipe, PreStartPipe);
    }

    /// <summary>Subscribes an observer to receive notifications.</summary>
    /// <param name="observer">The observer that receives the notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectReceiveObserver(IReceiveObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _context.ConnectReceiveObserver(observer);
    }

    /// <summary>Subscribes an observer to transport lifecycle notifications.</summary>
    /// <param name="observer">The observer that receives transport lifecycle notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectReceiveTransportObserver(IReceiveTransportObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _context.ConnectReceiveTransportObserver(observer);
    }

    /// <summary>Subscribes an observer to messages published by the receive endpoint.</summary>
    /// <param name="observer">The observer that receives publish notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _context.ConnectPublishObserver(observer);
    }

    /// <summary>Subscribes an observer to messages sent by the receive endpoint.</summary>
    /// <param name="observer">The observer that receives send notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _context.ConnectSendObserver(observer);
    }


    sealed class ReceiveTransportAgent :
        Agent,
        ReceiveTransportHandle
    {
        readonly ReceiveEndpointContext _context;
        readonly IPipe<TContext> _preStartPipe;
        readonly IRetryPolicy _retryPolicy;
        readonly Func<ITransportSupervisor<TContext>> _supervisorFactory;
        readonly IPipe<TContext> _transportPipe;
        ITransportSupervisor<TContext>? _supervisor;

        public ReceiveTransportAgent(IRetryPolicy retryPolicy, ReceiveEndpointContext context, Func<ITransportSupervisor<TContext>> supervisorFactory,
            IPipe<TContext> transportPipe, IPipe<TContext> preStartPipe)
        {
            _retryPolicy = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _supervisorFactory = supervisorFactory ?? throw new ArgumentNullException(nameof(supervisorFactory));
            _transportPipe = transportPipe ?? throw new ArgumentNullException(nameof(transportPipe));
            _preStartPipe = preStartPipe ?? throw new ArgumentNullException(nameof(preStartPipe));

            Task receiver = RunAsync();
            SetCompleted(receiver);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return this.StopAsync("Stop Receive Transport", cancellationToken);
        }

        protected override async Task StopAgentAsync(StopContext context)
        {
            LogContext.SetCurrentIfNull(_context.LogContext);

            if (_supervisor is not null)
                await _supervisor.StopAsync(context).ConfigureAwait(false);

            await Completed.ConfigureAwait(false);
        }

        async Task RunAsync()
        {
            var stoppingContext = new TransportStoppingContext(Stopping);
            stoppingContext.SetTimeProvider(_context.GetTimeProvider());

            using RetryPolicyContext<TransportStoppingContext> policyContext = _retryPolicy.CreatePolicyContext(stoppingContext)
                ?? throw new InvalidOperationException("The receive transport retry policy returned a null policy context.");

            RetryContext<TransportStoppingContext>? retryContext = null;

            while (!Stopping.IsCancellationRequested)
            {
                try
                {
                    await PrepareRetryAsync(retryContext).ConfigureAwait(false);

                    Stopping.ThrowIfCancellationRequested();
                    await RunTransportAsync().ConfigureAwait(false);
                    await ReportUnexpectedCompletionAsync().ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (Stopping.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception exception)
                {
                    retryContext = await GetNextRetryContextAsync(policyContext, retryContext, exception).ConfigureAwait(false);
                    if (retryContext is null)
                        return;
                }
            }
        }

        async Task PrepareRetryAsync(RetryContext<TransportStoppingContext>? retryContext)
        {
            if (retryContext is null)
                return;

            retryContext.CancellationToken.ThrowIfCancellationRequested();
            LogContext.Info?.Log(retryContext.Exception, "Retrying {Delay}: {Message}", retryContext.Delay,
                retryContext.Exception.Message);

            if (retryContext.Delay.HasValue)
            {
                await Task.Delay(retryContext.Delay.Value, _context.GetTimeProvider(), retryContext.CancellationToken)
                    .ConfigureAwait(false);
            }

            Task preRetryTask = retryContext.PreRetryAsync()
                ?? throw new InvalidOperationException("The receive transport retry context returned a null pre-retry task.");
            if (preRetryTask.Status != TaskStatus.RanToCompletion)
                await preRetryTask.ConfigureAwait(false);
        }

        async Task ReportUnexpectedCompletionAsync()
        {
            if (Stopping.IsCancellationRequested)
                return;

            var exception = new ConnectionException(
                $"Receive transport completed before shutdown: {_context.InputAddress}",
                isTransient: true);
            await NotifyFaultedAsync(exception, false).ConfigureAwait(false);
            throw exception;
        }

        async Task<RetryContext<TransportStoppingContext>?> GetNextRetryContextAsync(
            RetryPolicyContext<TransportStoppingContext> policyContext,
            RetryContext<TransportStoppingContext>? retryContext,
            Exception exception)
        {
            bool canRetry = retryContext is null
                ? policyContext.CanRetry(exception, out RetryContext<TransportStoppingContext> nextRetryContext)
                : retryContext.CanRetry(exception, out nextRetryContext);
            if (canRetry && nextRetryContext is not null)
                return nextRetryContext;

            if (nextRetryContext is not null && _retryPolicy.IsHandled(exception))
            {
                Task retryFaultedTask = nextRetryContext.RetryFaultedAsync(exception)
                    ?? throw new InvalidOperationException("The receive transport retry context returned a null retry-faulted task.");
                if (retryFaultedTask.Status != TaskStatus.RanToCompletion)
                    await retryFaultedTask.ConfigureAwait(false);
            }

            LogContext.Error?.Log(exception, "ReceiveTransport retry budget exhausted: {InputAddress}", _context.InputAddress);
            await NotifyFaultedAsync(exception, true).ConfigureAwait(false);
            return null;
        }

        async Task RunTransportAsync()
        {
            try
            {
                _supervisor = _supervisorFactory()
                    ?? throw new InvalidOperationException("The receive transport supervisor factory returned no supervisor.");

                if (_preStartPipe.IsNotEmpty())
                    await _supervisor.SendAsync(_preStartPipe, Stopping).ConfigureAwait(false);

                // An unconnected receive pipe can report transport availability before the supervisor loop.
                if (!_context.ReceivePipe.Connected.IsCompleted)
                    await _context.OnTransportStartupAsync(_supervisor, Stopping).ConfigureAwait(false);

                if (!IsStopping)
                    await _supervisor.SendAsync(_transportPipe, Stopped).ConfigureAwait(false);
            }
            catch (ConnectionException exception)
            {
                await NotifyFaultedAsync(exception, false).ConfigureAwait(false);
                throw;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw await NotifyFaultedAsync(exception, "ReceiveTransport faulted: ").ConfigureAwait(false);
            }
        }

        async Task<Exception> NotifyFaultedAsync(Exception originalException, string message)
        {
            ArgumentNullException.ThrowIfNull(originalException);
            ArgumentNullException.ThrowIfNull(message);

            var exception = _context.ConvertException(originalException, message)
                ?? throw new InvalidOperationException("The receive endpoint context returned no converted exception.");

            await NotifyFaultedAsync(exception, false).ConfigureAwait(false);

            return exception;
        }
        Task NotifyFaultedAsync(Exception exception, bool isTerminal)
        {
            ArgumentNullException.ThrowIfNull(exception);
            return _context.TransportObservers.NotifyFaultedAsync(_context.InputAddress, exception, isTerminal)
                ?? throw new InvalidOperationException("The receive transport observer returned no fault-notification task.");
        }


        sealed class TransportStoppingContext :
            BasePipeContext
        {
            public TransportStoppingContext(CancellationToken cancellationToken)
                : base(cancellationToken)
            {
            }
        }
    }
}
