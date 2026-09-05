using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Observables;

#nullable enable
namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Provides a base receive endpoint context implementation.
/// </summary>
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

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="configuration">The configuration callback.</param>
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

    /// <summary>
    /// Gets the is bus endpoint value.
    /// </summary>
    public bool IsBusEndpoint { get; }

    /// <summary>
    /// Gets the receive observers value.
    /// </summary>
    public IReceiveObserver ReceiveObservers => _receiveObservers;

    /// <summary>
    /// Gets the transport observers value.
    /// </summary>
    public IReceiveTransportObserver TransportObservers => _transportObservers;

    /// <summary>
    /// Gets the endpoint observers value.
    /// </summary>
    public IReceiveEndpointObserver EndpointObservers => _endpointObservers;

    /// <summary>
    /// Gets the message routes value.
    /// </summary>
    public IMessageRouteTable MessageRoutes => _hostConfiguration.BusConfiguration.MessageRoutes;

    /// <summary>
    /// Connects send observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        return _sendObservers.Connect(observer);
    }

    /// <summary>
    /// Connects publish observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
    {
        return _publishObservers.Connect(observer);
    }

    /// <summary>
    /// Connects receive transport observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectReceiveTransportObserver(IReceiveTransportObserver observer)
    {
        return _transportObservers.Connect(observer);
    }

    /// <summary>
    /// Connects receive observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectReceiveObserver(IReceiveObserver observer)
    {
        return _receiveObservers.Connect(observer);
    }

    /// <summary>
    /// Connects receive endpoint observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectReceiveEndpointObserver(IReceiveEndpointObserver observer)
    {
        return _endpointObservers.Connect(observer);
    }

    /// <summary>
    /// Gets the consumer stop timeout value.
    /// </summary>
    public TimeSpan? ConsumerStopTimeout => _hostConfiguration.ConsumerStopTimeout;
    /// <summary>
    /// Gets the stop timeout value.
    /// </summary>
    public TimeSpan? StopTimeout => _hostConfiguration.StopTimeout;

    /// <summary>
    /// Gets the input address value.
    /// </summary>
    public Uri InputAddress { get; }

    /// <summary>
    /// Gets the dependencies ready value.
    /// </summary>
    public Task DependenciesReady { get; }
    /// <summary>
    /// Gets the dependents completed value.
    /// </summary>
    public Task DependentsCompleted { get; }

    /// <summary>
    /// Gets the publish faults value.
    /// </summary>
    public bool PublishFaults { get; }

    /// <summary>
    /// Gets the prefetch count value.
    /// </summary>
    public int PrefetchCount { get; }
    /// <summary>
    /// Gets the concurrent message limit value.
    /// </summary>
    public int? ConcurrentMessageLimit { get; }

    /// <summary>
    /// Gets the log context value.
    /// </summary>
    public ILogContext LogContext => _hostConfiguration.ReceiveLogContext ?? throw new InvalidOperationException("ReceiveLogContext should not be null");

    /// <summary>
    /// Gets the publish value.
    /// </summary>
    public IPublishTopology Publish => _publishTopology;

    /// <summary>
    /// Gets the receive pipe value.
    /// </summary>
    public IReceivePipe ReceivePipe => _receivePipe.Value;

    /// <summary>
    /// Gets the send endpoint provider value.
    /// </summary>
    public ISendEndpointProvider SendEndpointProvider => _sendEndpointProvider.Value;

    /// <summary>
    /// Gets the publish endpoint provider value.
    /// </summary>
    public IPublishEndpointProvider PublishEndpointProvider => _publishEndpointProvider.Value;

    /// <summary>
    /// Creates receive pipe dispatcher.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IReceivePipeDispatcher CreateReceivePipeDispatcher()
    {
        return new ReceivePipeDispatcher(_receivePipe.Value, _receiveObservers, _hostConfiguration, InputAddress);
    }

    /// <summary>
    /// Performs the reset operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Performs the release send endpoint provider operation.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    /// <returns>The result of the operation.</returns>
    protected virtual ValueTask ReleaseSendEndpointProviderAsync(ISendEndpointProvider provider)
    {
        return provider is IAsyncDisposable disposable ? disposable.DisposeAsync() : default;
    }

    /// <summary>
    /// Performs the release publish endpoint provider operation.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    /// <returns>The result of the operation.</returns>
    protected virtual ValueTask ReleasePublishEndpointProviderAsync(IPublishEndpointProvider provider)
    {
        return provider is IAsyncDisposable disposable ? disposable.DisposeAsync() : default;
    }

    /// <summary>
    /// Adds send agent to the configuration.
    /// </summary>
    /// <param name="agent">The agent value.</param>
    public abstract void AddSendAgent(IAgent agent);
    /// <summary>
    /// Adds consume agent to the configuration.
    /// </summary>
    /// <param name="agent">The agent value.</param>
    public abstract void AddConsumeAgent(IAgent agent);

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public virtual void Probe(ProbeContext context)
    {
    }

    /// <summary>
    /// Performs the convert exception operation.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="message">The message value.</param>
    /// <returns>The result of the operation.</returns>
    public abstract Exception ConvertException(Exception exception, string message);

    /// <summary>
    /// Gets the serialization value.
    /// </summary>
    public ISerialization Serialization { get; }

    /// <summary>
    /// Creates send endpoint provider.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    protected virtual ISendEndpointProvider CreateSendEndpointProvider()
    {
        return new SendEndpointProvider(_sendTransportProvider.Value, _sendObservers, this, _sendPipe.Value);
    }

    /// <summary>
    /// Creates publish endpoint provider.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    protected virtual IPublishEndpointProvider CreatePublishEndpointProvider()
    {
        return new PublishEndpointProvider(_publishTransportProvider.Value, HostAddress, _publishObservers, this, _publishPipe.Value, _publishTopology);
    }

    /// <summary>
    /// Creates send transport provider.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    protected abstract ISendTransportProvider CreateSendTransportProvider();

    /// <summary>
    /// Creates publish transport provider.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    protected abstract IPublishTransportProvider CreatePublishTransportProvider();
}
