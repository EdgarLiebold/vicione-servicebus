using System;
using System.Collections.Generic;
using RabbitMQ.Client;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.RabbitMq.Middleware;
using ViciOne.ServiceBus.RabbitMq.Topology;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>
/// Provides a rabbit mq receive endpoint configuration implementation.
/// </summary>
public class RabbitMqReceiveEndpointConfiguration :
    ReceiveEndpointConfiguration,
    IRabbitMqReceiveEndpointConfiguration,
    IRabbitMqReceiveEndpointConfigurator
{
    readonly IBuildPipeConfigurator<ConnectionContext> _connectionConfigurator;
    readonly IRabbitMqEndpointConfiguration _endpointConfiguration;
    readonly IRabbitMqHostConfiguration _hostConfiguration;
    readonly Lazy<Uri> _inputAddress;
    readonly IBuildPipeConfigurator<ChannelContext> _channelConfigurator;
    readonly List<RabbitMqQueueRedeliveryPlan> _queueRedeliveryPlans = new();
    readonly RabbitMqReceiveSettings _settings;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="settings">The settings value.</param>
    /// <param name="endpointConfiguration">The endpoint configuration value.</param>
    public RabbitMqReceiveEndpointConfiguration(IRabbitMqHostConfiguration hostConfiguration, RabbitMqReceiveSettings settings,
        IRabbitMqEndpointConfiguration endpointConfiguration)
        : base(hostConfiguration, endpointConfiguration)
    {
        _hostConfiguration = hostConfiguration;
        _settings = settings;

        _endpointConfiguration = endpointConfiguration;

        _connectionConfigurator = new PipeConfigurator<ConnectionContext>();
        _channelConfigurator = new PipeConfigurator<ChannelContext>();

        _inputAddress = new Lazy<Uri>(FormatInputAddress);

        if (settings.QueueName == RabbitMqExchangeNames.ReplyTo)
        {
            settings.ExchangeName = "";
            settings.BindQueue = true;
            settings.NoAck = true;
        }
    }

    /// <summary>
    /// Gets the settings value.
    /// </summary>
    public ReceiveSettings Settings => _settings;

    /// <summary>
    /// Gets the host address value.
    /// </summary>
    public override Uri HostAddress => _hostConfiguration.HostAddress;
    /// <summary>
    /// Gets the input address value.
    /// </summary>
    public override Uri InputAddress => _inputAddress.Value;

    /// <summary>
    /// Creates receive endpoint context.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override ReceiveEndpointContext CreateReceiveEndpointContext()
    {
        return CreateRabbitMqReceiveEndpointContext();
    }

    IRabbitMqTopologyConfiguration IRabbitMqEndpointConfiguration.Topology => _endpointConfiguration.Topology;

    /// <summary>
    /// Performs the build operation.
    /// </summary>
    /// <param name="host">The host value.</param>
    public void Build(IHost host)
    {
        var context = CreateRabbitMqReceiveEndpointContext();

        _channelConfigurator.UseFilter(new ConfigureRabbitMqTopologyFilter<ReceiveSettings>(_settings, context.BrokerTopology));

        foreach (var plan in _queueRedeliveryPlans)
            _channelConfigurator.UseFilter(new ConfigureRabbitMqQueueRedeliveryFilter(plan));

        if (_hostConfiguration.DeployTopologyOnly)
            _channelConfigurator.UseFilter(new TransportReadyFilter<ChannelContext>(context));
        else
        {
            if (_settings.PurgeOnStartup)
                _channelConfigurator.UseFilter(new PurgeOnStartupFilter(_settings.QueueName));

            _channelConfigurator.UseFilter(new PrefetchCountFilter(_settings.PrefetchCount));
            _channelConfigurator.UseFilter(new ReceiveEndpointDependencyFilter<ChannelContext>(context));
            _channelConfigurator.UseFilter(new RabbitMqConsumerFilter(context));
        }

        IPipe<ChannelContext> channelPipe = _channelConfigurator.Build();

        var transport = new ReceiveTransport<ChannelContext>(_hostConfiguration, context, () => context.ChannelContextSupervisor, channelPipe);

        if (IsBusEndpoint && _hostConfiguration.DeployPublishTopology)
        {
            var publishTopology = _hostConfiguration.Topology.PublishTopology;

            var brokerTopology = publishTopology.GetPublishBrokerTopology();

            transport.PreStartPipe = new ConfigureRabbitMqTopologyFilter<IPublishTopology>(publishTopology, brokerTopology).ToPipe();
        }

        var receiveEndpoint = new ReceiveEndpoint(transport, context);

        var queueName = _settings.QueueName ?? NewId.Next().ToString(FormatUtil.Formatter);

        host.AddReceiveEndpoint(queueName, receiveEndpoint);

        ReceiveEndpoint = receiveEndpoint;
    }

    internal RabbitMqQueueRedeliveryPlan CreateQueueRedeliveryPlan(IEnumerable<TimeSpan> intervals)
    {
        if (_queueRedeliveryPlans.Count > 0)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("RabbitMQ", "unknown", "RabbitMQ queue redelivery may only be configured once per receive endpoint.", "Correct the named configuration before starting the host"));

        var plan = new RabbitMqQueueRedeliveryPlan(_settings, intervals);
        _queueRedeliveryPlans.Add(plan);
        Changed("QueueRedelivery");
        return plan;
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override IEnumerable<ValidationResult> Validate()
    {
        var queueName = $"{_settings.QueueName}";

        if (!RabbitMqEntityNameValidator.Validator.IsValidEntityName(_settings.QueueName))
            yield return this.Failure(queueName, "must be a valid queue name");

        if (_settings.PurgeOnStartup)
            yield return this.Warning(queueName, "Existing messages in the queue will be purged on service start");

        foreach (var result in base.Validate())
            yield return result.WithParentKey(queueName);
    }

    /// <summary>
    /// Gets or sets the durable value.
    /// </summary>
    public bool Durable
    {
        set
        {
            _settings.Durable = value;

            Changed("Durable");
        }
    }

    /// <summary>
    /// Gets or sets the exclusive value.
    /// </summary>
    public bool Exclusive
    {
        set
        {
            _settings.Exclusive = value;

            Changed("Exclusive");
        }
    }

    /// <summary>
    /// Gets or sets the auto delete value.
    /// </summary>
    public bool AutoDelete
    {
        set
        {
            _settings.AutoDelete = value;

            Changed("AutoDelete");
        }
    }

    /// <summary>
    /// Gets or sets the exchange type value.
    /// </summary>
    public string ExchangeType
    {
        set => _settings.ExchangeType = value;
    }

    /// <summary>
    /// Gets or sets the purge on startup value.
    /// </summary>
    public bool PurgeOnStartup
    {
        set => _settings.PurgeOnStartup = value;
    }

    /// <summary>
    /// Gets or sets the consumer priority value.
    /// </summary>
    public int ConsumerPriority
    {
        set => _settings.ConsumerPriority = value;
    }

    /// <summary>
    /// Gets or sets the exclusive consumer value.
    /// </summary>
    public bool ExclusiveConsumer
    {
        set => _settings.ExclusiveConsumer = value;
    }

    /// <summary>
    /// Performs the stream operation.
    /// </summary>
    /// <param name="callback">The callback value.</param>
    public void Stream(Action<IRabbitMqStreamConfigurator>? callback = null)
    {
        _settings.QueueArguments[RabbitMQ.Client.Headers.XQueueType] = "stream";

        var configurator = new RabbitMqStreamConfigurator(_settings);

        callback?.Invoke(configurator);
    }

    /// <summary>
    /// Performs the stream operation.
    /// </summary>
    /// <param name="consumerTag">The consumer tag value.</param>
    /// <param name="callback">The callback value.</param>
    public void Stream(string consumerTag, Action<IRabbitMqStreamConfigurator>? callback = null)
    {
        if (string.IsNullOrWhiteSpace(consumerTag))
            throw new ArgumentNullException(nameof(consumerTag));

        _settings.ConsumerTag = consumerTag;

        Stream(callback);
    }

    /// <summary>
    /// Gets or sets the lazy value.
    /// </summary>
    public bool Lazy
    {
        set => _settings.Lazy = value;
    }

    /// <summary>
    /// Gets or sets the bind queue value.
    /// </summary>
    public bool BindQueue
    {
        set => _settings.BindQueue = value;
    }

    /// <summary>
    /// Gets or sets the queue expiration value.
    /// </summary>
    public TimeSpan? QueueExpiration
    {
        set => _settings.QueueExpiration = value;
    }

    /// <summary>
    /// Gets or sets the single active consumer value.
    /// </summary>
    public bool SingleActiveConsumer
    {
        set => _settings.SingleActiveConsumer = value;
    }

    /// <summary>
    /// Gets or sets the dead letter exchange value.
    /// </summary>
    public string DeadLetterExchange
    {
        set => SetQueueArgument(RabbitMQ.Client.Headers.XDeadLetterExchange, value);
    }

    /// <summary>
    /// Sets queue argument.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="value">The value.</param>
    public void SetQueueArgument(string key, object? value)
    {
        _settings.SetQueueArgument(key, value);
    }

    /// <summary>
    /// Sets queue argument.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="value">The value.</param>
    public void SetQueueArgument(string key, TimeSpan value)
    {
        _settings.SetQueueArgument(key, value);
    }

    /// <summary>
    /// Sets exchange argument.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="value">The value.</param>
    public void SetExchangeArgument(string key, object? value)
    {
        _settings.SetExchangeArgument(key, value);
    }

    /// <summary>
    /// Sets exchange argument.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="value">The value.</param>
    public void SetExchangeArgument(string key, TimeSpan value)
    {
        _settings.SetExchangeArgument(key, value);
    }

    /// <summary>
    /// Performs the enable priority operation.
    /// </summary>
    /// <param name="maxPriority">The max priority value.</param>
    public void EnablePriority(byte maxPriority)
    {
        _settings.EnablePriority(maxPriority);
    }

    /// <summary>
    /// Sets quorum queue.
    /// </summary>
    /// <param name="replicationFactor">The replication factor value.</param>
    public void SetQuorumQueue(int? replicationFactor = default)
    {
        _settings.SetQuorumQueue(replicationFactor);
    }

    /// <summary>
    /// Sets delivery acknowledgement timeout.
    /// </summary>
    /// <param name="timeSpan">The time span value.</param>
    public void SetDeliveryAcknowledgementTimeout(TimeSpan timeSpan)
    {
        if (timeSpan <= TimeSpan.Zero)
            throw new ArgumentException("The RabbitMQ consumer timeout must be > 0");

        SetQueueArgument("x-consumer-timeout", (long)timeSpan.TotalMilliseconds);
    }

    /// <summary>
    /// Sets delivery acknowledgement timeout.
    /// </summary>
    /// <param name="d">The d value.</param>
    /// <param name="h">The h value.</param>
    /// <param name="m">The m value.</param>
    /// <param name="s">The s value.</param>
    /// <param name="ms">The ms value.</param>
    public void SetDeliveryAcknowledgementTimeout(int? d = null, int? h = null, int? m = null, int? s = null, int? ms = null)
    {
        var value = new TimeSpan(d ?? 0, h ?? 0, m ?? 0, s ?? 0, ms ?? 0);

        SetDeliveryAcknowledgementTimeout(value);
    }

    /// <summary>
    /// Performs the bind operation.
    /// </summary>
    /// <param name="exchangeName">The exchange name value.</param>
    /// <param name="callback">The callback value.</param>
    public void Bind(string exchangeName, Action<IRabbitMqExchangeToExchangeBindingConfigurator>? callback)
    {
        if (exchangeName == null)
            throw new ArgumentNullException(nameof(exchangeName));

        _endpointConfiguration.Topology.Consume.Bind(exchangeName, callback);
    }

    /// <summary>
    /// Performs the bind operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="callback">The callback value.</param>
    public void Bind<T>(Action<IRabbitMqExchangeBindingConfigurator>? callback)
        where T : class
    {
        _endpointConfiguration.Topology.Consume.GetMessageTopology<T>().Bind(callback);
    }

    /// <summary>
    /// Performs the bind dead letter queue operation.
    /// </summary>
    /// <param name="exchangeName">The exchange name value.</param>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="configure">The configuration callback.</param>
    public void BindDeadLetterQueue(string exchangeName, string? queueName, Action<IRabbitMqQueueBindingConfigurator>? configure)
    {
        _endpointConfiguration.Topology.Consume.BindQueue(exchangeName, queueName ?? exchangeName, configure);

        DeadLetterExchange = exchangeName;
    }

    /// <summary>
    /// Configures channel.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    public void ConfigureChannel(Action<IPipeConfigurator<ChannelContext>> configure)
    {
        configure?.Invoke(_channelConfigurator);
    }

    /// <summary>
    /// Configures connection.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    public void ConfigureConnection(Action<IPipeConfigurator<ConnectionContext>> configure)
    {
        configure?.Invoke(_connectionConfigurator);
    }

    /// <summary>
    /// Performs the override consumer tag operation.
    /// </summary>
    /// <param name="consumerTag">The consumer tag value.</param>
    public void OverrideConsumerTag(string consumerTag)
    {
        _settings.ConsumerTag = consumerTag;
    }

    RabbitMqReceiveEndpointContext CreateRabbitMqReceiveEndpointContext()
    {
        var builder = new RabbitMqReceiveEndpointBuilder(_hostConfiguration, this);

        ApplySpecifications(builder);

        return builder.CreateReceiveEndpointContext();
    }

    Uri FormatInputAddress()
    {
        return _settings.GetInputAddress(_hostConfiguration.HostAddress);
    }

    /// <summary>
    /// Determines whether already configured.
    /// </summary>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    protected override bool IsAlreadyConfigured()
    {
        return _inputAddress.IsValueCreated || base.IsAlreadyConfigured();
    }
}
