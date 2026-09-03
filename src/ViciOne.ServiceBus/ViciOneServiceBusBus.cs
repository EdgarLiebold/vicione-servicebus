#nullable enable
namespace ViciOne.ServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Configuration;
    using Events;
    using Internals;
    using Logging;
    using Transports;
    using Util;


    public class ViciOneServiceBusBus :
        IBusControl
    {
        /// <summary>
        /// How long a consumer connection waits for the on-demand bus endpoint. Same value StartAsync
        /// falls back to when the caller supplies no token, so both express one notion of "too long".
        /// </summary>
        static readonly TimeSpan ReadyTimeout = TimeSpan.FromSeconds(60);

        readonly IBusObserver _busObservable;
        readonly IConsumePipe _consumePipe;
        readonly IHost _host;
        readonly ILogContext _logContext;
        readonly IPublishEndpoint _publishEndpoint;
        readonly IReceiveEndpoint _receiveEndpoint;
        readonly TimeProvider _timeProvider;
        Handle? _busHandle;

        /// <summary>The bus endpoint's failure that waiting cannot resolve, once there is one.</summary>
        TerminalFaultObserver? _terminalFault;
        ConnectHandle? _terminalFaultHandle;

        BusState _busState;
        string _healthMessage = "not started";

        public ViciOneServiceBusBus(IHost host, IBusObserver busObservable, IReceiveEndpointConfiguration endpointConfiguration,
            TimeProvider? timeProvider = null)
        {
            Address = endpointConfiguration.InputAddress;
            _consumePipe = endpointConfiguration.ConsumePipe;
            _host = host;
            _busObservable = busObservable;
            _receiveEndpoint = endpointConfiguration.ReceiveEndpoint;
            _timeProvider = timeProvider ?? TimeProvider.System;

            _busState = BusState.Created;

            Topology = host.Topology;

            if (LogContext.Current == null)
                throw new ConfigurationException("The LogContext was not set.");

            _logContext = LogContext.Current;

            _publishEndpoint = new PublishEndpoint(_receiveEndpoint);
        }

        /// <summary>
        /// Waits for the bus endpoint to be ready after a consumer has been connected to a running bus,
        /// and stops waiting when there is nothing left to wait for.
        /// <para>
        /// The bus endpoint is materialised on demand: it declares its queue when something first
        /// consumes on the bus, not when the bus starts. Connecting a consumer is therefore what brings
        /// it up, and waiting here is deliberate — handing back a subscription that is not live yet
        /// would silently drop messages.
        /// </para>
        /// <para>
        /// The wait is bounded and cancellable. An endpoint that can never start, for instance because
        /// another connection already holds its exclusive queue, would otherwise block the caller on its
        /// thread for good: no exception, no timeout, nothing naming a cause. The bound is the same
        /// sixty seconds this class applies in <see cref="StartAsync" /> when a caller supplies no token
        /// of its own, so there is no second notion of "too long".
        /// </para>
        /// <para>
        /// A fault the transport recovers from leaves the wait exactly as it is: the retry runs and the
        /// endpoint still becomes ready, which is the behaviour every recoverable hiccup during startup
        /// depends on. A fault it cannot recover from ends the wait with the transport's own exception,
        /// because the endpoint will not become ready by itself and holding the caller for the full
        /// readiness limit replaces the broker's answer with a timeout that says nothing.
        /// </para>
        /// <para>
        /// The observer is connected here rather than in the receive endpoint, and that is deliberate.
        /// Completing the endpoint's own Started task on an unrecoverable fault reaches every endpoint
        /// in the process and not only the one being waited for, including the specs that deliberately
        /// provoke a refused credential. The wait is the only place that needs to know, so it is the
        /// only place that is told.
        /// </para>
        /// </summary>
        void WaitUntilBusEndpointIsReady()
        {
            if (_busHandle == null || _receiveEndpoint.Started.IsCompletedSuccessfully())
                return;

            var terminal = _terminalFault;

            using var timeout = new CancellationTokenSource(ReadyTimeout, _timeProvider);

            terminal?.Attach(timeout);
            try
            {
                TaskBlocking.Wait(_receiveEndpoint.Started, timeout.Token);
            }
            // Asked of the source rather than of the exception: a cancellation raised through a linked
            // token carries neither, which is how a comparable check elsewhere in this transport went
            // unreachable.
            catch (OperationCanceledException) when (timeout.IsCancellationRequested || terminal?.Cause != null)
            {
                // The broker's own answer first; the safety limit only when there is none to give.
                if (terminal?.Cause is { } cause)
                    throw cause;

                throw new ConnectionException(
                    $"The bus endpoint did not become ready within {ReadyTimeout.TotalSeconds:0} s, so the consumer "
                    + $"could not be connected: {Address}");
            }
            finally
            {
                terminal?.Detach(timeout);
            }
        }


        /// <summary>
        /// Remembers the failure of the bus endpoint that waiting cannot resolve, and ends the wait.
        /// <para>
        /// It ends it by cancelling rather than by offering a second task to wait on: this wait is
        /// entered by every ConnectConsumePipe and every ConnectRequestPipe, so a request client passes
        /// through it on each request, and it keeps exactly the shape it had.
        /// </para>
        /// <para>
        /// Terminality is supplied by the receive transport's retry owner. Observers therefore do not
        /// infer lifecycle state from exception types or provider-specific transient flags: a recoverable
        /// attempt fault leaves waiters attached, while retry exhaustion or another definitive startup
        /// failure wakes them with the original cause.
        /// </para>
        /// </summary>
        internal class TerminalFaultObserver :
            IReceiveEndpointObserver
        {
            readonly object _lock = new();
            readonly List<CancellationTokenSource> _waiting = new();

            Exception? _cause;

            /// <summary>
            /// The failure, published and read under the same lock.
            /// <para>
            /// An auto-property would have been written inside the lock and read outside it, which is
            /// not a synchronisation edge at all — a waiter woken by the cancellation could see the
            /// cancellation before the reason and report the safety limit instead of the broker's
            /// answer. The lock is held on both sides so that ordering is a fact rather than a hope.
            /// </para>
            /// </summary>
            public Exception? Cause
            {
                get
                {
                    lock (_lock)
                        return _cause;
                }
            }

            /// <summary>Registers a waiter, and wakes it at once if the failure already happened.</summary>
            public void Attach(CancellationTokenSource waiter)
            {
                lock (_lock)
                {
                    if (_cause == null)
                    {
                        _waiting.Add(waiter);
                        return;
                    }
                }

                CancelAttachedWaiter(waiter);
            }



            public void Detach(CancellationTokenSource waiter)
            {
                lock (_lock)
                    _waiting.Remove(waiter);
            }

            static void CancelAttachedWaiter(CancellationTokenSource waiter)
            {
                try
                {
                    waiter.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    // The waiter completed between observing the terminal cause and this cancellation.
                }
                catch (Exception exception)
                {
                    // The terminal cause remains authoritative. A cancellation callback is observation
                    // attached to the internal wait and must not replace the transport failure.
                    LogContext.Warning?.Log(exception, "Bus endpoint readiness cancellation callback faulted");
                }
            }

            static async Task CancelWaitersAsync(CancellationTokenSource[] waiters)
            {
                foreach (var waiter in waiters)
                {
                    try
                    {
                        await waiter.CancelAsync().ConfigureAwait(false);
                    }
                    catch (ObjectDisposedException)
                    {
                        // The waiter completed independently while the terminal fault was being published.
                    }
                    catch (Exception exception)
                    {
                        // Cancellation wakes an internal readiness wait. Callback failures must be owned and
                        // observable, but must never replace the receive transport's terminal failure.
                        LogContext.Warning?.Log(exception, "Bus endpoint readiness cancellation callback faulted");
                    }
                }
            }

            async Task IReceiveEndpointObserver.Faulted(ReceiveEndpointFaulted faulted)
            {
                CancellationTokenSource[] waiting;

                lock (_lock)
                {
                    if (_cause != null || !faulted.IsTerminal)
                        return;

                    _cause = faulted.Exception;

                    waiting = _waiting.ToArray();
                    _waiting.Clear();
                }

                await CancelWaitersAsync(waiting).ConfigureAwait(false);
            }

            Task IReceiveEndpointObserver.Ready(ReceiveEndpointReady ready)
            {
                return Task.CompletedTask;
            }

            Task IReceiveEndpointObserver.Stopping(ReceiveEndpointStopping stopping)
            {
                return Task.CompletedTask;
            }

            Task IReceiveEndpointObserver.Completed(ReceiveEndpointCompleted completed)
            {
                return Task.CompletedTask;
            }
        }


        /// <summary>
        /// Waits for the bus endpoint and hands the caller its handle, or gives the connection back.
        /// <para>
        /// The pipe is connected before the wait. Bounding that wait turned a throw from something that
        /// only happened when the endpoint faulted into the regular failure case, and on that path the
        /// handle was lost: the caller could no longer disconnect, the pipe stayed registered for the
        /// lifetime of the bus, and a request pipe kept its request id bound with it.
        /// </para>
        /// </summary>
        ConnectHandle WaitForBusEndpoint(ConnectHandle handle)
        {
            try
            {
                WaitUntilBusEndpointIsReady();
            }
            catch
            {
                handle.Disconnect();
                throw;
            }

            return handle;
        }

        ConnectHandle IConsumePipeConnector.ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe)
        {
            LogContext.SetCurrentIfNull(_logContext);

            return WaitForBusEndpoint(_consumePipe.ConnectConsumePipe(pipe));
        }

        ConnectHandle IConsumePipeConnector.ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe, ConnectPipeOptions options)
        {
            LogContext.SetCurrentIfNull(_logContext);

            return WaitForBusEndpoint(_consumePipe.ConnectConsumePipe(pipe, options));
        }

        ConnectHandle IRequestPipeConnector.ConnectRequestPipe<T>(Guid requestId, IPipe<ConsumeContext<T>> pipe)
        {
            LogContext.SetCurrentIfNull(_logContext);

            return WaitForBusEndpoint(_consumePipe.ConnectRequestPipe(requestId, pipe));
        }

        Task IPublishEndpoint.Publish<T>(T message, CancellationToken cancellationToken)
        {
            LogContext.SetCurrentIfNull(_logContext);

            return _publishEndpoint.Publish(message, cancellationToken);
        }

        Task IPublishEndpoint.Publish<T>(T message, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken)
        {
            LogContext.SetCurrentIfNull(_logContext);

            return _publishEndpoint.Publish(message, publishPipe, cancellationToken);
        }

        Task IPublishEndpoint.Publish<T>(T message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken)
        {
            LogContext.SetCurrentIfNull(_logContext);

            return _publishEndpoint.Publish(message, publishPipe, cancellationToken);
        }

        Task IPublishEndpoint.Publish(object message, CancellationToken cancellationToken)
        {
            LogContext.SetCurrentIfNull(_logContext);

            return _publishEndpoint.Publish(message, cancellationToken);
        }

        Task IPublishEndpoint.Publish(object message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken)
        {
            LogContext.SetCurrentIfNull(_logContext);

            return _publishEndpoint.Publish(message, publishPipe, cancellationToken);
        }

        Task IPublishEndpoint.Publish(object message, Type messageType, CancellationToken cancellationToken)
        {
            LogContext.SetCurrentIfNull(_logContext);

            return _publishEndpoint.Publish(message, messageType, cancellationToken);
        }

        Task IPublishEndpoint.Publish(object message, Type messageType, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken)
        {
            LogContext.SetCurrentIfNull(_logContext);

            return _publishEndpoint.Publish(message, messageType, publishPipe, cancellationToken);
        }

        Task IPublishEndpoint.Publish<T>(object values, CancellationToken cancellationToken)
        {
            LogContext.SetCurrentIfNull(_logContext);

            return _publishEndpoint.Publish<T>(values, cancellationToken);
        }

        Task IPublishEndpoint.Publish<T>(object values, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken)
        {
            LogContext.SetCurrentIfNull(_logContext);

            return _publishEndpoint.Publish(values, publishPipe, cancellationToken);
        }

        Task IPublishEndpoint.Publish<T>(object values, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken)
        {
            LogContext.SetCurrentIfNull(_logContext);

            return _publishEndpoint.Publish<T>(values, publishPipe, cancellationToken);
        }

        public Uri Address { get; }

        public IBusTopology Topology { get; }

        Task<ISendEndpoint> ISendEndpointProvider.GetSendEndpoint(Uri address)
        {
            LogContext.SetCurrentIfNull(_logContext);

            return _receiveEndpoint.GetSendEndpoint(address);
        }

        public async Task<BusHandle> StartAsync(CancellationToken cancellationToken)
        {
            LogContext.SetCurrentIfNull(_logContext);

            if (_busHandle != null)
            {
                LogContext.Warning?.Log("StartAsync called, but the bus was already started: {Address} ({Reason})", Address, "Already Started");
                return _busHandle;
            }

            await _busObservable.PreStart(this).ConfigureAwait(false);

            Handle? busHandle = null;

            CancellationTokenSource? tokenSource = null;
            try
            {
                if (cancellationToken == default)
                {
                    tokenSource = new CancellationTokenSource(ReadyTimeout, _timeProvider);
                    cancellationToken = tokenSource.Token;
                }

                var hostHandle = _host.Start(cancellationToken);

                busHandle = new Handle(_host, hostHandle, this, _busObservable, _logContext);

                try
                {
                    await busHandle.Ready.OrCanceled(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException exception) when (exception.CancellationToken == cancellationToken)
                {
                    LogContext.Warning?.Log(exception, "Bus start canceled: {HostAddress}", _host.Address);

                    try
                    {
                        await busHandle.StopAsync(TimeSpan.FromSeconds(30)).ConfigureAwait(false);
                    }
                    catch (Exception stopException)
                    {
                        LogContext.Warning?.Log(stopException, "Bus start canceled, bus stop faulted: {HostAddress}", _host.Address);
                    }

                    await busHandle.Ready.ConfigureAwait(false);
                }

                await _busObservable.PostStart(this, busHandle.Ready).ConfigureAwait(false);

                _busHandle = busHandle;

                _terminalFault = new TerminalFaultObserver();
                _terminalFaultHandle = (_receiveEndpoint as ReceiveEndpoint)?.ConnectReceiveEndpointObserver(_terminalFault);

                _busState = BusState.Started;
                _healthMessage = "";

                LogContext.Info?.Log("Bus started: {HostAddress}", _host.Address);

                return _busHandle;
            }
            catch (Exception ex)
            {
                try
                {
                    if (busHandle != null)
                    {
                        LogContext.Warning?.Log(ex, "Bus start faulted: {HostAddress}", _host.Address);

                        await busHandle.StopAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception stopException)
                {
                    LogContext.Warning?.Log(stopException, "Bus start faulted, bus stop faulted: {HostAddress}", _host.Address);
                }

                _busState = BusState.Faulted;
                _healthMessage = $"start faulted: {ex.Message}";

                await _busObservable.StartFaulted(this, ex).ConfigureAwait(false);

                throw;
            }
            finally
            {
                tokenSource?.Dispose();
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            LogContext.SetCurrentIfNull(_logContext);

            if (_busHandle == null)
            {
                LogContext.Warning?.Log("Failed to stop bus: {Address} ({Reason})", Address, "Not Started");
                return;
            }

            // Released whatever the stop does. A stop that throws used to leave the observer connected
            // and its terminal failure remembered, so the next successful start inherited a refusal that
            // belonged to the bus before it.
            try
            {
                await _busHandle.StopAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _terminalFaultHandle?.Disconnect();
                _terminalFaultHandle = null;
                _terminalFault = null;

                _busHandle = null;
            }
        }

        public BusHealthResult CheckHealth()
        {
            return _host.CheckHealth(_busState, _healthMessage);
        }

        ConnectHandle IConsumeObserverConnector.ConnectConsumeObserver(IConsumeObserver observer)
        {
            LogContext.SetCurrentIfNull(_logContext);

            return _host.ConnectConsumeObserver(observer);
        }

        ConnectHandle IConsumeMessageObserverConnector.ConnectConsumeMessageObserver<T>(IConsumeMessageObserver<T> observer)
        {
            LogContext.SetCurrentIfNull(_logContext);

            return _host.ConnectConsumeMessageObserver(observer);
        }

        public ConnectHandle ConnectReceiveObserver(IReceiveObserver observer)
        {
            LogContext.SetCurrentIfNull(_logContext);

            return _host.ConnectReceiveObserver(observer);
        }

        ConnectHandle IReceiveEndpointObserverConnector.ConnectReceiveEndpointObserver(IReceiveEndpointObserver observer)
        {
            LogContext.SetCurrentIfNull(_logContext);

            return _host.ConnectReceiveEndpointObserver(observer);
        }

        public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
        {
            LogContext.SetCurrentIfNull(_logContext);

            return _host.ConnectPublishObserver(observer);
        }

        public Task<ISendEndpoint> GetPublishSendEndpoint<T>()
            where T : class
        {
            LogContext.SetCurrentIfNull(_logContext);

            return _receiveEndpoint.GetPublishSendEndpoint<T>();
        }

        public ConnectHandle ConnectSendObserver(ISendObserver observer)
        {
            LogContext.SetCurrentIfNull(_logContext);

            return _host.ConnectSendObserver(observer);
        }

        ConnectHandle IEndpointConfigurationObserverConnector.ConnectEndpointConfigurationObserver(IEndpointConfigurationObserver observer)
        {
            LogContext.SetCurrentIfNull(_logContext);

            return _host.ConnectEndpointConfigurationObserver(observer);
        }

        HostReceiveEndpointHandle IReceiveConnector.ConnectReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
            Action<IReceiveEndpointConfigurator>? configureEndpoint)
        {
            return _host.ConnectReceiveEndpoint(definition, endpointNameFormatter, configureEndpoint);
        }

        HostReceiveEndpointHandle IReceiveConnector.ConnectReceiveEndpoint(string queueName, Action<IReceiveEndpointConfigurator>? configureEndpoint)
        {
            return _host.ConnectReceiveEndpoint(queueName, configureEndpoint);
        }

        void IProbeSite.Probe(ProbeContext context)
        {
            var scope = context.CreateScope("bus");
            scope.Add("address", Address);

            _host.Probe(scope);
        }


        class Handle :
            BusHandle
        {
            readonly ViciOneServiceBusBus _bus;
            readonly IBusObserver _busObserver;
            readonly IHost _host;
            readonly HostHandle _hostHandle;
            readonly ILogContext _logContext;
            bool _stopped;

            public Handle(IHost host, HostHandle hostHandle, ViciOneServiceBusBus bus, IBusObserver busObserver, ILogContext logContext)
            {
                _host = host;
                _bus = bus;
                _busObserver = busObserver;
                _logContext = logContext;
                _hostHandle = hostHandle;

                Ready = ReadyOrNot(hostHandle.Ready);
            }

            public Task<BusReady> Ready { get; }

            public async Task StopAsync(CancellationToken cancellationToken)
            {
                LogContext.SetCurrentIfNull(_logContext);

                if (_stopped)
                    return;

                await _busObserver.PreStop(_bus).ConfigureAwait(false);

                try
                {
                    await _hostHandle.Stop(cancellationToken).ConfigureAwait(false);

                    await _busObserver.PostStop(_bus).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception exception)
                {
                    await _busObserver.StopFaulted(_bus, exception).ConfigureAwait(false);

                    LogContext.Warning?.Log(exception, "Bus stop faulted: {HostAddress}", _host.Address);

                    _bus._busState = BusState.Faulted;
                    _bus._healthMessage = $"stop faulted: {exception.Message}";

                    throw;
                }

                LogContext.Info?.Log("Bus stopped: {HostAddress}", _host.Address);

                _stopped = true;

                _bus._busState = BusState.Stopped;
                _bus._healthMessage = "stopped";
            }

            async Task<BusReady> ReadyOrNot(Task<HostReady> ready)
            {
                var hostReady = await ready.ConfigureAwait(false);

                return new BusReadyEvent(hostReady, _bus);
            }
        }
    }
}
