using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Initializers;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Runtime;

internal sealed partial class ServiceBusRuntime :
    IBusControl,
    Advanced.IAdvancedPublishEndpoint,
    IMessageRouteProvider
{
    /// <summary>Bounds startup when the caller supplies no cancellation token.</summary>
    static readonly TimeSpan DefaultReadinessTimeout = TimeSpan.FromSeconds(60);

    /// <summary>Bounds host cleanup after bus startup fails or is canceled.</summary>
    static readonly TimeSpan StartupCleanupTimeout = TimeSpan.FromSeconds(30);

    readonly IBusObserver _busObservable;
    readonly IConsumePipe _consumePipe;
    readonly IHost _host;
    readonly ILogContext _logContext;
    readonly SemaphoreSlim _lifecycleGate = new(1, 1);
    readonly IPublishEndpoint _publishEndpoint;
    readonly IReceiveEndpoint _receiveEndpoint;
    readonly TimeProvider _timeProvider;
    BusLifecycleHandle? _busHandle;

    BusState _busState;
    string _healthMessage = "not started";

    public ServiceBusRuntime(IHost host, IBusObserver busObservable, IReceiveEndpointConfiguration endpointConfiguration,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(endpointConfiguration);

        _host = host ?? throw new ArgumentNullException(nameof(host));
        _busObservable = busObservable ?? throw new ArgumentNullException(nameof(busObservable));
        Address = endpointConfiguration.InputAddress;
        _consumePipe = endpointConfiguration.ConsumePipe;
        _receiveEndpoint = endpointConfiguration.ReceiveEndpoint;
        _timeProvider = timeProvider ?? TimeProvider.System;

        _busState = BusState.Created;

        Topology = _host.Topology;

        if (LogContext.Current == null)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Service Bus Runtime", "unknown", "The LogContext was not set.", "Correct the named configuration before starting the host"));

        _logContext = LogContext.Current;

        _publishEndpoint = new PublishEndpoint(_receiveEndpoint);
    }

    ConnectHandle IConsumePipeConnector.ConnectConsumePipe<TMessage>(IPipe<ConsumeContext<TMessage>> pipe)
    {
        LogContext.SetCurrentIfNull(_logContext);

        return _consumePipe.ConnectConsumePipe(pipe);
    }

    ConnectHandle IConsumePipeConnector.ConnectConsumePipe<TMessage>(IPipe<ConsumeContext<TMessage>> pipe, ConnectPipeOptions options)
    {
        LogContext.SetCurrentIfNull(_logContext);

        return _consumePipe.ConnectConsumePipe(pipe, options);
    }

    ConnectHandle IRequestPipeConnector.ConnectRequestPipe<TMessage>(Guid requestId, IPipe<ConsumeContext<TMessage>> pipe)
    {
        LogContext.SetCurrentIfNull(_logContext);

        return _consumePipe.ConnectRequestPipe(requestId, pipe);
    }

    Task IPublishEndpoint.PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken)
    {
        LogContext.SetCurrentIfNull(_logContext);

        return _publishEndpoint.PublishAsync(message, cancellationToken);
    }

    Task Advanced.IAdvancedPublishEndpoint.PublishAsync<TMessage>(TMessage message, IPipe<PublishContext<TMessage>> publishPipe,
        CancellationToken cancellationToken)
    {
        LogContext.SetCurrentIfNull(_logContext);

        return _publishEndpoint.PublishAsync(message, publishPipe, cancellationToken);
    }

    Task Advanced.IAdvancedPublishEndpoint.PublishAsync<TMessage>(TMessage message, IPipe<PublishContext> publishPipe,
        CancellationToken cancellationToken)
    {
        LogContext.SetCurrentIfNull(_logContext);

        return _publishEndpoint.PublishAsync(message, publishPipe, cancellationToken);
    }

    Task Advanced.IAdvancedPublishEndpoint.PublishAsync(object message, CancellationToken cancellationToken)
    {
        LogContext.SetCurrentIfNull(_logContext);

        ArgumentNullException.ThrowIfNull(message);
        return _publishEndpoint.PublishAsync(message, message.GetType(), cancellationToken);
    }

    Task Advanced.IAdvancedPublishEndpoint.PublishAsync(object message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken)
    {
        LogContext.SetCurrentIfNull(_logContext);

        ArgumentNullException.ThrowIfNull(message);
        return _publishEndpoint.PublishAsync(message, message.GetType(), publishPipe, cancellationToken);
    }

    Task Advanced.IAdvancedPublishEndpoint.PublishAsync(object message, Type messageType, CancellationToken cancellationToken)
    {
        LogContext.SetCurrentIfNull(_logContext);

        return _publishEndpoint.PublishAsync(message, messageType, cancellationToken);
    }

    Task Advanced.IAdvancedPublishEndpoint.PublishAsync(object message, Type messageType, IPipe<PublishContext> publishPipe,
        CancellationToken cancellationToken)
    {
        LogContext.SetCurrentIfNull(_logContext);

        return _publishEndpoint.PublishAsync(message, messageType, publishPipe, cancellationToken);
    }

    Task Advanced.IAdvancedPublishEndpoint.PublishAsync<TMessage>(object values, CancellationToken cancellationToken)
    {
        LogContext.SetCurrentIfNull(_logContext);

        return _publishEndpoint.PublishAsync<TMessage>(values, cancellationToken);
    }

    Task Advanced.IAdvancedPublishEndpoint.PublishAsync<TMessage>(object values, IPipe<PublishContext<TMessage>> publishPipe,
        CancellationToken cancellationToken)
    {
        LogContext.SetCurrentIfNull(_logContext);

        return _publishEndpoint.PublishAsync(values, publishPipe, cancellationToken);
    }

    Task Advanced.IAdvancedPublishEndpoint.PublishAsync<TMessage>(object values, IPipe<PublishContext> publishPipe,
        CancellationToken cancellationToken)
    {
        LogContext.SetCurrentIfNull(_logContext);

        return _publishEndpoint.PublishAsync<TMessage>(values, publishPipe, cancellationToken);
    }

    public Uri Address { get; }

    IMessageRouteTable IMessageRouteProvider.MessageRoutes => EndpointConvention.GetMessageRoutes(_receiveEndpoint);

    public IBusTopology Topology { get; }

    Task<ISendEndpoint> ISendEndpointProvider.GetSendEndpointAsync(Uri address, CancellationToken cancellationToken)
    {
        LogContext.SetCurrentIfNull(_logContext);

        return _receiveEndpoint.GetSendEndpointAsync(address, cancellationToken: cancellationToken);
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

    public Task<ISendEndpoint> GetPublishSendEndpointAsync<TMessage>(CancellationToken cancellationToken = default)
        where TMessage : class
    {
        LogContext.SetCurrentIfNull(_logContext);

        return _receiveEndpoint.GetPublishSendEndpointAsync<TMessage>(cancellationToken: cancellationToken);
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

    IHostReceiveEndpointHandle IReceiveConnector.ConnectReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
        Action<IReceiveEndpointConfigurator>? configureEndpoint)
    {
        return _host.ConnectReceiveEndpoint(definition, endpointNameFormatter, configureEndpoint);
    }

    IHostReceiveEndpointHandle IReceiveConnector.ConnectReceiveEndpoint(string queueName, Action<IReceiveEndpointConfigurator>? configureEndpoint)
    {
        return _host.ConnectReceiveEndpoint(queueName, configureEndpoint);
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var scope = context.CreateScope("bus");
        scope.Add("address", Address);

        _host.Probe(scope);
    }
}
