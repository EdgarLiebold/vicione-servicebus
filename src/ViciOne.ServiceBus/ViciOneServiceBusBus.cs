// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
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
        Handle? _busHandle;

        /// <summary>The bus endpoint's failure that waiting cannot resolve, once there is one.</summary>
        TerminalFaultObserver? _terminalFault;
        ConnectHandle? _terminalFaultHandle;

        BusState _busState;
        string _healthMessage = "not started";

        public ViciOneServiceBusBus(IHost host, IBusObserver busObservable, IReceiveEndpointConfiguration endpointConfiguration)
        {
            Address = endpointConfiguration.InputAddress;
            _consumePipe = endpointConfiguration.ConsumePipe;
            _host = host;
            _busObservable = busObservable;
            _receiveEndpoint = endpointConfiguration.ReceiveEndpoint;

            _busState = BusState.Created;

            Topology = host.Topology;

            if (LogContext.Current == null)
                throw new ConfigurationException("The LogContext was not set.");

            _logContext = LogContext.Current;

            _publishEndpoint = new PublishEndpoint(_receiveEndpoint);
        }

        /// <summary>
        /// Waits for the bus endpoint to be ready after a consumer has been connected to a running bus.
        /// <para>
        /// The bus endpoint is materialised on demand: it declares its queue when something first
        /// consumes on the bus, not when the bus starts. Connecting a consumer is therefore what brings
        /// it up, and waiting here is deliberate — handing back a subscription that is not live yet
        /// would silently drop messages.
        /// </para>
        /// <para>
        /// The wait used to be unbounded and uncancellable. An endpoint that can never start, for
        /// instance because another connection already holds its exclusive queue, left the caller
        /// blocked on its thread for good: no exception, no timeout, nothing naming a cause. Measured
        /// against the pinned fixture, StartAsync returned normally and the first ConnectHandler never
        /// came back. The bound below is the same sixty seconds this class already applies in
        /// <see cref="StartAsync" /> when a caller supplies no token of its own, so no second notion of
        /// "too long" is introduced, and TaskUtil.Await already accepted a token — it was simply never
        /// given one.
        /// </para>
        /// </summary>
        /// <summary>
        /// Waits for the on-demand bus endpoint, and stops waiting when there is nothing left to wait for.
        /// <para>
        /// A fault the transport recovers from leaves the wait exactly as it was: the retry runs and the
        /// endpoint still becomes ready, which is the behaviour every recoverable hiccup during startup
        /// depends on. A fault it cannot recover from ends the wait with the transport's own exception,
        /// because the endpoint will not become ready by itself and holding the caller for the full
        /// readiness limit replaces the broker's answer with a timeout that says nothing.
        /// </para>
        /// <para>
        /// The observer is connected here rather than in the receive endpoint, and that is deliberate.
        /// An earlier revision completed the endpoint's own Started task on any unrecoverable fault,
        /// which reaches every endpoint in the process and not only the one being waited for: measured,
        /// six unrelated specs in the RabbitMQ suite failed, among them the ones that deliberately
        /// provoke a refused credential — also an unrecoverable fault, and one whose existing behaviour
        /// was never in question. The wait is the only place that needs to know, so it is the only place
        /// that is told.
        /// </para>
        /// </summary>
        void WaitUntilBusEndpointIsReady()
        {
            if (_busHandle == null || _receiveEndpoint.Started.IsCompletedSuccessfully())
                return;

            var terminal = _terminalFault;

            using var timeout = new CancellationTokenSource(ReadyTimeout);

            terminal?.Attach(timeout);
            try
            {
                TaskUtil.Await(_receiveEndpoint.Started, timeout.Token);
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
        /// The filter is ConnectionException.IsTransient, which is the assembly-crossing contract and is
        /// checked by the compiler. Its earlier reading was what made it look unusable: an ordinary
        /// shutdown announced itself as non-transient, so filtering here turned every routine bus stop
        /// into a dead endpoint and failed unrelated specs across the suite — always the run after a
        /// stop, never the first one, which is why it never showed in isolation. The transport now says
        /// transient about its own stop, and the flag answers the question it was named for.
        /// </para>
        /// </summary>
        class TerminalFaultObserver :
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

                Wake(waiter);
            }



            public void Detach(CancellationTokenSource waiter)
            {
                lock (_lock)
                    _waiting.Remove(waiter);
            }

            /// <summary>
            /// Wakes a waiter away from the caller's thread.
            /// <para>
            /// CancellationTokenSource.Cancel runs its registrations synchronously, and this observer is
            /// notified from ReceiveTransport, which awaits the observer chain. Cancelling inline would
            /// therefore run a waiter's continuation on the transport's own fault path and hold it there
            /// while it runs — the reentrancy record 0041 named as a candidate. The wake-up is handed to
            /// the thread pool instead, so the notification returns to the transport at once.
            /// </para>
            /// </summary>
            static void Wake(CancellationTokenSource waiter)
            {
                ThreadPool.QueueUserWorkItem(state =>
                {
                    try
                    {
                        ((CancellationTokenSource)state!).Cancel();
                    }
                    catch (ObjectDisposedException)
                    {
                        // That waiter gave up on its own in the meantime.
                    }
                }, waiter);
            }

            Task IReceiveEndpointObserver.Faulted(ReceiveEndpointFaulted faulted)
            {

                CancellationTokenSource[] waiting;

                lock (_lock)
                {
                    if (_cause != null || !IsTerminal(faulted.Exception))
                        return Task.CompletedTask;

                    // The cause is set before anyone is woken, or a waiter could read an empty reason
                    // and report the safety limit instead of the broker's answer.
                    _cause = faulted.Exception;

                    waiting = _waiting.ToArray();
                    _waiting.Clear();
                }

                foreach (var waiter in waiting)
                    Wake(waiter);

                return Task.CompletedTask;
            }

            /// <summary>
            /// Whether waiting longer cannot change this failure. The cause is walked, because the
            /// endpoint reports what it caught and the transport's answer is often one level down.
            /// </summary>
            static bool IsTerminal(Exception exception)
            {
                for (var cause = exception; cause != null; cause = cause.InnerException)
                {
                    if (cause is ConnectionException connection)
                        return !connection.IsTransient;
                }

                return false;
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
                    tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(60));
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
