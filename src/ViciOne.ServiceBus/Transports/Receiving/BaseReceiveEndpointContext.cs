using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Observables;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Owns receive-pipeline configuration, observers, transport providers, and endpoint-scoped resources.</summary>
public abstract class BaseReceiveEndpointContext :
    BasePipeContext,
    ReceiveEndpointContext
{
    readonly ReceiveEndpointObservable _endpointObservers;
    readonly IHostConfiguration _hostConfiguration;
    readonly PublishObservable _publishObservers;
    readonly Lazy<IPublishPipe> _publishPipe;
    readonly IPublishTopologyConfigurator _publishTopology;
    readonly ReceiveObservable _receiveObservers;
    readonly Lazy<IReceivePipe> _receivePipe;
    readonly SendObservable _sendObservers;
    readonly Lazy<ISendPipe> _sendPipe;
    readonly ReceiveTransportObservable _transportObservers;
    readonly SemaphoreSlim _resetGate = new(1, 1);

    Lazy<IPublishEndpointProvider> _publishEndpointProvider;
    Lazy<IPublishTransportProvider> _publishTransportProvider;
    Lazy<ISendEndpointProvider> _sendEndpointProvider;
    Lazy<ISendTransportProvider> _sendTransportProvider;

    /// <summary>Initializes an endpoint context from validated host and endpoint configurations.</summary>
    /// <param name="hostConfiguration">The host configuration shared by transport providers.</param>
    /// <param name="configuration">The endpoint configuration that supplies pipes, observers, and addresses.</param>
    protected BaseReceiveEndpointContext(IHostConfiguration hostConfiguration, IReceiveEndpointConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(hostConfiguration);
        ArgumentNullException.ThrowIfNull(configuration);

        _hostConfiguration = hostConfiguration;

        if (hostConfiguration is IMessageLimitsHostConfiguration { MessageLimits: { } limits })
            GetOrAddPayload(() => limits);

        InputAddress = configuration.InputAddress
            ?? throw new InvalidOperationException("The receive endpoint configuration returned no input address.");
        HostAddress = configuration.HostAddress
            ?? throw new InvalidOperationException("The receive endpoint configuration returned no host address.");
        PublishFaults = configuration.PublishFaults;
        PrefetchCount = configuration.PrefetchCount;
        ConcurrentMessageLimit = configuration.ConcurrentMessageLimit;

        IsBusEndpoint = configuration.IsBusEndpoint;

        ITopologyConfiguration topology = configuration.Topology
            ?? throw new InvalidOperationException("The receive endpoint configuration returned no topology configuration.");
        _publishTopology = topology.Publish
            ?? throw new InvalidOperationException("The receive endpoint configuration returned no publish topology.");

        _sendObservers = new SendObservable();
        _publishObservers = new PublishObservable();

        _endpointObservers = configuration.EndpointObservers
            ?? throw new InvalidOperationException("The receive endpoint configuration returned no endpoint observer.");
        _receiveObservers = configuration.ReceiveObservers
            ?? throw new InvalidOperationException("The receive endpoint configuration returned no receive observer.");
        _transportObservers = configuration.TransportObservers
            ?? throw new InvalidOperationException("The receive endpoint configuration returned no transport observer.");

        DependenciesReady = configuration.DependenciesReady
            ?? throw new InvalidOperationException("The receive endpoint configuration returned no dependencies-ready task.");
        DependentsCompleted = configuration.DependentsCompleted
            ?? throw new InvalidOperationException("The receive endpoint configuration returned no dependents-completed task.");

        ISerializationConfiguration serialization = configuration.Serialization
            ?? throw new InvalidOperationException("The receive endpoint configuration returned no serialization configuration.");
        Serialization = serialization.CreateSerializerCollection()
            ?? throw new InvalidOperationException("The receive endpoint configuration returned no serializer collection.");

        ISendPipeConfiguration sendConfiguration = configuration.Send
            ?? throw new InvalidOperationException("The receive endpoint configuration returned no send-pipe configuration.");
        IPublishPipeConfiguration publishConfiguration = configuration.Publish
            ?? throw new InvalidOperationException("The receive endpoint configuration returned no publish-pipe configuration.");

        _sendPipe = new Lazy<ISendPipe>(() => sendConfiguration.CreatePipe()
            ?? throw new InvalidOperationException("The receive endpoint configuration returned no send pipe."));
        _publishPipe = new Lazy<IPublishPipe>(() => publishConfiguration.CreatePipe()
            ?? throw new InvalidOperationException("The receive endpoint configuration returned no publish pipe."));
        _receivePipe = new Lazy<IReceivePipe>(() => configuration.CreateReceivePipe()
            ?? throw new InvalidOperationException("The receive endpoint configuration returned no receive pipe."));

        _sendTransportProvider = CreateSendTransportProviderLazy();
        _publishTransportProvider = CreatePublishTransportProviderLazy();

        _sendEndpointProvider = CreateSendEndpointProviderLazy();
        _publishEndpointProvider = CreatePublishEndpointProviderLazy();

        hostConfiguration.ConnectReceiveEndpointContext(this);
    }

    Uri HostAddress { get; }

    /// <summary>Gets whether this endpoint receives messages addressed to the bus itself.</summary>
    public bool IsBusEndpoint { get; }

    /// <summary>Gets the aggregate receive-pipeline observer.</summary>
    public IReceiveObserver ReceiveObservers => _receiveObservers;

    /// <summary>Gets the aggregate receive-transport lifecycle observer.</summary>
    public IReceiveTransportObserver TransportObservers => _transportObservers;

    /// <summary>Gets the aggregate receive-endpoint lifecycle observer.</summary>
    public IReceiveEndpointObserver EndpointObservers => _endpointObservers;

    /// <summary>Gets the host message-route table.</summary>
    public IMessageRouteTable MessageRoutes => _hostConfiguration.BusConfiguration.MessageRoutes;

    /// <summary>Subscribes an observer to messages sent by the receive endpoint.</summary>
    /// <param name="observer">The observer that receives send notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _sendObservers.Connect(observer);
    }

    /// <summary>Subscribes an observer to messages published by the receive endpoint.</summary>
    /// <param name="observer">The observer that receives publish notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _publishObservers.Connect(observer);
    }

    /// <summary>Subscribes an observer to receive-transport lifecycle notifications.</summary>
    /// <param name="observer">The observer that receives transport lifecycle notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectReceiveTransportObserver(IReceiveTransportObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _transportObservers.Connect(observer);
    }

    /// <summary>Subscribes an observer to receive-pipeline notifications.</summary>
    /// <param name="observer">The observer that receives the notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectReceiveObserver(IReceiveObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _receiveObservers.Connect(observer);
    }

    /// <summary>Subscribes an observer to receive-endpoint lifecycle notifications.</summary>
    /// <param name="observer">The observer that receives endpoint lifecycle notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectReceiveEndpointObserver(IReceiveEndpointObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _endpointObservers.Connect(observer);
    }

    /// <summary>Gets the maximum graceful wait for active consumers.</summary>
    public TimeSpan? ConsumerStopTimeout => _hostConfiguration.ConsumerStopTimeout;
    /// <summary>Gets the maximum duration of endpoint shutdown.</summary>
    public TimeSpan? StopTimeout => _hostConfiguration.StopTimeout;

    /// <summary>Gets the address from which the endpoint receives messages.</summary>
    public Uri InputAddress { get; }

    /// <summary>Gets the task that completes when endpoint dependencies are ready.</summary>
    public Task DependenciesReady { get; }
    /// <summary>Gets the task that completes when endpoint dependents have stopped.</summary>
    public Task DependentsCompleted { get; }

    /// <summary>Gets whether unaddressed consumer faults are published.</summary>
    public bool PublishFaults { get; }

    /// <summary>Gets the transport prefetch limit.</summary>
    public int PrefetchCount { get; }
    /// <summary>Gets the endpoint-wide concurrent message limit.</summary>
    public int? ConcurrentMessageLimit { get; }

    /// <summary>Gets the endpoint-scoped diagnostic context.</summary>
    public ILogContext LogContext => _hostConfiguration.ReceiveLogContext
        ?? throw new InvalidOperationException("The host configuration returned no receive log context.");

    /// <summary>Gets the publish topology visible to the endpoint.</summary>
    public IPublishTopology Publish => _publishTopology;

    /// <summary>Gets the pipeline that processes received messages.</summary>
    public IReceivePipe ReceivePipe => _receivePipe.Value;

    /// <summary>Gets the endpoint provider used to send from the receive pipeline.</summary>
    public ISendEndpointProvider SendEndpointProvider => _sendEndpointProvider.Value;

    /// <summary>Gets the endpoint provider used to publish from the receive pipeline.</summary>
    public IPublishEndpointProvider PublishEndpointProvider => _publishEndpointProvider.Value;

    /// <summary>Creates an independently metered dispatcher over the endpoint receive pipeline.</summary>
    /// <returns>A new receive-pipeline dispatcher.</returns>
    public IReceivePipeDispatcher CreateReceivePipeDispatcher()
    {
        return new ReceivePipeDispatcher(_receivePipe.Value, _receiveObservers, _hostConfiguration, InputAddress);
    }

    /// <summary>Releases endpoint-provider resources and initializes empty provider caches for the next generation.</summary>
    /// <param name="cancellationToken">The token that cancels waiting to begin the reset.</param>
    /// <returns>A value task that completes after the current providers have been released.</returns>
    public async ValueTask ResetAsync(CancellationToken cancellationToken = default)
    {
        await _resetGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ISendEndpointProvider? sendEndpointProvider = _sendEndpointProvider.IsValueCreated ? _sendEndpointProvider.Value : null;
            IPublishEndpointProvider? publishEndpointProvider = _publishEndpointProvider.IsValueCreated ? _publishEndpointProvider.Value : null;

            Exception? sendReleaseFailure = null;
            if (sendEndpointProvider is not null)
            {
                try
                {
                    await ReleaseSendEndpointProviderAsync(sendEndpointProvider).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    sendReleaseFailure = exception;
                }
            }

            if (publishEndpointProvider is not null && !ReferenceEquals(publishEndpointProvider, sendEndpointProvider))
            {
                try
                {
                    await ReleasePublishEndpointProviderAsync(publishEndpointProvider).ConfigureAwait(false);
                }
                catch (Exception exception) when (sendReleaseFailure is not null)
                {
                    throw new AggregateException("Endpoint provider release failed.", sendReleaseFailure, exception);
                }
            }

            if (sendReleaseFailure is not null)
                ExceptionDispatchInfo.Capture(sendReleaseFailure).Throw();

            _sendTransportProvider = CreateSendTransportProviderLazy();
            _publishTransportProvider = CreatePublishTransportProviderLazy();

            _sendEndpointProvider = CreateSendEndpointProviderLazy();
            _publishEndpointProvider = CreatePublishEndpointProviderLazy();
        }
        finally
        {
            _resetGate.Release();
        }
    }

    /// <summary>Releases resources held by the current send endpoint provider.</summary>
    /// <param name="provider">The provider being retired.</param>
    /// <returns>A value task that completes after asynchronous disposal when supported.</returns>
    protected virtual ValueTask ReleaseSendEndpointProviderAsync(ISendEndpointProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        return provider is IAsyncDisposable disposable ? disposable.DisposeAsync() : default;
    }

    /// <summary>Releases resources held by the current publish endpoint provider.</summary>
    /// <param name="provider">The provider being retired.</param>
    /// <returns>A value task that completes after asynchronous disposal when supported.</returns>
    protected virtual ValueTask ReleasePublishEndpointProviderAsync(IPublishEndpointProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        return provider is IAsyncDisposable disposable ? disposable.DisposeAsync() : default;
    }

    /// <summary>Adds an agent that must stop after consume-side activity has ended.</summary>
    /// <param name="agent">The send-side agent owned by the endpoint.</param>
    public abstract void AddSendAgent(IAgent agent);
    /// <summary>Adds an agent that must stop with consume-side activity.</summary>
    /// <param name="agent">The consume-side agent owned by the endpoint.</param>
    public abstract void AddConsumeAgent(IAgent agent);

    /// <summary>Adds endpoint-specific diagnostics to a probe.</summary>
    /// <param name="context">The probe context that receives the diagnostics.</param>
    public virtual void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
    }

    /// <summary>Converts an unknown transport failure to a connection exception.</summary>
    /// <param name="exception">The original transport failure.</param>
    /// <param name="message">The contextual message prepended to the converted failure.</param>
    /// <returns>The connection exception understood by transport retry policy.</returns>
    public abstract Exception ConvertException(Exception exception, string message);

    /// <summary>Gets the serializer collection used by the endpoint.</summary>
    public ISerialization Serialization { get; }

    /// <summary>Creates the provider used to resolve send endpoints from this receive endpoint.</summary>
    /// <returns>A provider backed by the current send transport cache.</returns>
    protected virtual ISendEndpointProvider CreateSendEndpointProvider()
    {
        return new SendEndpointProvider(_sendTransportProvider.Value, _sendObservers, this, _sendPipe.Value);
    }

    /// <summary>Creates the provider used to resolve publish endpoints from this receive endpoint.</summary>
    /// <returns>A provider backed by the current publish transport cache.</returns>
    protected virtual IPublishEndpointProvider CreatePublishEndpointProvider()
    {
        return new PublishEndpointProvider(_publishTransportProvider.Value, HostAddress, _publishObservers, this, _publishPipe.Value, _publishTopology);
    }

    /// <summary>Creates the transport-specific provider used for direct sends.</summary>
    /// <returns>The send transport provider for the current endpoint generation.</returns>
    protected abstract ISendTransportProvider CreateSendTransportProvider();

    /// <summary>Creates the transport-specific provider used for publishes.</summary>
    /// <returns>The publish transport provider for the current endpoint generation.</returns>
    protected abstract IPublishTransportProvider CreatePublishTransportProvider();

    Lazy<ISendTransportProvider> CreateSendTransportProviderLazy()
    {
        return new Lazy<ISendTransportProvider>(() => CreateSendTransportProvider()
            ?? throw new InvalidOperationException("The receive endpoint context returned no send transport provider."));
    }

    Lazy<IPublishTransportProvider> CreatePublishTransportProviderLazy()
    {
        return new Lazy<IPublishTransportProvider>(() => CreatePublishTransportProvider()
            ?? throw new InvalidOperationException("The receive endpoint context returned no publish transport provider."));
    }

    Lazy<ISendEndpointProvider> CreateSendEndpointProviderLazy()
    {
        return new Lazy<ISendEndpointProvider>(() => CreateSendEndpointProvider()
            ?? throw new InvalidOperationException("The receive endpoint context returned no send endpoint provider."));
    }

    Lazy<IPublishEndpointProvider> CreatePublishEndpointProviderLazy()
    {
        return new Lazy<IPublishEndpointProvider>(() => CreatePublishEndpointProvider()
            ?? throw new InvalidOperationException("The receive endpoint context returned no publish endpoint provider."));
    }
}
