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

/// <summary>Stores RabbitMQ receive, topology, channel, and connection pipeline configuration.</summary>
public class RabbitMqReceiveEndpointConfiguration :
    ReceiveEndpointConfiguration,
    IRabbitMqReceiveEndpointConfiguration,
    IRabbitMqReceiveEndpointConfigurator
{
    readonly PipeConfigurator<ConnectionContext> _connectionConfigurator;
    readonly IRabbitMqEndpointConfiguration _endpointConfiguration;
    readonly IRabbitMqHostConfiguration _hostConfiguration;
    Uri? _builtInputAddress;
    volatile bool _inputAddressRead;
    readonly IBuildPipeConfigurator<ChannelContext> _channelConfigurator;
    readonly List<RabbitMqQueueRedeliveryPlan> _queueRedeliveryPlans = new();
    readonly RabbitMqReceiveSettings _settings;

    /// <summary>Creates a receive-endpoint configuration from host, queue, and provider-neutral settings.</summary>
    /// <param name="hostConfiguration">The owning RabbitMQ host configuration.</param>
    /// <param name="settings">The RabbitMQ receive and queue settings.</param>
    /// <param name="endpointConfiguration">The provider-neutral endpoint configuration.</param>
    public RabbitMqReceiveEndpointConfiguration(IRabbitMqHostConfiguration hostConfiguration, RabbitMqReceiveSettings settings,
        IRabbitMqEndpointConfiguration endpointConfiguration)
        : base(hostConfiguration, endpointConfiguration)
    {
        _hostConfiguration = hostConfiguration;
        _settings = settings;

        _endpointConfiguration = endpointConfiguration;

        _connectionConfigurator = new PipeConfigurator<ConnectionContext>();
        _channelConfigurator = new PipeConfigurator<ChannelContext>();

        if (settings.QueueName == RabbitMqExchangeNames.ReplyTo)
        {
            settings.ExchangeName = "";
            settings.BindQueue = true;
            settings.NoAck = true;

            // Direct reply-to is an at-most-once pseudo-queue without broker-side error or skipped queues.
            this.DiscardFaultedMessages();
            this.DiscardSkippedMessages();
        }
    }

    /// <summary>Gets the effective RabbitMQ receive settings.</summary>
    public ReceiveSettings Settings => _settings;

    /// <summary>Gets the RabbitMQ host and virtual-host address.</summary>
    public override Uri HostAddress => _hostConfiguration.HostAddress;
    /// <summary>Gets the normalized receive endpoint address.</summary>
    public override Uri InputAddress
    {
        get
        {
            if (_builtInputAddress is { } builtInputAddress)
                return builtInputAddress;

            Uri address = FormatInputAddress();
            _inputAddressRead = true;
            return address;
        }
    }

    /// <summary>Creates the runtime context and broker topology for this endpoint.</summary>
    /// <returns>The RabbitMQ receive-endpoint context.</returns>
    public override ReceiveEndpointContext CreateReceiveEndpointContext()
    {
        return CreateRabbitMqReceiveEndpointContext();
    }

    IRabbitMqTopologyConfiguration IRabbitMqEndpointConfiguration.Topology => _endpointConfiguration.Topology;

    /// <summary>Builds the channel pipeline and attaches the receive endpoint to a host.</summary>
    /// <param name="host">The host that owns the receive endpoint.</param>
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
        _builtInputAddress = context.InputAddress;
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

    /// <summary>Validates the queue name, prefetch limit, configured channel and connection pipelines, destructive startup purge, and base endpoint configuration.</summary>
    /// <returns>All RabbitMQ receive-endpoint validation results.</returns>
    public override IEnumerable<ValidationResult> Validate()
    {
        var queueName = $"{_settings.QueueName}";

        if (!RabbitMqEntityNameValidator.Validator.IsValidEntityName(_settings.QueueName))
            yield return this.Failure(queueName, "must be a valid queue name");

        if (_settings.PurgeOnStartup)
            yield return this.Warning(queueName, "Existing messages in the queue will be purged on service start");

        if (_endpointConfiguration.Transport.PrefetchCount > ushort.MaxValue)
            yield return this.Failure("PrefetchCount", "must be at most 65535 for RabbitMQ").WithParentKey(queueName);

        foreach (var result in _channelConfigurator.Validate())
            yield return result.WithParentKey(queueName);

        foreach (var result in _connectionConfigurator.Validate())
            yield return result.WithParentKey(queueName);

        foreach (var result in base.Validate())
            yield return result.WithParentKey(queueName);
    }

    /// <summary>Sets whether endpoint topology survives broker restarts.</summary>
    public bool Durable
    {
        set
        {
            _settings.Durable = value;

            Changed("Durable");
        }
    }

    /// <summary>Sets whether the queue is exclusive to its declaring connection.</summary>
    public bool Exclusive
    {
        set
        {
            _settings.Exclusive = value;

            Changed("Exclusive");
        }
    }

    /// <summary>Sets whether RabbitMQ deletes endpoint topology when unused.</summary>
    public bool AutoDelete
    {
        set
        {
            _settings.AutoDelete = value;

            Changed("AutoDelete");
        }
    }

    /// <summary>Sets the endpoint exchange type.</summary>
    public string ExchangeType
    {
        set => _settings.ExchangeType = value;
    }

    /// <summary>Sets whether the endpoint queue is purged on first startup.</summary>
    public bool PurgeOnStartup
    {
        set => _settings.PurgeOnStartup = value;
    }

    /// <summary>Sets the RabbitMQ consumer priority.</summary>
    public int ConsumerPriority
    {
        set => _settings.ConsumerPriority = value;
    }

    /// <summary>Sets whether the broker permits only this endpoint consumer on the queue.</summary>
    public bool ExclusiveConsumer
    {
        set => _settings.ExclusiveConsumer = value;
    }

    /// <summary>Configures the endpoint queue as a RabbitMQ stream.</summary>
    /// <param name="callback">An optional callback that configures retention and consumer offset.</param>
    public void Stream(Action<IRabbitMqStreamConfigurator>? callback = null)
    {
        _settings.QueueArguments[RabbitMQ.Client.Headers.XQueueType] = "stream";

        var configurator = new RabbitMqStreamConfigurator(_settings);

        callback?.Invoke(configurator);
    }

    /// <summary>Configures the endpoint queue as a RabbitMQ stream with an explicit consumer tag.</summary>
    /// <param name="consumerTag">The stable stream consumer tag.</param>
    /// <param name="callback">An optional callback that configures retention and consumer offset.</param>
    public void Stream(string consumerTag, Action<IRabbitMqStreamConfigurator>? callback = null)
    {
        if (string.IsNullOrWhiteSpace(consumerTag))
            throw new ArgumentNullException(nameof(consumerTag));

        _settings.ConsumerTag = consumerTag;

        Stream(callback);
    }

    /// <summary>Sets RabbitMQ queue mode to <c>lazy</c> or <c>default</c>.</summary>
    public bool Lazy
    {
        set => _settings.Lazy = value;
    }

    /// <summary>Sets whether deployment includes the endpoint queue and its exchange binding.</summary>
    public bool BindQueue
    {
        set => _settings.BindQueue = value;
    }

    /// <summary>Sets how long an unused endpoint queue may remain before RabbitMQ deletes it; positive values must use whole milliseconds.</summary>
    public TimeSpan? QueueExpiration
    {
        set => _settings.QueueExpiration = value;
    }

    /// <summary>Sets or removes RabbitMQ single-active-consumer behavior.</summary>
    public bool SingleActiveConsumer
    {
        set => _settings.SingleActiveConsumer = value;
    }

    /// <summary>Sets the exchange that receives expired or rejected queue messages.</summary>
    public string DeadLetterExchange
    {
        set => SetQueueArgument(RabbitMQ.Client.Headers.XDeadLetterExchange, value);
    }

    /// <summary>Sets or removes a queue declaration argument.</summary>
    /// <param name="key">The RabbitMQ queue-argument key.</param>
    /// <param name="value">The argument value, or <see langword="null" /> to remove it.</param>
    public void SetQueueArgument(string key, object? value)
    {
        _settings.SetQueueArgument(key, value);
    }

    /// <summary>Sets a queue declaration argument from a nonnegative duration converted to whole milliseconds; <c>x-expires</c> must be positive.</summary>
    /// <param name="key">The RabbitMQ queue-argument key.</param>
    /// <param name="value">The duration to convert.</param>
    public void SetQueueArgument(string key, TimeSpan value)
    {
        _settings.SetQueueArgument(key, value);
    }

    /// <summary>Sets or removes an exchange declaration argument.</summary>
    /// <param name="key">The RabbitMQ exchange-argument key.</param>
    /// <param name="value">The argument value, or <see langword="null" /> to remove it.</param>
    public void SetExchangeArgument(string key, object? value)
    {
        _settings.SetExchangeArgument(key, value);
    }

    /// <summary>Sets an exchange declaration argument from a nonnegative duration converted to whole milliseconds.</summary>
    /// <param name="key">The RabbitMQ exchange-argument key.</param>
    /// <param name="value">The duration to convert.</param>
    public void SetExchangeArgument(string key, TimeSpan value)
    {
        _settings.SetExchangeArgument(key, value);
    }

    /// <summary>Enables priority delivery on the endpoint queue.</summary>
    /// <param name="maxPriority">The highest accepted message priority.</param>
    public void EnablePriority(byte maxPriority)
    {
        _settings.EnablePriority(maxPriority);
    }

    /// <summary>Configures the endpoint as a RabbitMQ quorum queue.</summary>
    /// <param name="replicationFactor">The optional initial quorum-group size.</param>
    public void SetQuorumQueue(int? replicationFactor = default)
    {
        _settings.SetQuorumQueue(replicationFactor);
    }

    /// <summary>Sets the queue's RabbitMQ consumer acknowledgement timeout.</summary>
    /// <param name="timeSpan">The positive acknowledgement timeout in whole milliseconds.</param>
    public void SetDeliveryAcknowledgementTimeout(TimeSpan timeSpan)
    {
        if (timeSpan <= TimeSpan.Zero)
            throw new ArgumentException("The RabbitMQ consumer timeout must be > 0");

        if (timeSpan.Ticks % TimeSpan.TicksPerMillisecond != 0)
            throw new ArgumentOutOfRangeException(nameof(timeSpan), "The RabbitMQ consumer timeout must use whole milliseconds.");

        SetQueueArgument("x-consumer-timeout", timeSpan.Ticks / TimeSpan.TicksPerMillisecond);
    }

    /// <summary>Sets the queue's RabbitMQ consumer acknowledgement timeout from duration components.</summary>
    /// <param name="d">Days.</param>
    /// <param name="h">Hours.</param>
    /// <param name="m">Minutes.</param>
    /// <param name="s">Seconds.</param>
    /// <param name="ms">Milliseconds.</param>
    public void SetDeliveryAcknowledgementTimeout(int? d = null, int? h = null, int? m = null, int? s = null, int? ms = null)
    {
        var value = new TimeSpan(d ?? 0, h ?? 0, m ?? 0, s ?? 0, ms ?? 0);

        SetDeliveryAcknowledgementTimeout(value);
    }

    /// <summary>Binds an exchange to the receive endpoint exchange.</summary>
    /// <param name="exchangeName">The exchange name.</param>
    /// <param name="callback">An optional callback that customizes the exchange and binding.</param>
    public void Bind(string exchangeName, Action<IRabbitMqExchangeToExchangeBindingConfigurator>? callback)
    {
        if (exchangeName == null)
            throw new ArgumentNullException(nameof(exchangeName));

        _endpointConfiguration.Topology.Consume.Bind(exchangeName, callback);
    }

    /// <summary>Binds a message contract's publish exchange to the receive endpoint.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <param name="callback">An optional callback that customizes the exchange binding.</param>
    public void Bind<T>(Action<IRabbitMqExchangeBindingConfigurator>? callback)
        where T : class
    {
        _endpointConfiguration.Topology.Consume.GetMessageTopology<T>().Bind(callback);
    }

    /// <summary>Declares a dead-letter exchange and queue and assigns the exchange to this endpoint queue.</summary>
    /// <param name="exchangeName">The exchange name.</param>
    /// <param name="queueName">The queue name.</param>
    /// <param name="configure">An optional callback that customizes the dead-letter topology.</param>
    public void BindDeadLetterQueue(string exchangeName, string? queueName, Action<IRabbitMqQueueBindingConfigurator>? configure)
    {
        _endpointConfiguration.Topology.Consume.BindQueue(exchangeName, queueName ?? exchangeName, configure);

        DeadLetterExchange = exchangeName;
    }

    /// <summary>Adds middleware to the RabbitMQ channel pipeline.</summary>
    /// <param name="configure">The channel-pipeline configuration callback.</param>
    public void ConfigureChannel(Action<IPipeConfigurator<ChannelContext>> configure)
    {
        configure?.Invoke(_channelConfigurator);
    }

    /// <summary>Adds middleware to the RabbitMQ connection pipeline.</summary>
    /// <param name="configure">The connection-pipeline configuration callback.</param>
    public void ConfigureConnection(Action<IPipeConfigurator<ConnectionContext>> configure)
    {
        configure?.Invoke(_connectionConfigurator);
    }

    /// <summary>Overrides the broker-generated consumer tag.</summary>
    /// <param name="consumerTag">The explicit RabbitMQ consumer tag.</param>
    public void OverrideConsumerTag(string consumerTag)
    {
        _settings.ConsumerTag = consumerTag;
    }

    RabbitMqReceiveEndpointContext CreateRabbitMqReceiveEndpointContext()
    {
        var builder = new RabbitMqReceiveEndpointBuilder(_hostConfiguration, this);

        ApplySpecifications(builder);

        return builder.CreateReceiveEndpointContext(_connectionConfigurator.BuildWithContinuation());
    }

    Uri FormatInputAddress()
    {
        return _settings.GetInputAddress(_hostConfiguration.HostAddress);
    }

    /// <summary>Determines whether address evaluation or base configuration has frozen the endpoint.</summary>
    /// <returns><see langword="true" /> when the endpoint can no longer be changed.</returns>
    protected override bool IsAlreadyConfigured()
    {
        return _inputAddressRead || base.IsAlreadyConfigured();
    }
}
