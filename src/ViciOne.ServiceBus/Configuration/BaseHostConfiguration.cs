using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Logging.Internal;
using ViciOne.ServiceBus.Logging.Monitoring;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Stores and validates base host configuration.</summary>
/// <typeparam name="TConfiguration">The configuration type.</typeparam>
/// <typeparam name="TConfigurator">The configurator type.</typeparam>
public abstract class BaseHostConfiguration<TConfiguration, TConfigurator> :
    IHostConfiguration,
    IMessageLimitsHostConfiguration,
    IPayloadAdmissionHostConfiguration,
    IReceiveConfigurator<TConfigurator>
    where TConfiguration : IReceiveEndpointConfiguration
    where TConfigurator : IReceiveEndpointConfigurator
{
    readonly ConsumeObservable _consumeObservers;
    readonly EndpointConfigurationObservable _endpointObservable;
    readonly PublishObservable _publishObservers;
    readonly ReceiveObservable _receiveObservers;
    readonly SendObservable _sendObservers;
    List<TConfiguration> _endpoints;
    ILogContext? _logContext;
    MessageLimits? _messageLimits;
    IPayloadAdmissionRuntime? _payloadAdmissionRuntime;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="busConfiguration">The bus configuration.</param>
    protected BaseHostConfiguration(IBusConfiguration busConfiguration)
    {
        BusConfiguration = busConfiguration;
        _endpoints = [];

        _endpointObservable = new EndpointConfigurationObservable();

        _receiveObservers = new ReceiveObservable();
        _consumeObservers = new ConsumeObservable();
        _publishObservers = new PublishObservable();
        _sendObservers = new SendObservable();
    }

    /// <summary>Gets the observers.</summary>
    protected IEndpointConfigurationObserver Observers => _endpointObservable;

    /// <summary>Gets the bus configuration.</summary>
    public IBusConfiguration BusConfiguration { get; }

    /// <summary>Gets the host address.</summary>
    public abstract Uri HostAddress { get; }

    /// <summary>Gets or sets the deploy topology only.</summary>
    public bool DeployTopologyOnly { get; set; }
    /// <summary>Gets or sets the deploy publish topology.</summary>
    public bool DeployPublishTopology { get; set; }

    /// <summary>Gets the send observers.</summary>
    public ISendObserver SendObservers => _sendObservers;

    /// <summary>Gets or sets the log context.</summary>
    public ILogContext? LogContext
    {
        get => _logContext;
        set
        {
            _logContext = value;

            SendLogContext = value?.CreateLogContext(ServiceBusLogCategories.SendTransport);
            ReceiveLogContext = value?.CreateLogContext(ServiceBusLogCategories.ReceiveTransport);

            LogContextMetricsExtensions.CopyMetrics(value, SendLogContext);
            LogContextMetricsExtensions.CopyMetrics(value, ReceiveLogContext);
        }
    }

    /// <summary>Gets or sets the receive log context.</summary>
    public ILogContext? ReceiveLogContext { get; private set; }
    /// <summary>Gets or sets the send log context.</summary>
    public ILogContext? SendLogContext { get; private set; }

    /// <summary>Connects endpoint configuration observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectEndpointConfigurationObserver(IEndpointConfigurationObserver observer)
    {
        return _endpointObservable.Connect(observer);
    }

    /// <summary>Connects receive endpoint context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectReceiveEndpointContext(ReceiveEndpointContext context)
    {
        var consume = context.ReceivePipe.ConnectConsumeObserver(_consumeObservers);
        var receive = context.ConnectReceiveObserver(_receiveObservers);
        var publish = context.ConnectPublishObserver(_publishObservers);
        var send = context.ConnectSendObserver(_sendObservers);

        return new MultipleConnectHandle(consume, receive, publish, send);
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public virtual IEnumerable<ValidationResult> Validate()
    {
        return _endpoints.SelectMany(x => x.Validate());
    }

    /// <summary>Gets the topology.</summary>
    public abstract IBusTopology Topology { get; }

    /// <summary>Gets the receive transport retry policy.</summary>
    public abstract IRetryPolicy ReceiveTransportRetryPolicy { get; }
    /// <summary>Gets the send transport retry policy.</summary>
    public virtual IRetryPolicy SendTransportRetryPolicy => ReceiveTransportRetryPolicy;
    /// <summary>Gets or sets the consumer stop timeout.</summary>
    public TimeSpan? ConsumerStopTimeout { get; set; }
    /// <summary>Gets or sets the stop timeout.</summary>
    public TimeSpan? StopTimeout { get; set; }

    MessageLimits? IMessageLimitsHostConfiguration.MessageLimits => Volatile.Read(ref _messageLimits);

    void IMessageLimitsHostConfiguration.SetMessageLimits(MessageLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);
        MessageLimits? existing = Interlocked.CompareExchange(ref _messageLimits, limits, null);
        if (existing is not null && existing != limits)
            throw new ConfigurationException("Message limits for bus 'unknown': Limits is already assigned. Configure exactly one limits owner per bus.");
    }

    IPayloadAdmissionRuntime? IPayloadAdmissionHostConfiguration.PayloadAdmissionRuntime
        => Volatile.Read(ref _payloadAdmissionRuntime);

    void IPayloadAdmissionHostConfiguration.SetPayloadAdmissionRuntime(IPayloadAdmissionRuntime runtime)
    {
        if (runtime == null)
            throw new ArgumentNullException(nameof(runtime));

        IPayloadAdmissionRuntime? existing = Interlocked.CompareExchange(ref _payloadAdmissionRuntime, runtime, null);
        if (existing != null && !ReferenceEquals(existing, runtime))
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Base Host", "unknown", "Payload admission is already configured for this bus owner.", "Correct the named configuration before starting the host"));

    }

    /// <summary>Creates receive endpoint configuration.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The created receive endpoint configuration.</returns>
    public abstract IReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(string queueName, Action<IReceiveEndpointConfigurator>? configure);

    /// <summary>Builds the configured component.</summary>
    /// <returns>The configured component.</returns>
    public abstract IHost Build();

    /// <summary>Connects receive observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectReceiveObserver(IReceiveObserver observer)
    {
        return _receiveObservers.Connect(observer);
    }

    /// <summary>Connects consume observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumeObserver(IConsumeObserver observer)
    {
        return _consumeObservers.Connect(observer);
    }

    /// <summary>Connects publish observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
    {
        return _publishObservers.Connect(observer);
    }

    /// <summary>Connects send observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        return _sendObservers.Connect(observer);
    }

    /// <summary>Applies the receive-endpoint configuration.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="configureEndpoint">The configure endpoint.</param>
    public void ReceiveEndpoint(string queueName, Action<IReceiveEndpointConfigurator> configureEndpoint)
    {
        ReceiveEndpoint(queueName, (TConfigurator configuration) => configureEndpoint(configuration));
    }

    /// <summary>Applies the receive-endpoint configuration.</summary>
    /// <param name="definition">The definition.</param>
    /// <param name="endpointNameFormatter">The endpoint name formatter.</param>
    /// <param name="configureEndpoint">The configure endpoint.</param>
    public void ReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
        Action<IReceiveEndpointConfigurator>? configureEndpoint)
    {
        ReceiveEndpoint(definition, endpointNameFormatter, (TConfigurator configuration) => configureEndpoint?.Invoke(configuration));
    }

    /// <summary>Applies the receive-endpoint configuration.</summary>
    /// <param name="definition">The definition.</param>
    /// <param name="endpointNameFormatter">The endpoint name formatter.</param>
    /// <param name="configureEndpoint">The configure endpoint.</param>
    public abstract void ReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
        Action<TConfigurator>? configureEndpoint = null);

    /// <summary>Applies the receive-endpoint configuration.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="configureEndpoint">The configure endpoint.</param>
    public abstract void ReceiveEndpoint(string queueName, Action<TConfigurator> configureEndpoint);

    /// <summary>Applies endpoint definition.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="definition">The definition.</param>
    protected void ApplyEndpointDefinition(IReceiveEndpointConfigurator configurator, IEndpointDefinition definition)
    {
        configurator.ConfigureConsumeTopology = definition.ConfigureConsumeTopology;

        configurator.ConcurrentMessageLimit = definition.ConcurrentMessageLimit;

        if (definition.PrefetchCount.HasValue)
            configurator.PrefetchCount = (ushort)definition.PrefetchCount.Value;
        else if (definition.ConcurrentMessageLimit.HasValue)
        {
            var concurrentMessageLimit = definition.ConcurrentMessageLimit.Value;

            var calculatedPrefetchCount = concurrentMessageLimit * 12 / 10;

            configurator.PrefetchCount = (ushort)calculatedPrefetchCount;
        }

        definition.Configure(configurator);
    }

    /// <summary>Gets configured endpoints.</summary>
    /// <returns>The configured endpoints.</returns>
    protected IEnumerable<TConfiguration> GetConfiguredEndpoints()
    {
        IList<TConfiguration> endpoints = _endpoints;

        _endpoints = new List<TConfiguration>();

        return endpoints;
    }

    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <param name="configuration">The callback used to configure the component.</param>
    protected void Add(TConfiguration configuration)
    {
        _endpoints.Add(configuration);
    }
}
