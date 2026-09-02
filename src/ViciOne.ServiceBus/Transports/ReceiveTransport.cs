namespace ViciOne.ServiceBus.Transports
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Configuration;
    using Middleware;


    public class ReceiveTransport<TContext> :
        IReceiveTransport
        where TContext : class, PipeContext
    {
        readonly ReceiveEndpointContext _context;
        readonly IHostConfiguration _hostConfiguration;
        readonly Func<ITransportSupervisor<TContext>> _supervisorFactory;
        readonly IPipe<TContext> _transportPipe;

        public ReceiveTransport(IHostConfiguration hostConfiguration, ReceiveEndpointContext context, Func<ITransportSupervisor<TContext>> supervisorFactory,
            IPipe<TContext> transportPipe)
        {
            _hostConfiguration = hostConfiguration;
            _context = context;
            _supervisorFactory = supervisorFactory;
            _transportPipe = transportPipe;
        }

        public IPipe<TContext> PreStartPipe { get; set; }

        public void Probe(ProbeContext context)
        {
            var scope = context.CreateScope("receiveTransport");

            _context.Probe(scope);
        }

        /// <summary>
        /// Start the receive transport, returning a Task that can be awaited to signal the transport has
        /// completely shutdown once the cancellation token is cancelled.
        /// </summary>
        /// <returns>A task that is completed once the transport is shut down</returns>
        public ReceiveTransportHandle Start()
        {
            return new ReceiveTransportAgent(_hostConfiguration.ReceiveTransportRetryPolicy, _context, _supervisorFactory, _transportPipe, PreStartPipe);
        }

        public ConnectHandle ConnectReceiveObserver(IReceiveObserver observer)
        {
            return _context.ConnectReceiveObserver(observer);
        }

        public ConnectHandle ConnectReceiveTransportObserver(IReceiveTransportObserver observer)
        {
            return _context.ConnectReceiveTransportObserver(observer);
        }

        public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
        {
            return _context.ConnectPublishObserver(observer);
        }

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
            ITransportSupervisor<TContext> _supervisor;

            public ReceiveTransportAgent(IRetryPolicy retryPolicy, ReceiveEndpointContext context, Func<ITransportSupervisor<TContext>> supervisorFactory,
                IPipe<TContext> transportPipe, IPipe<TContext> preStartPipe)
            {
                _retryPolicy = retryPolicy;
                _context = context;
                _supervisorFactory = supervisorFactory;
                _transportPipe = transportPipe;
                _preStartPipe = preStartPipe;

                Task receiver = Run();
                SetCompleted(receiver);
            }

            public Task Stop(CancellationToken cancellationToken)
            {
                return this.Stop("Stop Receive Transport", cancellationToken);
            }

            protected override async Task StopAgent(StopContext context)
            {
                LogContext.SetCurrentIfNull(_context.LogContext);

                if (_supervisor != null)
                    await _supervisor.Stop(context).ConfigureAwait(false);

                await Completed.ConfigureAwait(false);
            }

            async Task Run()
            {
                var stoppingContext = new TransportStoppingContext(Stopping);
                stoppingContext.SetTimeProvider(_context.GetTimeProvider());

                using RetryPolicyContext<TransportStoppingContext> policyContext = _retryPolicy.CreatePolicyContext(stoppingContext)
                    ?? throw new InvalidOperationException("The receive transport retry policy returned a null policy context.");

                RetryContext<TransportStoppingContext> retryContext = null;

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

                            Task preRetry = retryContext.PreRetry()
                                ?? throw new InvalidOperationException("The receive transport retry context returned a null pre-retry task.");
                            if (preRetry.Status != TaskStatus.RanToCompletion)
                                await preRetry.ConfigureAwait(false);
                        }

                        Stopping.ThrowIfCancellationRequested();
                        await RunTransport().ConfigureAwait(false);

                        if (!Stopping.IsCancellationRequested)
                        {
                            var exception = new ConnectionException(
                                $"Receive transport completed before shutdown: {_context.InputAddress}", isTransient: true);
                            await NotifyFaulted(exception, false).ConfigureAwait(false);
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
                                Task retryFaulted = nextRetryContext.RetryFaulted(exception)
                                    ?? throw new InvalidOperationException("The receive transport retry context returned a null retry-faulted task.");
                                if (retryFaulted.Status != TaskStatus.RanToCompletion)
                                    await retryFaulted.ConfigureAwait(false);
                            }

                            LogContext.Error?.Log(exception, "ReceiveTransport retry budget exhausted: {InputAddress}", _context.InputAddress);
                            await NotifyFaulted(exception, true).ConfigureAwait(false);
                            break;
                        }

                        retryContext = nextRetryContext;
                    }
                }
            }

            async Task RunTransport()
            {
                try
                {
                    _supervisor = _supervisorFactory();

                    if (_preStartPipe.IsNotEmpty())
                        await _supervisor.Send(_preStartPipe, Stopping).ConfigureAwait(false);

                    // Nothing connected to the pipe, so signal early we are available
                    if (!_context.ReceivePipe.Connected.IsCompleted)
                        await _context.OnTransportStartup(_supervisor, Stopping).ConfigureAwait(false);

                    if (!IsStopping)
                        await _supervisor.Send(_transportPipe, Stopped).ConfigureAwait(false);
                }
                catch (ConnectionException exception)
                {
                    await NotifyFaulted(exception, false).ConfigureAwait(false);
                    throw;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    throw await NotifyFaulted(exception, "ReceiveTransport faulted: ").ConfigureAwait(false);
                }
            }

            async Task<Exception> NotifyFaulted(Exception originalException, string message)
            {

                var exception = _context.ConvertException(originalException, message);


                await NotifyFaulted(exception, false).ConfigureAwait(false);

                return exception;
            }




            Task NotifyFaulted(Exception exception, bool isTerminal)
            {
                return _context.TransportObservers.NotifyFaulted(_context.InputAddress, exception, isTerminal);
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
}
