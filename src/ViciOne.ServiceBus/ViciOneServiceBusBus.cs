using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Initializers;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Events;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus;

internal sealed partial class ViciOneServiceBusBus :
    IBusControl,
    Advanced.IAdvancedPublishEndpoint,
    IMessageRouteProvider
{
    /// <summary>
    /// Defines the default bound for bus startup and for consumers waiting on the on-demand bus endpoint.
    /// </summary>
    static readonly TimeSpan ReadyTimeout = TimeSpan.FromSeconds(60);

    readonly IBusObserver _busObservable;
    readonly IConsumePipe _consumePipe;
    readonly IHost _host;
    readonly ILogContext _logContext;
    readonly SemaphoreSlim _lifecycleGate = new(1, 1);
    readonly IPublishEndpoint _publishEndpoint;
    readonly IReceiveEndpoint _receiveEndpoint;
    readonly TimeProvider _timeProvider;
    BusLifecycleHandle? _busHandle;

    /// <summary>The terminal bus-endpoint failure that no subsequent retry can resolve.</summary>
    TerminalFaultObserver? _terminalFault;
    ConnectHandle? _terminalFaultHandle;

    BusState _busState;
    string _healthMessage = "not started";

    public ViciOneServiceBusBus(IHost host, IBusObserver busObservable, IReceiveEndpointConfiguration endpointConfiguration,
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
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Vici One Service Bus Bus", "unknown", "The LogContext was not set.", "Correct the named configuration before starting the host"));

        _logContext = LogContext.Current;

        _publishEndpoint = new PublishEndpoint(_receiveEndpoint);
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

    Task IPublishEndpoint.PublishAsync<T>(T message, CancellationToken cancellationToken)
    {
        LogContext.SetCurrentIfNull(_logContext);

        return _publishEndpoint.PublishAsync(message, cancellationToken);
    }

    Task Advanced.IAdvancedPublishEndpoint.PublishAsync<T>(T message, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken)
    {
        LogContext.SetCurrentIfNull(_logContext);

        return _publishEndpoint.PublishAsync(message, publishPipe, cancellationToken);
    }

    Task Advanced.IAdvancedPublishEndpoint.PublishAsync<T>(T message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken)
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

    Task Advanced.IAdvancedPublishEndpoint.PublishAsync<T>(object values, CancellationToken cancellationToken)
    {
        LogContext.SetCurrentIfNull(_logContext);

        return _publishEndpoint.PublishAsync<T>(values, cancellationToken);
    }

    Task Advanced.IAdvancedPublishEndpoint.PublishAsync<T>(object values, IPipe<PublishContext<T>> publishPipe,
        CancellationToken cancellationToken)
    {
        LogContext.SetCurrentIfNull(_logContext);

        return _publishEndpoint.PublishAsync(values, publishPipe, cancellationToken);
    }

    Task Advanced.IAdvancedPublishEndpoint.PublishAsync<T>(object values, IPipe<PublishContext> publishPipe,
        CancellationToken cancellationToken)
    {
        LogContext.SetCurrentIfNull(_logContext);

        return _publishEndpoint.PublishAsync<T>(values, publishPipe, cancellationToken);
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

    public Task<ISendEndpoint> GetPublishSendEndpointAsync<T>(CancellationToken cancellationToken = default)
        where T : class
    {
        LogContext.SetCurrentIfNull(_logContext);

        return _receiveEndpoint.GetPublishSendEndpointAsync<T>(cancellationToken: cancellationToken);
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
}
