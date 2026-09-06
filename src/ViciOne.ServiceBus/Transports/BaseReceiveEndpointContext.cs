using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Observables;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Carries state for base receive endpoint operations.</summary>
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

    Lazy<IPublishEndpointProvider> _publishEndpointProvider;
    Lazy<IPublishTransportProvider> _publishTransportProvider;
    Lazy<ISendEndpointProvider> _sendEndpointProvider;
    Lazy<ISendTransportProvider> _sendTransportProvider;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="hostConfiguration">The host configuration.</param>
    /// <param name="configuration">The callback used to configure the component.</param>
    protected BaseReceiveEndpointContext(IHostConfiguration hostConfiguration, IReceiveEndpointConfiguration configuration)
    {
        _hostConfiguration = hostConfiguration;

        if (hostConfiguration is IMessageLimitsHostConfiguration { MessageLimits: { } limits })
            GetOrAddPayload(() => limits);

        InputAddress = configuration.InputAddress;
        HostAddress = configuration.HostAddress;
        PublishFaults = configuration.PublishFaults;
        PrefetchCount = configuration.PrefetchCount;
        ConcurrentMessageLimit = configuration.ConcurrentMessageLimit;

        IsBusEndpoint = configuration.IsBusEndpoint;

        _publishTopology = configuration.Topology.Publish;

        _sendObservers = new SendObservable();
        _publishObservers = new PublishObservable();

        _endpointObservers = configuration.EndpointObservers;
        _receiveObservers = configuration.ReceiveObservers;
        _transportObservers = configuration.TransportObservers;

        DependenciesReady = configuration.DependenciesReady;
        DependentsCompleted = configuration.DependentsCompleted;

        Serialization = configuration.Serialization.CreateSerializerCollection();

        _sendPipe = new Lazy<ISendPipe>(() => configuration.Send.CreatePipe());
        _publishPipe = new Lazy<IPublishPipe>(() => configuration.Publish.CreatePipe());
        _receivePipe = new Lazy<IReceivePipe>(configuration.CreateReceivePipe);

        _sendTransportProvider = new Lazy<ISendTransportProvider>(CreateSendTransportProvider);
        _publishTransportProvider = new Lazy<IPublishTransportProvider>(CreatePublishTransportProvider);

        _sendEndpointProvider = new Lazy<ISendEndpointProvider>(CreateSendEndpointProvider);
        _publishEndpointProvider = new Lazy<IPublishEndpointProvider>(CreatePublishEndpointProvider);

        hostConfiguration.ConnectReceiveEndpointContext(this);
    }

    Uri HostAddress { get; }

    /// <summary>Gets a value indicating whether bus endpoint.</summary>
    public bool IsBusEndpoint { get; }

    /// <summary>Gets the receive observers.</summary>
    public IReceiveObserver ReceiveObservers => _receiveObservers;

    /// <summary>Gets the transport observers.</summary>
    public IReceiveTransportObserver TransportObservers => _transportObservers;

    /// <summary>Gets the endpoint observers.</summary>
    public IReceiveEndpointObserver EndpointObservers => _endpointObservers;

    /// <summary>Gets the message routes.</summary>
    public IMessageRouteTable MessageRoutes => _hostConfiguration.BusConfiguration.MessageRoutes;

    /// <summary>Connects send observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        return _sendObservers.Connect(observer);
    }

    /// <summary>Connects publish observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
    {
        return _publishObservers.Connect(observer);
    }

    /// <summary>Connects receive transport observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectReceiveTransportObserver(IReceiveTransportObserver observer)
    {
        return _transportObservers.Connect(observer);
    }

    /// <summary>Connects receive observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectReceiveObserver(IReceiveObserver observer)
    {
        return _receiveObservers.Connect(observer);
    }

    /// <summary>Connects receive endpoint observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectReceiveEndpointObserver(IReceiveEndpointObserver observer)
    {
        return _endpointObservers.Connect(observer);
    }

    /// <summary>Gets the consumer stop timeout.</summary>
    public TimeSpan? ConsumerStopTimeout => _hostConfiguration.ConsumerStopTimeout;
    /// <summary>Gets the stop timeout.</summary>
    public TimeSpan? StopTimeout => _hostConfiguration.StopTimeout;

    /// <summary>Gets the input address.</summary>
    public Uri InputAddress { get; }

    /// <summary>Gets the dependencies ready.</summary>
    public Task DependenciesReady { get; }
    /// <summary>Gets the dependents completed.</summary>
    public Task DependentsCompleted { get; }

    /// <summary>Gets the publish faults.</summary>
    public bool PublishFaults { get; }

    /// <summary>Gets the prefetch count.</summary>
    public int PrefetchCount { get; }
    /// <summary>Gets the concurrent message limit.</summary>
    public int? ConcurrentMessageLimit { get; }

    /// <summary>Gets the log context.</summary>
    public ILogContext LogContext => _hostConfiguration.ReceiveLogContext ?? throw new InvalidOperationException("ReceiveLogContext should not be null");

    /// <summary>Gets the publish.</summary>
    public IPublishTopology Publish => _publishTopology;

    /// <summary>Gets the receive pipe.</summary>
    public IReceivePipe ReceivePipe => _receivePipe.Value;

    /// <summary>Gets the send endpoint provider.</summary>
    public ISendEndpointProvider SendEndpointProvider => _sendEndpointProvider.Value;

    /// <summary>Gets the publish endpoint provider.</summary>
    public IPublishEndpointProvider PublishEndpointProvider => _publishEndpointProvider.Value;

    /// <summary>Creates receive pipe dispatcher.</summary>
    /// <returns>The created receive pipe dispatcher.</returns>
    public IReceivePipeDispatcher CreateReceivePipeDispatcher()
    {
        return new ReceivePipeDispatcher(_receivePipe.Value, _receiveObservers, _hostConfiguration, InputAddress);
    }

    /// <summary>Resets the current state.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async ValueTask ResetAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); ISendEndpointProvider? sendEndpointProvider = _sendEndpointProvider.IsValueCreated ? _sendEndpointProvider.Value : null;
        IPublishEndpointProvider? publishEndpointProvider = _publishEndpointProvider.IsValueCreated ? _publishEndpointProvider.Value : null;

        if (sendEndpointProvider is not null)
            await ReleaseSendEndpointProviderAsync(sendEndpointProvider).ConfigureAwait(false);

        if (publishEndpointProvider is not null && !ReferenceEquals(publishEndpointProvider, sendEndpointProvider))
            await ReleasePublishEndpointProviderAsync(publishEndpointProvider).ConfigureAwait(false);

        _sendTransportProvider = new Lazy<ISendTransportProvider>(CreateSendTransportProvider);
        _publishTransportProvider = new Lazy<IPublishTransportProvider>(CreatePublishTransportProvider);

        _sendEndpointProvider = new Lazy<ISendEndpointProvider>(CreateSendEndpointProvider);
        _publishEndpointProvider = new Lazy<IPublishEndpointProvider>(CreatePublishEndpointProvider);
    }

    /// <summary>Releases send endpoint provider.</summary>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected virtual ValueTask ReleaseSendEndpointProviderAsync(ISendEndpointProvider provider)
    {
        return provider is IAsyncDisposable disposable ? disposable.DisposeAsync() : default;
    }

    /// <summary>Releases publish endpoint provider.</summary>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected virtual ValueTask ReleasePublishEndpointProviderAsync(IPublishEndpointProvider provider)
    {
        return provider is IAsyncDisposable disposable ? disposable.DisposeAsync() : default;
    }

    /// <summary>Adds send agent to the configuration.</summary>
    /// <param name="agent">The agent.</param>
    public abstract void AddSendAgent(IAgent agent);
    /// <summary>Adds consume agent to the configuration.</summary>
    /// <param name="agent">The agent.</param>
    public abstract void AddConsumeAgent(IAgent agent);

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public virtual void Probe(ProbeContext context)
    {
    }

    /// <summary>Converts exception.</summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="message">The message to process.</param>
    /// <returns>The converted exception.</returns>
    public abstract Exception ConvertException(Exception exception, string message);

    /// <summary>Gets the serialization.</summary>
    public ISerialization Serialization { get; }

    /// <summary>Creates send endpoint provider.</summary>
    /// <returns>The created send endpoint provider.</returns>
    protected virtual ISendEndpointProvider CreateSendEndpointProvider()
    {
        return new SendEndpointProvider(_sendTransportProvider.Value, _sendObservers, this, _sendPipe.Value);
    }

    /// <summary>Creates publish endpoint provider.</summary>
    /// <returns>The created publish endpoint provider.</returns>
    protected virtual IPublishEndpointProvider CreatePublishEndpointProvider()
    {
        return new PublishEndpointProvider(_publishTransportProvider.Value, HostAddress, _publishObservers, this, _publishPipe.Value, _publishTopology);
    }

    /// <summary>Creates send transport provider.</summary>
    /// <returns>The created send transport provider.</returns>
    protected abstract ISendTransportProvider CreateSendTransportProvider();

    /// <summary>Creates publish transport provider.</summary>
    /// <returns>The created publish transport provider.</returns>
    protected abstract IPublishTransportProvider CreatePublishTransportProvider();
}
