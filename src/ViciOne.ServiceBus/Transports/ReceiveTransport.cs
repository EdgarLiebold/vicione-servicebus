using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Transports receive messages.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public class ReceiveTransport<TContext> :
    IReceiveTransport
    where TContext : class, PipeContext
{
    readonly ReceiveEndpointContext _context;
    readonly IHostConfiguration _hostConfiguration;
    readonly Func<ITransportSupervisor<TContext>> _supervisorFactory;
    readonly IPipe<TContext> _transportPipe;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="hostConfiguration">The host configuration.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="supervisorFactory">The supervisor factory.</param>
    /// <param name="transportPipe">The transport pipe.</param>
    public ReceiveTransport(IHostConfiguration hostConfiguration, ReceiveEndpointContext context, Func<ITransportSupervisor<TContext>> supervisorFactory,
        IPipe<TContext> transportPipe)
    {
        _hostConfiguration = hostConfiguration;
        _context = context;
        _supervisorFactory = supervisorFactory;
        _transportPipe = transportPipe;
    }

    /// <summary>Gets or sets the pre start pipe.</summary>
    public IPipe<TContext> PreStartPipe { get; set; } = null!;
    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateScope("receiveTransport");

        _context.Probe(scope);
    }

    /// <summary>Starts the receive transport.</summary>
    /// <returns>A handle that exposes transport readiness and controls its lifetime.</returns>
    public ReceiveTransportHandle Start()
    {
        return new ReceiveTransportAgent(_hostConfiguration.ReceiveTransportRetryPolicy, _context, _supervisorFactory, _transportPipe, PreStartPipe);
    }

    /// <summary>Connects receive observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectReceiveObserver(IReceiveObserver observer)
    {
        return _context.ConnectReceiveObserver(observer);
    }

    /// <summary>Connects receive transport observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectReceiveTransportObserver(IReceiveTransportObserver observer)
    {
        return _context.ConnectReceiveTransportObserver(observer);
    }

    /// <summary>Connects publish observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
    {
        return _context.ConnectPublishObserver(observer);
    }

    /// <summary>Connects send observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        return _context.ConnectSendObserver(observer);
    }


    class ReceiveTransportAgent :
        Agent,
        ReceiveTransportHandle
    {
        readonly ReceiveEndpointContext _context;
        readonly IPipe<TContext> _preStartPipe;
        readonly IRetryPolicy _retryPolicy;
        readonly Func<ITransportSupervisor<TContext>> _supervisorFactory;
        readonly IPipe<TContext> _transportPipe;
        ITransportSupervisor<TContext> _supervisor = null!;

        public ReceiveTransportAgent(IRetryPolicy retryPolicy, ReceiveEndpointContext context, Func<ITransportSupervisor<TContext>> supervisorFactory,
            IPipe<TContext> transportPipe, IPipe<TContext> preStartPipe)
        {
            _retryPolicy = retryPolicy;
            _context = context;
            _supervisorFactory = supervisorFactory;
            _transportPipe = transportPipe;
            _preStartPipe = preStartPipe;

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

            if (_supervisor != null)
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
                    if (retryContext != null)
                    {
                        retryContext.CancellationToken.ThrowIfCancellationRequested();

                        LogContext.Info?.Log(retryContext.Exception, "Retrying {Delay}: {Message}", retryContext.Delay,
                            retryContext.Exception.Message);

                        if (retryContext.Delay.HasValue)
                        {
                            await Task.Delay(retryContext.Delay.Value, _context.GetTimeProvider(), retryContext.CancellationToken)
                                .ConfigureAwait(false);
                        }

                        Task preRetry = retryContext.PreRetryAsync()
                            ?? throw new InvalidOperationException("The receive transport retry context returned a null pre-retry task.");
                        if (preRetry.Status != TaskStatus.RanToCompletion)
                            await preRetry.ConfigureAwait(false);
                    }

                    Stopping.ThrowIfCancellationRequested();
                    await RunTransportAsync().ConfigureAwait(false);

                    if (!Stopping.IsCancellationRequested)
                    {
                        var exception = new ConnectionException(
                            $"Receive transport completed before shutdown: {_context.InputAddress}", isTransient: true);
                        await NotifyFaultedAsync(exception, false).ConfigureAwait(false);
                        throw exception;
                    }
                }
                catch (OperationCanceledException) when (Stopping.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    bool canRetry = retryContext == null
                        ? policyContext.CanRetry(exception, out RetryContext<TransportStoppingContext> nextRetryContext)
                        : retryContext.CanRetry(exception, out nextRetryContext);

                    if (!canRetry || nextRetryContext == null)
                    {
                        if (nextRetryContext != null && _retryPolicy.IsHandled(exception))
                        {
                            Task retryFaulted = nextRetryContext.RetryFaultedAsync(exception)
                                ?? throw new InvalidOperationException("The receive transport retry context returned a null retry-faulted task.");
                            if (retryFaulted.Status != TaskStatus.RanToCompletion)
                                await retryFaulted.ConfigureAwait(false);
                        }

                        LogContext.Error?.Log(exception, "ReceiveTransport retry budget exhausted: {InputAddress}", _context.InputAddress);
                        await NotifyFaultedAsync(exception, true).ConfigureAwait(false);
                        break;
                    }

                    retryContext = nextRetryContext;
                }
            }
        }

        async Task RunTransportAsync()
        {
            try
            {
                _supervisor = _supervisorFactory();

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

            var exception = _context.ConvertException(originalException, message);


            await NotifyFaultedAsync(exception, false).ConfigureAwait(false);

            return exception;
        }




        Task NotifyFaultedAsync(Exception exception, bool isTerminal)
        {
            return _context.TransportObservers.NotifyFaultedAsync(_context.InputAddress, exception, isTerminal);
        }


        class TransportStoppingContext :
            BasePipeContext
        {
            public TransportStoppingContext(CancellationToken cancellationToken)
                : base(cancellationToken)
            {
            }
        }
    }
}
