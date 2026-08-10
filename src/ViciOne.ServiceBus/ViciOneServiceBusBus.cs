// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
#nullable enable
namespace ViciOne.ServiceBus
{
    using System;
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
        void WaitUntilBusEndpointIsReady()
        {
            if (_busHandle == null || _receiveEndpoint.Started.IsCompletedSuccessfully())
                return;

            using var timeout = new CancellationTokenSource(ReadyTimeout);
            try
            {
                TaskUtil.Await(_receiveEndpoint.Started, timeout.Token);
            }
            // Asked of the source rather than of the exception: a cancellation raised through a linked
            // token carries neither, which is how a comparable check elsewhere in this transport went
            // unreachable.
            catch (OperationCanceledException) when (timeout.IsCancellationRequested)
            {
                throw new ConnectionException(
                    $"The bus endpoint did not become ready within {ReadyTimeout.TotalSeconds:0} s, so the consumer "
                    + $"could not be connected: {Address}");
            }
        }

        ConnectHandle IConsumePipeConnector.ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe)
        {
            LogContext.SetCurrentIfNull(_logContext);

            var handle = _consumePipe.ConnectConsumePipe(pipe);

            WaitUntilBusEndpointIsReady();

            return handle;
        }

        ConnectHandle IConsumePipeConnector.ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe, ConnectPipeOptions options)
        {
            LogContext.SetCurrentIfNull(_logContext);

            var handle = _consumePipe.ConnectConsumePipe(pipe, options);

            WaitUntilBusEndpointIsReady();

            return handle;
        }

        ConnectHandle IRequestPipeConnector.ConnectRequestPipe<T>(Guid requestId, IPipe<ConsumeContext<T>> pipe)
        {
            LogContext.SetCurrentIfNull(_logContext);

            var handle = _consumePipe.ConnectRequestPipe(requestId, pipe);

            WaitUntilBusEndpointIsReady();

            return handle;
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

            await _busHandle.StopAsync(cancellationToken).ConfigureAwait(false);

            _busHandle = null;
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
